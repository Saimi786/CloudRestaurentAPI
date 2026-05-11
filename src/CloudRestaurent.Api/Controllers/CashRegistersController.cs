using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Infrastructure.Identity;
using CloudRestaurent.Modules.Sales.Application.CashRegisters.Commands;
using CloudRestaurent.Modules.Sales.Application.CashRegisters.Dtos;
using CloudRestaurent.Modules.Sales.Application.CashRegisters.Queries;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/cash-registers")]
public sealed class CashRegistersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CashRegisterDto>>> List(
        [FromQuery] Guid? branchId, [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetCashRegistersQuery(branchId, includeInactive), ct));

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<ActionResult<CashRegisterDto>> Create(
        [FromBody] CreateCashRegisterCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<IActionResult> Update(Guid id,
        [FromBody] UpdateCashRegisterCommand command, CancellationToken ct)
    {
        if (id != command.Id)
            throw new ValidationException(new Dictionary<string, string[]> { ["id"] = ["Route id must match body id."] });
        await mediator.Send(command, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{AppRoles.SuperAdmin},{AppRoles.TenantAdmin}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateCashRegisterCommand(id), ct);
        return NoContent();
    }
}

[ApiController]
[Authorize]
[Route("api/v1/cash-register-shifts")]
public sealed class CashRegisterShiftsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CashRegisterShiftSummaryDto>>> List(
        [FromQuery] Guid? cashRegisterId, [FromQuery] ShiftStatus? status,
        [FromQuery] int take = 100, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetShiftsQuery(cashRegisterId, status, take), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CashRegisterShiftDto>> Get(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new GetShiftByIdQuery(id), ct));

    [HttpPost("open")]
    public async Task<ActionResult<CashRegisterShiftDto>> Open(
        [FromBody] OpenShiftCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<CashRegisterShiftDto>> Close(
        Guid id, [FromBody] CloseShiftBody body, CancellationToken ct) =>
        Ok(await mediator.Send(new CloseShiftCommand(id, body.DeclaredClosingAmount, body.Notes), ct));

    [HttpPost("{id:guid}/movements")]
    public async Task<IActionResult> AddMovement(
        Guid id, [FromBody] AddMovementBody body, CancellationToken ct)
    {
        await mediator.Send(new AddShiftMovementCommand(
            id, body.Type, body.Amount, body.Reference, body.Notes), ct);
        return NoContent();
    }

    public sealed record CloseShiftBody(decimal DeclaredClosingAmount, string? Notes);
    public sealed record AddMovementBody(ShiftMovementType Type, decimal Amount, string? Reference, string? Notes);
}
