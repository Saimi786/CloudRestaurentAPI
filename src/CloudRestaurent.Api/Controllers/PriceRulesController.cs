using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Pricing.Application.Commands;
using CloudRestaurent.Modules.Pricing.Application.Dtos;
using CloudRestaurent.Modules.Pricing.Application.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/price-rules")]
public sealed class PriceRulesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PriceRuleDto>>> List(
        [FromQuery] Guid? productId = null,
        [FromQuery] Guid? branchId = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetPriceRulesQuery(productId, branchId, includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PriceRuleDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetPriceRuleByIdQuery(id), ct));

    [HttpGet("resolve")]
    public async Task<ActionResult<ResolvedPriceDto>> Resolve(
        [FromQuery] Guid productId,
        [FromQuery] Guid? branchId = null,
        [FromQuery] DateTime? at = null,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new ResolvePriceQuery(productId, branchId, at), ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<ActionResult<PriceRuleDto>> Create(
        [FromBody] CreatePriceRuleCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.TenantAdmin},{AppRoles.BranchManager}")]
    public async Task<ActionResult<PriceRuleDto>> Update(
        Guid id, [FromBody] UpdatePriceRuleCommand command, CancellationToken ct)
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
        await mediator.Send(new DeactivatePriceRuleCommand(id), ct);
        return NoContent();
    }
}
