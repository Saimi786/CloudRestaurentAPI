using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Brands.Commands;
using CloudRestaurent.Modules.Catalog.Application.Brands.Dtos;
using CloudRestaurent.Modules.Catalog.Application.Brands.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/brands")]
public sealed class BrandsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BrandDto>>> List(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetBrandsQuery(includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BrandDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetBrandByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<ActionResult<BrandDto>> Create(
        [FromBody] CreateBrandCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<ActionResult<BrandDto>> Update(
        Guid id, [FromBody] UpdateBrandCommand command, CancellationToken ct)
    {
        if (id != command.Id)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Route id and body id must match."]
            });
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateBrandCommand(id), ct);
        return NoContent();
    }
}
