using CloudRestaurent.Application.Common.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Modules.Tax;

public sealed class TaxModule : IModuleInstaller
{
    public ModuleManifest Manifest { get; } = new(
        Id: "Tax",
        Name: "Tax",
        Version: "1.0.0",
        Description: "Tax rates — applied to product sales for GST/VAT/PST compliance");

    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaxModule).Assembly);
    }
}
