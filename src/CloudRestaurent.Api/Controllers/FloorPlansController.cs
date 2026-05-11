using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Commands;
using CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Dtos;
using CloudRestaurent.Modules.Restaurant.Application.FloorPlans.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/floor-plans")]
public sealed class FloorPlansController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FloorPlanDto>>> List(
        [FromQuery] Guid? branchId = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetFloorPlansQuery(branchId, includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<FloorPlanDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetFloorPlanByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<ActionResult<FloorPlanDto>> Create(
        [FromBody] CreateFloorPlanCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<ActionResult<FloorPlanDto>> Update(
        Guid id, [FromBody] UpdateFloorPlanCommand command, CancellationToken ct)
    {
        if (id != command.Id)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Route id and body id must match."]
            });
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateFloorPlanCommand(id), ct);
        return NoContent();
    }
}
