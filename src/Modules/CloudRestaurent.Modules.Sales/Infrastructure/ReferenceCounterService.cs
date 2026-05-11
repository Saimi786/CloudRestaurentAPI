using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Infrastructure;

public sealed class ReferenceCounterService(IAppDbContext db) : IReferenceCounterService
{
    public async Task<string> NextAsync(
        Guid tenantId, Guid branchId, string documentType, string prefix, CancellationToken ct)
    {
        var counter = await db.Set<ReferenceCounter>()
            .FirstOrDefaultAsync(c =>
                c.BranchId == branchId &&
                c.DocumentType == documentType, ct);

        if (counter is null)
        {
            counter = new ReferenceCounter(Guid.NewGuid(), tenantId, branchId, documentType, prefix);
            db.Set<ReferenceCounter>().Add(counter);
        }

        var next = counter.Next();
        // SaveChanges is the caller's responsibility — happens once for the whole order.
        return next;
    }
}
