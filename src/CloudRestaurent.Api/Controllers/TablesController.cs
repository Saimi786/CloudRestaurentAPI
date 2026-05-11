using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Application.Tables.Commands;
using CloudRestaurent.Modules.Restaurant.Application.Tables.Dtos;
using CloudRestaurent.Modules.Restaurant.Application.Tables.Queries;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tables")]
public sealed class TablesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TableDto>>> List(
        [FromQuery] Guid? branchId = null,
        [FromQuery] Guid? floorPlanId = null,
        [FromQuery] TableStatus? status = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetTablesQuery(branchId, floorPlanId, status, includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TableDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetTableByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<ActionResult<TableDto>> Create(
        [FromBody] CreateTableCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<ActionResult<TableDto>> Update(
        Guid id, [FromBody] UpdateTableCommand command, CancellationToken ct)
    {
        if (id != command.Id)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Route id and body id must match."]
            });
        return Ok(await mediator.Send(command, ct));
    }

    public sealed record SetStatusBody(TableStatus Status);

    [HttpPost("{id:guid}/status")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.Cashier},{AppRoles.Waiter}")]
    public async Task<IActionResult> SetStatus(
        Guid id, [FromBody] SetStatusBody body, CancellationToken ct)
    {
        await mediator.Send(new SetTableStatusCommand(id, body.Status), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateTableCommand(id), ct);
        return NoContent();
    }
}
