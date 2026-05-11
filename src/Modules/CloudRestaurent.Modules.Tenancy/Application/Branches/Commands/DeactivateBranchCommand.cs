using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Companies;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Branches.Commands;

public sealed record DeactivateBranchCommand(Guid Id) : IRequest;

public sealed class DeactivateBranchHandler(IAppDbContext db)
    : IRequestHandler<DeactivateBranchCommand>
{
    public async Task Handle(DeactivateBranchCommand request, CancellationToken ct)
    {
        // SuperAdmin-only endpoint; cross-tenant deactivation is intentional.
        var branch = await db.Set<Branch>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == request.Id, ct)
            ?? throw new NotFoundException("Branch", request.Id);

        if (!branch.IsActive) return;

        branch.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
