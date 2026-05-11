using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Infrastructure.Identity;
using CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Commands;
using CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Dtos;
using CloudRestaurent.Modules.Restaurant.Application.KitchenStations.Queries;
using CloudRestaurent.Modules.Restaurant.Application.Printing;
using CloudRestaurent.Modules.Sales.Application.KitchenPrinting;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/kitchen-stations")]
public sealed class KitchenStationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<KitchenStationDto>>> List(
        [FromQuery] Guid? branchId = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetKitchenStationsQuery(branchId, includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<KitchenStationDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetKitchenStationByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<KitchenStationDto>> Create(
        [FromBody] CreateKitchenStationCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<KitchenStationDto>> Update(
        Guid id, [FromBody] UpdateKitchenStationCommand command, CancellationToken ct)
    {
        if (id != command.Id)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Route id and body id must match."]
            });
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateKitchenStationCommand(id), ct);
        return NoContent();
    }

    public sealed record PrintTicketBody(Guid TicketId);

    [HttpPost("{id:guid}/print")]
    public async Task<ActionResult<PrintResult>> PrintTicket(
        Guid id, [FromBody] PrintTicketBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new PrintKitchenTicketCommand(body.TicketId, id), ct));
}
