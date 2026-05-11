using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Inventory.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Branch = CloudRestaurent.Domain.Companies.Branch;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;
using Unit = CloudRestaurent.Modules.Catalog.Domain.Unit;

namespace CloudRestaurent.Modules.Inventory.Application.Commands;

public sealed record LogWasteCommand(
    Guid BranchId,
    Guid ProductId,
    Guid UnitId,
    decimal Quantity,
    WasteReason Reason,
    string? Notes,
    DateTimeOffset? OccurredAt) : IRequest<WasteLogDto>;

public sealed record WasteLogDto(
    Guid Id, Guid BranchId, string BranchName,
    Guid ProductId, string Sku, string ProductName,
    Guid UnitId, string UnitCode,
    decimal Quantity, decimal QuantityInProductUnit, string ProductUnitCode,
    WasteReason Reason, string ReasonName,
    string? Notes,
    Guid CreatedByUserId, DateTimeOffset OccurredAt);

public sealed class LogWasteValidator : AbstractValidator<LogWasteCommand>
{
    public LogWasteValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.UnitId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Reason).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class LogWasteHandler(IAppDbContext db, ITenantContext tenant, ICurrentUser user)
    : IRequestHandler<LogWasteCommand, WasteLogDto>
{
    public async Task<WasteLogDto> Handle(LogWasteCommand request, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");
        var userId = user.UserId
            ?? throw new UnauthorizedException("No authenticated user.");

        var branch = await db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);
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

        var deltaInProductUnit = request.Quantity * movementUnit.ConversionFactor / productUnit.ConversionFactor;
        var occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;

        // Stock-side: signed-negative Wastage movement
        var movement = new StockMovement(
            Guid.NewGuid(), tenantId, request.BranchId, product.Id, request.UnitId,
            StockMovementType.Wastage, -request.Quantity, -deltaInProductUnit,
            reference: $"WASTE-{Guid.NewGuid():N}"[..14],
            notes: request.Notes, occurredAt);
        db.Set<StockMovement>().Add(movement);

        var balance = await db.Set<StockBalance>()
            .FirstOrDefaultAsync(b => b.BranchId == request.BranchId && b.ProductId == product.Id, ct);
        if (balance is null)
        {
            balance = new StockBalance(Guid.NewGuid(), tenantId, request.BranchId, product.Id);
            db.Set<StockBalance>().Add(balance);
        }
        balance.Apply(-deltaInProductUnit, occurredAt);

        // Audit-side: WasteLog record with reason
        var log = new WasteLog(
            Guid.NewGuid(), tenantId, request.BranchId, product.Id, request.UnitId,
            request.Quantity, deltaInProductUnit, request.Reason, request.Notes,
            movement.Id, userId, occurredAt);
        db.Set<WasteLog>().Add(log);

        await db.SaveChangesAsync(ct);

        return new WasteLogDto(
            log.Id, log.BranchId, branch.Name,
            log.ProductId, product.Sku, product.Name,
            log.UnitId, movementUnit.Code,
            log.Quantity, log.QuantityInProductUnit, productUnit.Code,
            log.Reason, log.Reason.ToString(),
            log.Notes,
            log.CreatedByUserId, log.OccurredAt);
    }
}
