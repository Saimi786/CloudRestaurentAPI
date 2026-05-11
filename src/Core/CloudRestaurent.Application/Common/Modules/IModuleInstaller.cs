using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Application.Common.Modules;

/// <summary>
/// Contract every module implements. Modeled on UltimatePOS's
/// <c>nwidart/laravel-modules</c> service-provider pattern but adapted for .NET DI.
/// </summary>
public interface IModuleInstaller
{
    ModuleManifest Manifest { get; }

    /// <summary>
    /// Register module-specific DI services (handlers, services, etc.).
    /// MediatR/FluentValidation discovery is centralized — modules don't re-register those.
    /// </summary>
    void RegisterServices(IServiceCollection services, IConfiguration configuration);

    /// <summary>
    /// Apply EF Core entity configurations from the module assembly.
    /// </summary>
    void ConfigureModel(ModelBuilder modelBuilder);
}
