using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Infrastructure.Identity;
using CloudRestaurent.Modules.Pricing.Application.MixMatch.Commands;
using CloudRestaurent.Modules.Pricing.Application.MixMatch.Dtos;
using CloudRestaurent.Modules.Pricing.Application.MixMatch.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/mix-match-groups")]
public sealed class MixMatchGroupsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MixMatchGroupDto>>> List(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetMixMatchGroupsQuery(includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MixMatchGroupDetailDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetMixMatchGroupByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<MixMatchGroupDetailDto>> Create(
        [FromBody] CreateMixMatchGroupCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<MixMatchGroupDetailDto>> Update(
        Guid id, [FromBody] UpdateMixMatchGroupCommand command, CancellationToken ct)
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
        await mediator.Send(new DeactivateMixMatchGroupCommand(id), ct);
        return NoContent();
    }
}
