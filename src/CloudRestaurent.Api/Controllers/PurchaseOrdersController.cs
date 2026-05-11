using CloudRestaurent.Infrastructure.Identity;
using CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Commands;
using CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Dtos;
using CloudRestaurent.Modules.Inventory.Application.PurchaseOrders.Queries;
using CloudRestaurent.Modules.Inventory.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/purchase-orders")]
public sealed class PurchaseOrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderSummaryDto>>> List(
        [FromQuery] Guid? branchId, [FromQuery] Guid? supplierId,
        [FromQuery] PurchaseOrderStatus? status, [FromQuery] int take = 200,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetPurchaseOrdersQuery(branchId, supplierId, status, take), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetPurchaseOrderByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.InventoryManager}")]
    public async Task<ActionResult<PurchaseOrderDto>> Create(
        [FromBody] CreatePurchaseOrderCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpPost("{id:guid}/send")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct)
    {
        await mediator.Send(new SendPurchaseOrderCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await mediator.Send(new CancelPurchaseOrderCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/receive")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.InventoryManager}")]
    public async Task<ActionResult<PurchaseOrderDto>> Receive(
        Guid id, [FromBody] ReceiveBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new ReceivePurchaseOrderCommand(
            id, body.SupplierBillReference, body.BillDate, body.DueDate, body.Lines), ct));

    public sealed record ReceiveBody(
        string? SupplierBillReference,
        DateOnly? BillDate,
        DateOnly? DueDate,
        IReadOnlyList<ReceiveLineInput> Lines);
}

[ApiController]
[Authorize]
[Route("api/v1/supplier-bills")]
public sealed class SupplierBillsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SupplierBillDto>>> List(
        [FromQuery] Guid? supplierId, [FromQuery] SupplierBillStatus? status,
        [FromQuery] int take = 200, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetSupplierBillsQuery(supplierId, status, take), ct));

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<ActionResult<Guid>> Pay(
        Guid id, [FromBody] PayBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new PaySupplierBillCommand(id, body.Amount, body.Method, body.Reference), ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateBillBody body, CancellationToken ct)
    {
        await mediator.Send(new UpdateSupplierBillCommand(
            id, body.Amount, body.SupplierBillReference, body.BillDate, body.DueDate, body.Notes), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/match")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<ActionResult<BillMatchStatus>> Match(
        Guid id, [FromBody] MatchBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new MatchSupplierBillCommand(id, body.Tolerance, body.OverrideReason), ct));

    [HttpPost("{id:guid}/dispute")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<IActionResult> Dispute(
        Guid id, [FromBody] DisputeBody body, CancellationToken ct)
    {
        await mediator.Send(new DisputeSupplierBillCommand(id, body.Reason), ct);
        return NoContent();
    }

    public sealed record PayBody(decimal Amount, SupplierBillPaymentMethod Method, string? Reference);
    public sealed record UpdateBillBody(decimal Amount, string? SupplierBillReference, DateOnly BillDate, DateOnly? DueDate, string? Notes);
    public sealed record MatchBody(decimal Tolerance = 0.01m, string? OverrideReason = null);
    public sealed record DisputeBody(string Reason);
}
