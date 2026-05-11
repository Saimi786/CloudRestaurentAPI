using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Contacts.Domain;
using CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Dtos;
using CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Queries;
using CloudRestaurent.Modules.Inventory.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Commands;

public sealed record CreatePurchaseOrderCommand(
    Guid BranchId,
    Guid SupplierId,
    DateOnly? ExpectedDate,
    string Currency,
    string? Notes,
    IReadOnlyList<PurchaseOrderLineInput> Lines) : IRequest<PurchaseOrderDto>;

public sealed class CreatePurchaseOrderValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches(@"^[A-Z]{3}$");
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.Lines).NotEmpty();
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.ProductId).NotEmpty();
            l.RuleFor(x => x.UnitId).NotEmpty();
            l.RuleFor(x => x.OrderedQuantity).GreaterThan(0);
            l.RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.Notes).MaximumLength(500);
        });
    }
}

public sealed class CreatePurchaseOrderHandler(
    IAppDbContext db, ITenantContext tenant, IMediator mediator)
    : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDto>
{
    public async Task<PurchaseOrderDto> Handle(CreatePurchaseOrderCommand request, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        _ = await db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);

        var supplier = await db.Set<Customer>().FirstOrDefaultAsync(c => c.Id == request.SupplierId, ct)
            ?? throw new NotFoundException("Supplier", request.SupplierId);
        if (supplier.Type != ContactType.Supplier && supplier.Type != ContactType.Both)
            throw new BusinessRuleException($"Contact '{supplier.FullName}' is not a supplier.");

        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await db.Set<Product>().AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
        var missing = productIds.Except(products.Keys).ToList();
        if (missing.Count > 0)
            throw new NotFoundException("Product", string.Join(", ", missing));

        // Number: PO-NNNNN per tenant. Count existing rows + 1.
        var seq = await db.Set<PurchaseOrder>().CountAsync(ct) + 1;
        var number = $"PO-{seq:D5}";

        var poId = Guid.NewGuid();
        var po = new PurchaseOrder(
            poId, tenantId, request.BranchId, request.SupplierId,
            number, request.Currency.ToUpperInvariant(),
            request.ExpectedDate, request.Notes);

        var lines = request.Lines.Select(l =>
        {
            var p = products[l.ProductId];
            return new PurchaseOrderLine(
                Guid.NewGuid(), poId, l.ProductId, p.Sku, p.Name, l.UnitId,
                l.OrderedQuantity, l.UnitCost, l.Notes);
        }).ToList();
        po.ReplaceLines(lines);

        var subtotal = lines.Sum(l => l.LineTotal);
        po.RecomputeTotals(subtotal, 0m);

        db.Set<PurchaseOrder>().Add(po);
        await db.SaveChangesAsync(ct);

        return await mediator.Send(new GetPurchaseOrderByIdQuery(po.Id), ct);
    }
}

public sealed record SendPurchaseOrderCommand(Guid Id) : IRequest;
public sealed record CancelPurchaseOrderCommand(Guid Id) : IRequest;

public sealed class SendPurchaseOrderHandler(IAppDbContext db) : IRequestHandler<SendPurchaseOrderCommand>
{
    public async Task Handle(SendPurchaseOrderCommand request, CancellationToken ct)
    {
        var po = await db.Set<PurchaseOrder>().Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("PurchaseOrder", request.Id);
        po.Send();
        await db.SaveChangesAsync(ct);
    }
}

public sealed class CancelPurchaseOrderHandler(IAppDbContext db) : IRequestHandler<CancelPurchaseOrderCommand>
{
    public async Task Handle(CancelPurchaseOrderCommand request, CancellationToken ct)
    {
        var po = await db.Set<PurchaseOrder>().FirstOrDefaultAsync(p => p.Id == request.Id, ct)
            ?? throw new NotFoundException("PurchaseOrder", request.Id);
        po.Cancel();
        await db.SaveChangesAsync(ct);
    }
}
