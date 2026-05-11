using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CloudRestaurent.Infrastructure.Persistence.Interceptors;

public sealed class TenantStampingInterceptor(ITenantContext tenantContext, ICurrentUser currentUser)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            Stamp(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            Stamp(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void Stamp(DbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var userName = currentUser.UserName;
        var tenantId = tenantContext.TenantId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is ITenantScoped tenantScoped && entry.State == EntityState.Added)
            {
                if (tenantScoped.TenantId == Guid.Empty)
                {
                    if (tenantId is null)
                        throw new InvalidOperationException(
                            $"Cannot insert {entry.Entity.GetType().Name}: no tenant in current context and no explicit TenantId set.");
                    tenantScoped.TenantId = tenantId.Value;
                }
            }

            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAt = now;
                    auditable.CreatedBy = userName;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAt = now;
                    auditable.UpdatedBy = userName;
                }
            }
        }
    }
}

