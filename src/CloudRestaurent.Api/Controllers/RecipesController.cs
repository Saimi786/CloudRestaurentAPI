using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Commands;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Dtos;
using CloudRestaurent.Modules.Catalog.Application.Recipes.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/recipes")]
public sealed class RecipesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RecipeSummaryDto>>> List(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetRecipesQuery(includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RecipeDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetRecipeByIdQuery(id), ct));

    [HttpGet("by-product/{productId:guid}")]
    public async Task<ActionResult<RecipeDto>> GetByProduct(Guid productId, CancellationToken ct) =>
        Ok(await mediator.Send(new GetRecipeByProductIdQuery(productId), ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<ActionResult<RecipeDto>> Create(
        [FromBody] CreateRecipeCommand command, CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<ActionResult<RecipeDto>> Update(
        Guid id, [FromBody] UpdateRecipeCommand command, CancellationToken ct)
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
        await mediator.Send(new DeactivateRecipeCommand(id), ct);
        return NoContent();
    }
}
