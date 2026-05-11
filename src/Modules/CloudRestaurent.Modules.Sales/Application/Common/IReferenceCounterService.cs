namespace CloudRestaurent.Modules.Sales.Application.Common;

public interface IReferenceCounterService
{
    /// <summary>
    /// Allocate (and persist) the next reference for the given branch + document type.
    /// Creates the counter row on first call. Caller must SaveChangesAsync afterwards.
    /// </summary>
    Task<string> NextAsync(Guid tenantId, Guid branchId, string documentType, string prefix, CancellationToken ct);
}
