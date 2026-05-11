using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Accounting.Application.Accounts.Dtos;
using CloudRestaurent.Modules.Accounting.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Accounting.Application.Accounts.Queries;

public sealed record GetAccountsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<AccountDto>>;

public sealed class GetAccountsHandler(IAppDbContext db)
    : IRequestHandler<GetAccountsQuery, IReadOnlyList<AccountDto>>
{
    public async Task<IReadOnlyList<AccountDto>> Handle(GetAccountsQuery request, CancellationToken ct)
    {
        var query = db.Set<Account>().AsNoTracking();
        if (!request.IncludeInactive) query = query.Where(a => a.IsActive);
        var accounts = await query.OrderBy(a => a.Code).ToListAsync(ct);

        // Compute balance per account: debits − credits for asset/expense; credits − debits for the rest.
        var sums = await db.Set<AccountTransaction>().AsNoTracking()
            .GroupBy(t => new { t.AccountId, t.Side })
            .Select(g => new { g.Key.AccountId, g.Key.Side, Total = g.Sum(t => t.Amount) })
            .ToListAsync(ct);
        var byAccount = sums
            .GroupBy(s => s.AccountId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return accounts.Select(a =>
        {
            var rows = byAccount.GetValueOrDefault(a.Id, []);
            var debits = rows.Where(r => r.Side == LedgerSide.Debit).Sum(r => r.Total);
            var credits = rows.Where(r => r.Side == LedgerSide.Credit).Sum(r => r.Total);
            // Asset / Expense are debit-natured → balance = debits − credits
            // Liability / Equity / Revenue are credit-natured → balance = credits − debits
            var balance = a.Class is AccountClass.Asset or AccountClass.Expense
                ? debits - credits
                : credits - debits;
            return new AccountDto(a.Id, a.Code, a.Name, a.Class, a.Class.ToString(),
                a.Description, a.IsSystem, a.IsCashOrBank, a.IsActive, balance);
        }).ToList();
    }
}
