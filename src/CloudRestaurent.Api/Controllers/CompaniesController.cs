using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tenancy.Application.Companies.Commands;
using CloudRestaurent.Modules.Tenancy.Application.Companies.Dtos;
using CloudRestaurent.Modules.Tenancy.Application.Companies.Queries;
using CloudRestaurent.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/companies")]
public sealed class CompaniesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CompanyDto>>> List(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetCompaniesQuery(includeInactive), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetCompanyByIdQuery(id), ct));

    [HttpPost]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<ActionResult<CompanyDto>> Create(
        [FromBody] CreateCompanyCommand command,
        CancellationToken ct)
    {
        var dto = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<ActionResult<CompanyDto>> Update(
        Guid id,
        [FromBody] UpdateCompanyCommand command,
        CancellationToken ct)
    {
        if (id != command.Id)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["id"] = ["Route id and body id must match."]
            });
        return Ok(await mediator.Send(command, ct));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateCompanyCommand(id), ct);
        return NoContent();
    }
}
