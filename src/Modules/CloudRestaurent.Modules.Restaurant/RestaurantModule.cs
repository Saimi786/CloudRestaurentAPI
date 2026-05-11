using CloudRestaurent.Application.Common.Modules;
using CloudRestaurent.Modules.Restaurant.Application.Printing;
using CloudRestaurent.Modules.Restaurant.Infrastructure.Printing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Modules.Restaurant;

public sealed class RestaurantModule : IModuleInstaller
{
    public ModuleManifest Manifest { get; } = new(
        Id: "Restaurant",
        Name: "Restaurant",
        Version: "1.0.0",
        Description: "Floor plans, tables — the restaurant vertical");

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPrinterAdapter, NetworkEscPosAdapter>();
    }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestaurantModule).Assembly);
    }
}
