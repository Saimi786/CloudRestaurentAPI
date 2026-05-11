using CloudRestaurent.Modules.Sales.Application.Kitchen.Commands;
using CloudRestaurent.Modules.Sales.Application.Kitchen.Dtos;
using CloudRestaurent.Modules.Sales.Application.Kitchen.Queries;
using CloudRestaurent.Modules.Sales.Application.KitchenPrinting;
using BumpKitchenStationCommand = CloudRestaurent.Modules.Sales.Application.Kitchen.Commands.BumpKitchenStationCommand;
using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/kitchen")]
public sealed class KitchenController(IMediator mediator) : ControllerBase
{
    [HttpGet("tickets")]
    public async Task<ActionResult<IReadOnlyList<KitchenTicketDto>>> List(
        [FromQuery] Guid? branchId = null,
        [FromQuery] Guid? stationId = null,
        [FromQuery] bool includeServed = false,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetKitchenTicketsQuery(branchId, stationId, includeServed), ct));

    public sealed record AdvanceBody(KitchenTicketStatus Status);

    [HttpPost("tickets/{id:guid}/advance")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.KitchenStaff}")]
    public async Task<ActionResult<KitchenTicketDto>> Advance(
        Guid id, [FromBody] AdvanceBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new AdvanceKitchenTicketCommand(id, body.Status), ct));

    public sealed record BumpBody(Guid StationId, bool Unbump = false);

    [HttpPost("tickets/{id:guid}/bump")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.KitchenStaff}")]
    public async Task<ActionResult<KitchenTicketDto>> Bump(
        Guid id, [FromBody] BumpBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new BumpKitchenStationCommand(id, body.StationId, body.Unbump), ct));

    [HttpPost("tickets/{id:guid}/fire")]
    public async Task<ActionResult<FireKitchenTicketResult>> Fire(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new FireKitchenTicketCommand(id), ct));
}
