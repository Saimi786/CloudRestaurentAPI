using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Accounting.Application.Accounts.Dtos;
using CloudRestaurent.Modules.Accounting.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Accounting.Application.Accounts.Queries;

public sealed record GetAccountTransactionsQuery(
    Guid? AccountId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Limit = 200) : IRequest<IReadOnlyList<AccountTransactionDto>>;

public sealed class GetAccountTransactionsHandler(IAppDbContext db)
    : IRequestHandler<GetAccountTransactionsQuery, IReadOnlyList<AccountTransactionDto>>
{
    public async Task<IReadOnlyList<AccountTransactionDto>> Handle(GetAccountTransactionsQuery request, CancellationToken ct)
    {
        var query = db.Set<AccountTransaction>().AsNoTracking();
        if (request.AccountId is { } a) query = query.Where(t => t.AccountId == a);
        if (request.From is { } f) query = query.Where(t => t.OperationDate >= f);
        if (request.To is { } t) query = query.Where(x => x.OperationDate <= t);

        var limit = Math.Clamp(request.Limit, 1, 1000);

        return await (
            from x in query
            join acc in db.Set<Account>().AsNoTracking() on x.AccountId equals acc.Id
            orderby x.OperationDate descending
            select new AccountTransactionDto(
                x.Id, x.AccountId, acc.Code, acc.Name,
                x.Side, x.Side.ToString(),
                x.Amount, x.Currency,
                x.OperationDate, x.SourceType, x.SourceId, x.Description, x.BatchId))
            .Take(limit).ToListAsync(ct);
    }
}
