using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Infrastructure.Identity;
using CloudRestaurent.Modules.Tax.Application.TaxRates.Commands;
using CloudRestaurent.Modules.Tax.Application.TaxRates.Dtos;
using CloudRestaurent.Modules.Tax.Application.TaxRates.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/tax-rates")]
public sealed class TaxRatesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaxRateDto>>> List(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetTaxRatesQuery(includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaxRateDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetTaxRateByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<TaxRateDto>> Create(
        [FromBody] CreateTaxRateCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<TaxRateDto>> Update(
        Guid id, [FromBody] UpdateTaxRateCommand command, CancellationToken ct)
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
        await mediator.Send(new DeactivateTaxRateCommand(id), ct);
        return NoContent();
    }
}
