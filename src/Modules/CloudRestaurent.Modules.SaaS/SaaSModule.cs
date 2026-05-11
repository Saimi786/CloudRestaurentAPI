using CloudRestaurent.Application.Common.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CloudRestaurent.Modules.SaaS;

/// <summary>
/// Stub. Will own Subscriptions, Packages, Coupons, manual approval flow once we
/// build them per <c>REFERENCE_ANALYSIS.md</c> §5.
/// </summary>
public sealed class SaaSModule : IModuleInstaller
{
    public ModuleManifest Manifest { get; } = new(
        Id: "SaaS",
        Name: "SaaS",
        Version: "0.1.0",
        Description: "Subscriptions, packages, manual billing approval (stub)",
        RequiredTier: SubscriptionTier.Basic);

    public void RegisterServices(IServiceCollection services, IConfiguration configuration) { }

    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SaaSModule).Assembly);
    }
}
