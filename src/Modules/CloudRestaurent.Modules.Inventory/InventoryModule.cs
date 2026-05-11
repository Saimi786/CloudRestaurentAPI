using CloudRestaurent.Application.Common.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Modules.Inventory;

public sealed class InventoryModule : IModuleInstaller
{
    public ModuleManifest Manifest { get; } = new(
        Id: "Inventory",
        Name: "Inventory",
        Version: "1.0.0",
        Description: "Stock balances, stock movements, adjustments");

    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryModule).Assembly);
    }
}
