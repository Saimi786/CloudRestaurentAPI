using CloudRestaurent.Domain.Tenants;

namespace CloudRestaurent.Modules.Tenancy.Application.Tenants.Dtos;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    BusinessType BusinessType,
    SubscriptionPlan Plan,
    bool IsActive,
    string? LogoUrl);
