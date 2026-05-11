using System.Text;
using CloudRestaurent.Api.Common;
using CloudRestaurent.Api.Hubs;
using CloudRestaurent.Application;
using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Modules;
using CloudRestaurent.Infrastructure;
using CloudRestaurent.Infrastructure.Identity;
using CloudRestaurent.Infrastructure.Modules;
using CloudRestaurent.Modules.Accounting;
using CloudRestaurent.Modules.Catalog;
using CloudRestaurent.Modules.Contacts;
using CloudRestaurent.Modules.Identity;
using CloudRestaurent.Modules.Inventory;
using CloudRestaurent.Modules.Pricing;
using CloudRestaurent.Modules.Restaurant;
using CloudRestaurent.Modules.SaaS;
using CloudRestaurent.Modules.Sales;
using CloudRestaurent.Modules.Tax;
using CloudRestaurent.Modules.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// OpenTelemetry — traces + metrics. Console exporter for dev; OTLP wired but only
// activated when OTEL_EXPORTER_OTLP_ENDPOINT is set (so prod ops choose the backend
// at deploy time without a code change).
const string ServiceName = "CloudRestaurent.Api";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(ServiceName, serviceVersion: "1.0.0"))
    .WithTracing(t =>
    {
        t.AddAspNetCoreInstrumentation(o =>
        {
            // Don't trace the health check / metrics scrape paths — they bury the noise floor.
            o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health")
                           && !ctx.Request.Path.StartsWithSegments("/metrics");
        });
        t.AddHttpClientInstrumentation();
        t.AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = true);
        if (builder.Environment.IsDevelopment()) t.AddConsoleExporter();
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
            t.AddOtlpExporter();
    })
    .WithMetrics(m =>
    {
        m.AddAspNetCoreInstrumentation();
        m.AddHttpClientInstrumentation();
        m.AddRuntimeInstrumentation();
        if (builder.Environment.IsDevelopment()) m.AddConsoleExporter();
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
            m.AddOtlpExporter();
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IRequestAuditContext, HttpRequestAuditContext>();

// Module discovery — explicit list, not reflection. Mirrors UltimatePOS's
// modules_statuses.json activator. Each module's RegisterServices is invoked
// once, then a singleton IModuleRegistry exposes the enabled set to the runtime
// (e.g. AppDbContext.OnModelCreating, future quota-gating middleware).
IModuleInstaller[] moduleInstallers =
[
    new TenancyModule(),
    new IdentityModule(),
    new CatalogModule(),
    new PricingModule(),
    new InventoryModule(),
    new ContactsModule(),
    new SalesModule(),
    new RestaurantModule(),
    new TaxModule(),
    new AccountingModule(),
    new SaaSModule()
];

var moduleRegistry = new FileModuleRegistry(
    moduleInstallers,
    Path.Combine(builder.Environment.ContentRootPath, "modules_statuses.json"));

builder.Services.AddSingleton<IModuleRegistry>(moduleRegistry);

foreach (var module in moduleRegistry.EnabledModules)
    module.RegisterServices(builder.Services, builder.Configuration);

// Pass each module's assembly so MediatR + FluentValidation pick up handlers + validators
// that live in `Modules.X.Application`. Without this they silently fail to resolve.
var moduleAssemblies = moduleRegistry.EnabledModules
    .Select(m => m.GetType().Assembly)
    .Distinct()
    .ToArray();
builder.Services.AddApplication(moduleAssemblies);
builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:4200")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSignalR();
builder.Services.AddSingleton<IKitchenNotifier, SignalRKitchenNotifier>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await initializer.InitializeAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();
// Serve tenant uploads (logos, future images) from ContentRoot/wwwroot/uploads at /uploads/*.
// Configured explicitly with a PhysicalFileProvider so the request pipeline doesn't depend on
// WebRootPath being non-null — wwwroot may not exist on a fresh checkout, but uploaded files
// still need to be reachable as soon as the upload handler creates them.
var uploadRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads");
Directory.CreateDirectory(uploadRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadRoot),
    RequestPath = "/uploads"
});
// HTTPS redirection is dev-noisy: VS often launches with the "https" profile and bounces
// http://localhost:5001 → https://localhost:7273 (untrusted dev cert), breaking Angular's
// CORS calls. Skip it in Development; production should be fronted by a reverse proxy
// that enforces TLS anyway.
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CloudRestaurent.Api.Common.IdempotencyMiddleware>();
app.MapControllers();
app.MapHub<KitchenHub>("/hubs/kitchen");

app.Run();

public partial class Program;
