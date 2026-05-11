using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tenancy.Application.Branches.Commands;
using CloudRestaurent.Modules.Tenancy.Application.Branches.Dtos;
using CloudRestaurent.Modules.Tenancy.Application.Branches.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/branches")]
public sealed class BranchesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BranchDto>>> List(
        [FromQuery] Guid? companyId = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetBranchesQuery(companyId, includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BranchDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetBranchByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<ActionResult<BranchDto>> Create(
        [FromBody] CreateBranchCommand command,
        CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<ActionResult<BranchDto>> Update(
        Guid id,
        [FromBody] UpdateBranchCommand command,
        CancellationToken ct)
    {
        if (id != command.Id)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Route id and body id must match."]
            });
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateBranchCommand(id), ct);
        return NoContent();
    }
}
