using CloudRestaurent.Application.Common.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Modules.Tenancy;

public sealed class TenancyModule : IModuleInstaller
{
    public ModuleManifest Manifest { get; } = new(
        Id: "Tenancy",
        Name: "Tenancy",
        Version: "1.0.0",
        Description: "Tenants, Companies, Branches — multi-tenant foundation");

    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }

    // Tenant/Company/Branch entities live in Core.Domain; their EF configurations
    // live in Core.Infrastructure. Nothing for this module to contribute (yet).
    public void ConfigureModel(ModelBuilder modelBuilder) { }
}
