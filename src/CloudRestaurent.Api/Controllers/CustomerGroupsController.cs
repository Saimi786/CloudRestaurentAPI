using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Commands;
using CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Dtos;
using CloudRestaurent.Modules.Contacts.Application.CustomerGroups.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/customer-groups")]
public sealed class CustomerGroupsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerGroupDto>>> List(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetCustomerGroupsQuery(includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerGroupDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetCustomerGroupByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<CustomerGroupDto>> Create(
        [FromBody] CreateCustomerGroupCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<CustomerGroupDto>> Update(
        Guid id, [FromBody] UpdateCustomerGroupCommand command, CancellationToken ct)
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
        await mediator.Send(new DeactivateCustomerGroupCommand(id), ct);
        return NoContent();
    }
}
