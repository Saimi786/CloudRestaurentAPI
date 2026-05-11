using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Application.Commands;
using CloudRestaurent.Modules.Contacts.Application.Dtos;
using CloudRestaurent.Modules.Contacts.Application.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/customers")]
public sealed class CustomersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List(
        [FromQuery] string? search = null,
        [FromQuery] CloudRestaurent.Modules.Contacts.Domain.ContactType? type = null,
        [FromQuery] Guid? customerGroupId = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetCustomersQuery(search, type, customerGroupId, includeInactive), ct));

    [HttpGet("by-phone")]
    public async Task<ActionResult<CustomerDto>> GetByPhone(
        [FromQuery] string phone, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["phone"] = ["Phone is required."]
            });
        return Ok(await mediator.Send(new GetCustomerByPhoneQuery(phone), ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetCustomerByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.Cashier}")]
    public async Task<ActionResult<CustomerDto>> Create(
        [FromBody] CreateCustomerCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.Cashier}")]
    public async Task<ActionResult<CustomerDto>> Update(
        Guid id, [FromBody] UpdateCustomerCommand command, CancellationToken ct)
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
        await mediator.Send(new DeactivateCustomerCommand(id), ct);
        return NoContent();
    }

    public sealed record LoyaltyPointsBody(int Points);

    [HttpPost("{id:guid}/loyalty/earn")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.Cashier}")]
    public async Task<ActionResult<CustomerDto>> EarnPoints(
        Guid id, [FromBody] LoyaltyPointsBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new EarnLoyaltyPointsCommand(id, body.Points), ct));

    [HttpPost("{id:guid}/loyalty/redeem")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager},{AppRoles.Cashier}")]
    public async Task<ActionResult<CustomerDto>> RedeemPoints(
        Guid id, [FromBody] LoyaltyPointsBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new RedeemLoyaltyPointsCommand(id, body.Points), ct));
}
