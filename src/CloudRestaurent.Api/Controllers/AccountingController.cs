using CloudRestaurent.Modules.Accounting.Application.Accounts.Dtos;
using CloudRestaurent.Modules.Accounting.Application.Accounts.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CloudRestaurent.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/accounting")]
public sealed class AccountingController(IMediator mediator) : ControllerBase
{
    [HttpGet("accounts")]
    public async Task<ActionResult<IReadOnlyList<AccountDto>>> GetAccounts(
        [FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetAccountsQuery(includeInactive), ct));

    [HttpGet("transactions")]
    public async Task<ActionResult<IReadOnlyList<AccountTransactionDto>>> GetTransactions(
        [FromQuery] Guid? accountId = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int limit = 200,
        CancellationToken ct = default) =>
        Ok(await mediator.Send(new GetAccountTransactionsQuery(accountId, from, to, limit), ct));
}
