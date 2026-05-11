using CloudRestaurent.Modules.Accounting.Application.Expenses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/expenses")]
public sealed class ExpensesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseDto>>> List(
        [FromQuery] Guid? branchId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int take = 200,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetExpensesQuery(branchId, from, to, take), ct));

    [HttpPost]
    public async Task<ActionResult<ExpenseDto>> Create(
        [FromBody] CreateExpenseCommand command, CancellationToken ct) =>
        Ok(await mediator.Send(command, ct));
}
