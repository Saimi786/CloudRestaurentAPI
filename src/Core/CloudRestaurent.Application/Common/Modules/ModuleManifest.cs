namespace CloudRestaurent.Application.Common.Modules;

/// <summary>
/// Identity card for a module. Mirrors UltimatePOS's <c>module.json</c>.
/// </summary>
public sealed record ModuleManifest(
    string Id,
    string Name,
    string Version,
    string Description,
    SubscriptionTier RequiredTier = SubscriptionTier.Basic,
    string[]? DependsOn = null);

/// <summary>
/// Subscription tier required to enable a module. Modules above the tenant's
/// active tier return 402 Payment Required when their handlers are invoked.
/// </summary>
public enum SubscriptionTier
{
    Basic = 0,
    Standard = 1,
    Premium = 2,
    Enterprise = 3
}
