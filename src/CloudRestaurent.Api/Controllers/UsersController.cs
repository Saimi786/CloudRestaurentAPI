using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Identity.Application.Users.Commands;
using CloudRestaurent.Modules.Identity.Application.Users.Dtos;
using CloudRestaurent.Modules.Identity.Application.Users.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize(Roles = AppRoles.TenantAdmin)]
[Route("api/v1/users")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetUsersQuery(includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetUserByIdQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    public sealed record UpdateUserBody(
        string FullName,
        bool IsActive,
        IReadOnlyList<string> Roles,
        IReadOnlyList<Guid>? BranchIds = null,
        decimal? MaxDiscountPercent = null);

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserDto>> Update(
        Guid id, [FromBody] UpdateUserBody body, CancellationToken ct) =>
        Ok(await mediator.Send(
            new UpdateUserCommand(id, body.FullName, body.IsActive, body.Roles,
                body.BranchIds, body.MaxDiscountPercent), ct));

    public sealed record ResetPasswordBody(string NewPassword);

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid id, [FromBody] ResetPasswordBody body, CancellationToken ct)
    {
        await mediator.Send(new ResetPasswordCommand(id, body.NewPassword), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateUserCommand(id), ct);
        return NoContent();
    }
}
