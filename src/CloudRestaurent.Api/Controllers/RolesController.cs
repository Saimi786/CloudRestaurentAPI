using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Identity.Application.Roles;
using CloudRestaurent.Modules.Identity.Application.Users.Dtos;
using CloudRestaurent.Modules.Identity.Application.Users.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api/v1/roles")]
public sealed class RolesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> List(CancellationToken ct) =>
        Ok(await mediator.Send(new GetAssignableRolesQuery(), ct));

    /// <summary>
    /// Returns every role visible to this tenant (built-ins + custom) with their granted
    /// permissions and current user count. Used by the role admin UI.
    /// </summary>
    [HttpGet("details")]
    public async Task<ActionResult<IReadOnlyList<RoleDetailsDto>>> Details(CancellationToken ct) =>
        Ok(await mediator.Send(new GetRolesDetailedQuery(), ct));

    [HttpGet("permissions")]
    public async Task<ActionResult<IReadOnlyList<PermissionDescriptor>>> Permissions(CancellationToken ct) =>
        Ok(await mediator.Send(new GetPermissionCatalogQuery(), ct));

    public sealed record CreateRoleBody(string Name, IReadOnlyList<string> Permissions);

    [HttpPost]
    public async Task<ActionResult<RoleDetailsDto>> Create([FromBody] CreateRoleBody body, CancellationToken ct)
    {
        var dto = await mediator.Send(new CreateRoleCommand(body.Name, body.Permissions), ct);
        return CreatedAtAction(nameof(Details), new { id = dto.Id }, dto);
    }

    public sealed record UpdateRoleBody(string Name, IReadOnlyList<string> Permissions);

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoleDetailsDto>> Update(
        Guid id, [FromBody] UpdateRoleBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new UpdateRoleCommand(id, body.Name, body.Permissions), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteRoleCommand(id), ct);
        return NoContent();
    }
}
