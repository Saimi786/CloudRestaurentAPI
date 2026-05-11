using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Commands;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Modules.Sales.Application.Queries;
using CloudRestaurent.Modules.Sales.Domain;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/orders")]
public sealed class OrdersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryDto>>> List(
        [FromQuery] Guid? branchId = null,
        [FromQuery] OrderStatus? status = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] int limit = 100,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetOrdersQuery(branchId, status, customerId, limit), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetOrderByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.Cashier},{AppRoles.Waiter}")]
    public async Task<ActionResult<OrderDto>> Open(
        [FromBody] OpenOrderCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    public sealed record AddLineBody(Guid ProductId, decimal Quantity, string? Notes, IReadOnlyList<Guid>? ModifierIds);

    [HttpPost("{id:guid}/lines")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.Cashier},{AppRoles.Waiter}")]
    public async Task<ActionResult<OrderDto>> AddLine(
        Guid id, [FromBody] AddLineBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new AddOrderLineCommand(
            id, body.ProductId, body.Quantity, body.Notes, body.ModifierIds ?? []), ct));

    [HttpDelete("{id:guid}/lines/{lineId:guid}")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.Cashier},{AppRoles.Waiter}")]
    public async Task<ActionResult<OrderDto>> RemoveLine(Guid id, Guid lineId, CancellationToken ct) =>
        Ok(await mediator.Send(new RemoveOrderLineCommand(id, lineId), ct));

    public sealed record AddPaymentBody(PaymentMethod Method, decimal Amount, string? Reference);

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.Cashier}")]
    public async Task<ActionResult<OrderDto>> AddPayment(
        Guid id, [FromBody] AddPaymentBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new AddPaymentCommand(id, body.Method, body.Amount, body.Reference), ct));

    public sealed record OverrideLinePriceBody(decimal UnitPrice);

    [HttpPut("{id:guid}/lines/{lineId:guid}/price")]
    [HasPermission(AppPermissions.CatalogManagePricing)]
    public async Task<ActionResult<OrderDto>> OverrideLinePrice(
        Guid id, Guid lineId, [FromBody] OverrideLinePriceBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new OverrideLinePriceCommand(id, lineId, body.UnitPrice), ct));

    public sealed record SetDiscountBody(decimal DiscountAmount);

    [HttpPost("{id:guid}/discount")]
    [HasPermission(AppPermissions.SalesApplyDiscount)]
    public async Task<ActionResult<OrderDto>> SetDiscount(
        Guid id, [FromBody] SetDiscountBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new SetOrderDiscountCommand(id, body.DiscountAmount), ct));

    public sealed record RedeemPointsBody(int Points);

    [HttpPost("{id:guid}/redeem-points")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.Cashier}")]
    public async Task<ActionResult<OrderDto>> RedeemPoints(
        Guid id, [FromBody] RedeemPointsBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new SetOrderRewardRedemptionCommand(id, body.Points), ct));

    [HttpGet("{id:guid}/redeem-preview")]
    public async Task<ActionResult<Modules.Sales.Application.Rewards.RewardRedemptionPreviewDto>> RedeemPreview(
        Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new Modules.Sales.Application.Rewards.GetRewardRedemptionPreviewQuery(id), ct));

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.Cashier}")]
    public async Task<ActionResult<OrderDto>> Close(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new CloseOrderCommand(id), ct));

    [HttpPost("{id:guid}/void")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<ActionResult<OrderDto>> Void(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new VoidOrderCommand(id), ct));
}
