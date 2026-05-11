using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Commands;
using CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/modifier-groups")]
public sealed class ModifierGroupsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ModifierGroupSummaryDto>>> List(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetModifierGroupsQuery(includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ModifierGroupDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetModifierGroupByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<ModifierGroupDto>> Create(
        [FromBody] CreateModifierGroupCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<ModifierGroupDto>> Update(
        Guid id, [FromBody] UpdateModifierGroupCommand command, CancellationToken ct)
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
        await mediator.Send(new DeactivateModifierGroupCommand(id), ct);
        return NoContent();
    }
}
