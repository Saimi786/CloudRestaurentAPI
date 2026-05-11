using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Units.Commands;
using CloudRestaurent.Modules.Catalog.Application.Units.Dtos;
using CloudRestaurent.Modules.Catalog.Application.Units.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/units")]
public sealed class UnitsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UnitDto>>> List(
        [FromQuery] Guid? groupId = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetUnitsQuery(groupId, includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetUnitByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<ActionResult<UnitDto>> Create(
        [FromBody] CreateUnitCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<ActionResult<UnitDto>> Update(
        Guid id, [FromBody] UpdateUnitCommand command, CancellationToken ct)
    {
        if (id != command.Id)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Route id and body id must match."]
            });
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateUnitCommand(id), ct);
        return NoContent();
    }
}
