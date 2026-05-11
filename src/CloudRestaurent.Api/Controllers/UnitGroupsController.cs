using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.UnitGroups.Commands;
using CloudRestaurent.Modules.Catalog.Application.UnitGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Application.UnitGroups.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/unit-groups")]
public sealed class UnitGroupsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UnitGroupDto>>> List(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetUnitGroupsQuery(includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitGroupDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetUnitGroupByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<ActionResult<UnitGroupDto>> Create(
        [FromBody] CreateUnitGroupCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<ActionResult<UnitGroupDto>> Update(
        Guid id, [FromBody] UpdateUnitGroupCommand command, CancellationToken ct)
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
        await mediator.Send(new DeactivateUnitGroupCommand(id), ct);
        return NoContent();
    }
}
