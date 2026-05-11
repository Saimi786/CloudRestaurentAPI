using CloudRestaurent.Application.Common.Modules;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Application.Promotions;
using CloudRestaurent.Modules.Sales.Infrastructure;
using CloudRestaurent.Modules.Sales.Infrastructure.Rewards;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Modules.Sales;

public sealed class SalesModule : IModuleInstaller
{
    public ModuleManifest Manifest { get; } = new(
        Id: "Sales",
        Name: "Sales",
        Version: "1.0.0",
        Description: "Orders, payments, kitchen tickets, sales workflow");

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IReferenceCounterService, ReferenceCounterService>();
        services.AddScoped<PromotionRecomputer>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<RewardPointsExpiryJob>();
    }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesModule).Assembly);
    }
}
