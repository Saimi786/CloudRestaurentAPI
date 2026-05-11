using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Modules;
using CloudRestaurent.Modules.Pricing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Modules.Pricing;

public sealed class PricingModule : IModuleInstaller
{
    public ModuleManifest Manifest { get; } = new(
        Id: "Pricing",
        Name: "Pricing",
        Version: "1.0.0",
        Description: "Price rules, time/day-of-week and per-branch overrides");

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IPriceResolver, PriceResolver>();
    }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PricingModule).Assembly);
    }
}
