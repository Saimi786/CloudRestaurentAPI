using CloudRestaurent.Infrastructure.Identity;
using CloudRestaurent.Modules.Tenancy.Application.Settings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

/// <summary>
/// Tenant-wide business settings — currency, timezone, fiscal year, tax label, reward points,
/// reference number prefixes, POS toggles. Mirrors UltimatePOS's "Business Settings" hub.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/settings")]
public sealed class SettingsController(IMediator mediator) : ControllerBase
{
    [HttpGet("business")]
    public async Task<ActionResult<BusinessSettingsDto>> Get(CancellationToken ct) =>
        Ok(await mediator.Send(new GetBusinessSettingsQuery(), ct));

    [HttpPut("business")]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<BusinessSettingsDto>> Update(
        [FromBody] UpdateBusinessSettingsCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));
}
