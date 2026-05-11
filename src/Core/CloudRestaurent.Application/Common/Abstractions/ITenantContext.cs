namespace CloudRestaurent.Application.Common.Abstractions;

public interface ITenantContext
{
    Guid? TenantId { get; }
    bool IsAuthenticated { get; }
}
