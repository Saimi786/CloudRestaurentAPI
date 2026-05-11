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

public sealed record TransferStockCommand(
    Guid FromBranchId,
    Guid ToBranchId,
    Guid ProductId,
    Guid UnitId,
    decimal Quantity,
    string? Reference,
    string? Notes,
    DateTimeOffset? OccurredAt) : IRequest<StockTransferResultDto>;

public sealed record StockTransferResultDto(
    Guid TransferOutId, Guid TransferInId, decimal QuantityInProductUnit, string ProductUnitCode);

public sealed class TransferStockValidator : AbstractValidator<TransferStockCommand>
{
    public TransferStockValidator()
    {
        RuleFor(x => x.FromBranchId).NotEmpty();
        RuleFor(x => x.ToBranchId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x).Must(c => c.FromBranchId != c.ToBranchId)
            .WithMessage("Source and destination branch must differ.");
    }
}

public sealed class TransferStockHandler(IAppDbContext db, ITenantContext tenant)
    : IRequestHandler<TransferStockCommand, StockTransferResultDto>
{
    public async Task<StockTransferResultDto> Handle(TransferStockCommand request, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        _ = await db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == request.FromBranchId, ct)
            ?? throw new NotFoundException("Branch", request.FromBranchId);
        _ = await db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == request.ToBranchId, ct)
            ?? throw new NotFoundException("Branch", request.ToBranchId);

        var product = await db.Set<Product>().FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new NotFoundException("Product", request.ProductId);
        if (!product.IsStockTracked)
            throw new BusinessRuleException($"Product '{product.Name}' is not stock-tracked.");

        var movementUnit = await db.Set<Unit>().FirstOrDefaultAsync(u => u.Id == request.UnitId, ct)
            ?? throw new NotFoundException("Unit", request.UnitId);
        var productUnit = await db.Set<Unit>().FirstOrDefaultAsync(u => u.Id == product.UnitId, ct)
            ?? throw new NotFoundException("Unit", product.UnitId);
        if (movementUnit.GroupId != productUnit.GroupId)
            throw new BusinessRuleException(
                $"Unit '{movementUnit.Code}' is not compatible with product unit '{productUnit.Code}'.");

        var qtyInProductUnit = request.Quantity * movementUnit.ConversionFactor / productUnit.ConversionFactor;
        var occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;
        var refKey = string.IsNullOrWhiteSpace(request.Reference)
            ? $"XFER-{Guid.NewGuid():N}"[..14]
            : request.Reference.Trim();

        // OUT side
        var outMovement = new StockMovement(
            Guid.NewGuid(), tenantId, request.FromBranchId, product.Id, request.UnitId,
            StockMovementType.TransferOut, -request.Quantity, -qtyInProductUnit,
            refKey, request.Notes, occurredAt);
        db.Set<StockMovement>().Add(outMovement);

        var fromBalance = await db.Set<StockBalance>()
            .FirstOrDefaultAsync(b => b.BranchId == request.FromBranchId && b.ProductId == product.Id, ct);
        if (fromBalance is null)
        {
            fromBalance = new StockBalance(Guid.NewGuid(), tenantId, request.FromBranchId, product.Id);
            db.Set<StockBalance>().Add(fromBalance);
        }
        fromBalance.Apply(-qtyInProductUnit, occurredAt);

        // IN side
        var inMovement = new StockMovement(
            Guid.NewGuid(), tenantId, request.ToBranchId, product.Id, request.UnitId,
            StockMovementType.TransferIn, request.Quantity, qtyInProductUnit,
            refKey, request.Notes, occurredAt);
        db.Set<StockMovement>().Add(inMovement);

        var toBalance = await db.Set<StockBalance>()
            .FirstOrDefaultAsync(b => b.BranchId == request.ToBranchId && b.ProductId == product.Id, ct);
        if (toBalance is null)
        {
            toBalance = new StockBalance(Guid.NewGuid(), tenantId, request.ToBranchId, product.Id);
            db.Set<StockBalance>().Add(toBalance);
        }
        toBalance.Apply(qtyInProductUnit, occurredAt);

        await db.SaveChangesAsync(ct);
        return new StockTransferResultDto(outMovement.Id, inMovement.Id, qtyInProductUnit, productUnit.Code);
    }
}
