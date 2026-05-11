using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Combos;
using CloudRestaurent.Modules.Catalog.Application.ModifierGroups.Dtos;
using CloudRestaurent.Modules.Catalog.Application.Products.Commands;
using CloudRestaurent.Modules.Catalog.Application.Products.Dtos;
using CloudRestaurent.Modules.Catalog.Application.Products.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/products")]
public sealed class ProductsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> List(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] string? search = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetProductsQuery(categoryId, brandId, search, includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetProductByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<ProductDto>> Update(
        Guid id, [FromBody] UpdateProductCommand command, CancellationToken ct)
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
        await mediator.Send(new DeactivateProductCommand(id), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/modifier-groups")]
    public async Task<ActionResult<IReadOnlyList<ModifierGroupSummaryDto>>> GetModifierGroups(
        Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetProductModifierGroupsQuery(id), ct));

    public sealed record SetModifierGroupsBody(IReadOnlyList<Guid> ModifierGroupIds);

    [HttpPut("{id:guid}/modifier-groups")]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<IActionResult> SetModifierGroups(
        Guid id, [FromBody] SetModifierGroupsBody body, CancellationToken ct)
    {
        await mediator.Send(new SetProductModifierGroupsCommand(id, body.ModifierGroupIds ?? []), ct);
        return NoContent();
    }

    public sealed record SetAvailabilityBody(bool IsAvailable);

    [HttpPut("{id:guid}/availability")]
    [HasPermission(AppPermissions.CatalogToggleAvailability)]
    public async Task<IActionResult> SetAvailability(
        Guid id, [FromBody] SetAvailabilityBody body, CancellationToken ct)
    {
        await mediator.Send(new SetProductAvailabilityCommand(id, body.IsAvailable), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/availability-windows")]
    public async Task<ActionResult<IReadOnlyList<AvailabilityWindowDto>>> GetWindows(
        Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetProductAvailabilityWindowsQuery(id), ct));

    public sealed record SetWindowsBody(IReadOnlyList<AvailabilityWindowInput> Windows);

    [HttpPut("{id:guid}/availability-windows")]
    [HasPermission(AppPermissions.CatalogManageProducts)]
    public async Task<IActionResult> SetWindows(
        Guid id, [FromBody] SetWindowsBody body, CancellationToken ct)
    {
        await mediator.Send(new SetProductAvailabilityWindowsCommand(id, body.Windows ?? []), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/combo-components")]
    public async Task<ActionResult<IReadOnlyList<ComboComponentDto>>> GetComboComponents(
        Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetComboComponentsQuery(id), ct));

    public sealed record SetComboComponentsBody(IReadOnlyList<ComboInput> Components);

    [HttpPut("{id:guid}/combo-components")]
    [Authorize(Roles = AppRoles.TenantAdmin)]
    public async Task<ActionResult<IReadOnlyList<ComboComponentDto>>> SetComboComponents(
        Guid id, [FromBody] SetComboComponentsBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new SetComboComponentsCommand(id, body.Components ?? []), ct));
}
