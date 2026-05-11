using CloudRestaurent.Application.Common.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Modules.Catalog;

public sealed class CatalogModule : IModuleInstaller
{
    public ModuleManifest Manifest { get; } = new(
        Id: "Catalog",
        Name: "Catalog",
        Version: "1.0.0",
        Description: "Categories, products, units, modifiers, recipes");

    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogModule).Assembly);
    }
}
