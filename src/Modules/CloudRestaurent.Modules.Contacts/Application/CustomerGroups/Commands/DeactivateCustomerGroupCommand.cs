using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Commands;

public sealed record DeactivateCustomerGroupCommand(Guid Id) : IRequest;

public sealed class DeactivateCustomerGroupHandler(IAppDbContext db)
    : IRequestHandler<DeactivateCustomerGroupCommand>
{
    public async Task Handle(DeactivateCustomerGroupCommand request, CancellationToken ct)
    {
        var group = await db.Set<CustomerGroup>().FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("CustomerGroup", request.Id);
        if (!group.IsActive) return;

        if (await db.Set<Customer>().AnyAsync(c => c.CustomerGroupId == request.Id && c.IsActive, ct))
            throw new BusinessRuleException(
                "Cannot deactivate a group that still has active customers. Reassign or deactivate them first.");

        group.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
