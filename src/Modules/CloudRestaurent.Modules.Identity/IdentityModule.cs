using CloudRestaurent.Application.Common.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Modules.Identity;

public sealed class IdentityModule : IModuleInstaller
{
    public ModuleManifest Manifest { get; } = new(
        Id: "Identity",
        Name: "Identity",
        Version: "1.0.0",
        Description: "Authentication, users, roles");

    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }

    // AppUser/AppRole and their configurations live in Core.Infrastructure
    // (tightly coupled to ASP.NET Identity). Nothing for this module to contribute.
    public void ConfigureModel(ModelBuilder modelBuilder) { }
}
