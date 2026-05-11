using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Contacts.Domain;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Commands;

public sealed record OpenOrderCommand(
    Guid BranchId,
    Guid? TableId,
    Guid? CustomerId,
    OrderType Type,
    string? Notes) : IRequest<OrderDto>;

public sealed class OpenOrderValidator : AbstractValidator<OpenOrderCommand>
{
    public OpenOrderValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class OpenOrderHandler(
    IAppDbContext db,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IKitchenNotifier kitchen,
    IReferenceCounterService referenceCounters)
    : IRequestHandler<OpenOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(OpenOrderCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        currentUser.EnsureCanAccess(request.BranchId);

        var branch = await db.Set<Branch>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);

        var company = await db.Set<Company>().AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == branch.CompanyId, ct)
            ?? throw new NotFoundException("Company", branch.CompanyId);

        if (request.TableId is { } tableId)
        {
            var table = await db.Set<RestaurantTable>().FirstOrDefaultAsync(t => t.Id == tableId, ct)
                ?? throw new NotFoundException("Table", tableId);
            if (table.BranchId != branch.Id)
                throw new BusinessRuleException("Table does not belong to the selected branch.");
            if (table.Status == TableStatus.OutOfService)
                throw new BusinessRuleException("Cannot seat guests at an out-of-service table.");
            table.SetStatus(TableStatus.Occupied);
        }

        if (request.CustomerId is { } cid &&
            !await db.Set<Customer>().AnyAsync(c => c.Id == cid && c.IsActive, ct))
            throw new NotFoundException("Customer", cid);

        var order = new Order(
            Guid.NewGuid(), tenantId, branch.Id,
            request.TableId, request.CustomerId,
            request.Type, company.DefaultCurrency,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes!.Trim());

        // Allocate sequential order number per branch — "SAL-00001" etc.
        var orderNumber = await referenceCounters.NextAsync(tenantId, branch.Id, "Sale", "SAL", ct);
        order.SetOrderNumber(orderNumber);

        db.Set<Order>().Add(order);

        // Auto-create the kitchen ticket so the kitchen sees the order the moment it's opened.
        var ticket = new KitchenTicket(Guid.NewGuid(), tenantId, order.Id, branch.Id);
        db.Set<KitchenTicket>().Add(ticket);

        await db.SaveChangesAsync(ct);
        await kitchen.TicketChangedAsync(tenantId, branch.Id, ticket.Id, ct);

        return await OrderDtoBuilder.BuildAsync(db, order, ct);
    }
}
