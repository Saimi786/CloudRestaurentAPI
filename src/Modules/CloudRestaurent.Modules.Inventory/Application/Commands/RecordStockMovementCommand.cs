using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Inventory.Application.Dtos;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Inventory.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Inventory.Application.Commands;

public sealed record RecordStockMovementCommand(
    Guid BranchId,
    Guid ProductId,
    Guid UnitId,
    StockMovementType Type,
    decimal Quantity,
    string? Reference,
    string? Notes,
    DateTimeOffset? OccurredAt) : IRequest<StockMovementDto>;

public sealed class RecordStockMovementValidator : AbstractValidator<RecordStockMovementCommand>
{
    public RecordStockMovementValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Quantity).NotEqual(0).WithMessage("Quantity cannot be zero.");
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Type).IsInEnum();
    }
}

public sealed class RecordStockMovementHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<RecordStockMovementCommand, StockMovementDto>
{
    public async Task<StockMovementDto> Handle(RecordStockMovementCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var branch = await db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);

        var product = await db.Set<Product>().FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new NotFoundException("Product", request.ProductId);

        if (!product.IsStockTracked)
            throw new BusinessRuleException(
                $"Product '{product.Name}' is not stock-tracked. Enable tracking on the Product first.");

        var movementUnit = await db.Set<Unit>().FirstOrDefaultAsync(u => u.Id == request.UnitId, ct)
            ?? throw new NotFoundException("Unit", request.UnitId);

        var productUnit = await db.Set<Unit>().FirstOrDefaultAsync(u => u.Id == product.UnitId, ct)
            ?? throw new NotFoundException("Unit", product.UnitId);

        if (movementUnit.GroupId != productUnit.GroupId)
            throw new BusinessRuleException(
                $"Unit '{movementUnit.Code}' is not in the same group as the product's unit '{productUnit.Code}'. Cannot convert.");

        // Apply sign convention: Purchase always +, Wastage/Sale/TransferOut always -, Adjustment uses caller's sign.
        var fixedSign = request.Type.FixedSign();
        var signedQty = fixedSign switch
        {
            > 0 => Math.Abs(request.Quantity),
            < 0 => -Math.Abs(request.Quantity),
            _ => request.Quantity
        };

        var deltaInProductUnit = signedQty * movementUnit.ConversionFactor / productUnit.ConversionFactor;
        var occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;

        // Upsert balance + insert movement, atomic via single SaveChanges.
        var balance = await db.Set<StockBalance>()
            .FirstOrDefaultAsync(b => b.BranchId == request.BranchId && b.ProductId == request.ProductId, ct);

        if (balance is null)
        {
            balance = new StockBalance(Guid.NewGuid(), tenantId, request.BranchId, request.ProductId);
            db.Set<StockBalance>().Add(balance);
        }
        balance.Apply(deltaInProductUnit, occurredAt);

        var movement = new StockMovement(
            Guid.NewGuid(), tenantId, request.BranchId, request.ProductId, request.UnitId,
            request.Type, signedQty, deltaInProductUnit,
            string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            occurredAt);
        db.Set<StockMovement>().Add(movement);

        await db.SaveChangesAsync(ct);

        return new StockMovementDto(
            movement.Id, movement.BranchId, branch.Name,
            movement.ProductId, product.Sku, product.Name,
            movement.Type, movement.Type.ToString(),
            movement.UnitId, movementUnit.Code,
            movement.Quantity, movement.QuantityInProductUnit, productUnit.Code,
            movement.Reference, movement.Notes, movement.OccurredAt, movement.CreatedBy);
    }
}
