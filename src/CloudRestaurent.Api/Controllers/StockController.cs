using CloudRestaurent.Modules.Inventory.Application.Commands;
using CloudRestaurent.Modules.Inventory.Application.Dtos;
using CloudRestaurent.Modules.Inventory.Application.Queries;
using CloudRestaurent.Modules.Inventory.Domain;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/stock")]
public sealed class StockController(IMediator mediator) : ControllerBase
{
    [HttpGet("balances")]
    public async Task<ActionResult<IReadOnlyList<StockBalanceDto>>> Balances(
        [FromQuery] Guid? branchId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetStockBalancesQuery(branchId, productId, search), ct));

    [HttpGet("movements")]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>> Movements(
        [FromQuery] Guid? branchId = null,
        [FromQuery] Guid? productId = null,
        [FromQuery] StockMovementType? type = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int limit = 200,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetStockMovementsQuery(branchId, productId, type, from, to, limit), ct));

    [HttpPost("movements")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.InventoryManager}")]
    public async Task<ActionResult<StockMovementDto>> RecordMovement(
        [FromBody] RecordStockMovementCommand command,
        CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpPost("transfers")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.InventoryManager}")]
    public async Task<ActionResult<StockTransferResultDto>> Transfer(
        [FromBody] TransferStockCommand command,
        CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpGet("waste-logs")]
    public async Task<ActionResult<IReadOnlyList<WasteLogDto>>> WasteLogs(
        [FromQuery] Guid? branchId,
        [FromQuery] WasteReason? reason,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int take = 200,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetWasteLogQuery(branchId, reason, from, to, take), ct));

    [HttpPost("waste-logs")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.InventoryManager}")]
    public async Task<ActionResult<WasteLogDto>> RecordWaste(
        [FromBody] LogWasteCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));
}
