using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.Commands;

public sealed record DeactivateCustomerCommand(Guid Id) : IRequest;

public sealed class DeactivateCustomerHandler(IAppDbContext db)
    : IRequestHandler<DeactivateCustomerCommand>
{
    public async Task Handle(DeactivateCustomerCommand request, CancellationToken ct)
    {
        var customer = await db.Set<Customer>().FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException("Customer", request.Id);
        if (!customer.IsActive) return;

        customer.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
