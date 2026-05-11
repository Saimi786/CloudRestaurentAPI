using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Accounting.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Accounting.Application.Reports;

public sealed record PnlAccountRow(string Code, string Name, decimal Amount);

public sealed record ProfitAndLossDto(
    DateTimeOffset From,
    DateTimeOffset To,
    IReadOnlyList<PnlAccountRow> RevenueAccounts,
    decimal TotalRevenue,
    IReadOnlyList<PnlAccountRow> ExpenseAccounts,
    decimal TotalExpense,
    decimal NetIncome);

/// <summary>
/// Profit &amp; Loss derived from the GL: Revenue accounts (credit-natured)
/// minus Expense accounts (debit-natured) over the chosen period. Pure read off
/// AccountTransaction — no per-source-type aggregation, so as long as ledger
/// posters are correct everywhere, the P&amp;L stays consistent for free.
/// </summary>
public sealed record GetProfitAndLossQuery(DateTimeOffset From, DateTimeOffset To)
    : IRequest<ProfitAndLossDto>;

public sealed class GetProfitAndLossHandler(IAppDbContext db)
    : IRequestHandler<GetProfitAndLossQuery, ProfitAndLossDto>
{
    public async Task<ProfitAndLossDto> Handle(GetProfitAndLossQuery request, CancellationToken ct)
    {
        // Pull only the accounts we care about — Revenue (Class=3) and Expense (Class=4).
        var accounts = await db.Set<Account>().AsNoTracking()
            .Where(a => a.Class == AccountClass.Revenue || a.Class == AccountClass.Expense)
            .Select(a => new { a.Id, a.Code, a.Name, a.Class })
            .ToListAsync(ct);

        var accountIds = accounts.Select(a => a.Id).ToHashSet();

        // Sum debits and credits per account in the period.
        var txQuery = db.Set<AccountTransaction>().AsNoTracking()
            .Where(t => accountIds.Contains(t.AccountId)
                     && t.OperationDate >= request.From
                     && t.OperationDate < request.To);

        var grouped = await txQuery
            .GroupBy(t => new { t.AccountId, t.Side })
            .Select(g => new { g.Key.AccountId, g.Key.Side, Total = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        // Per-account net effect.
        // Revenue: credit increases revenue, debit reduces it. Net = credits - debits.
        // Expense: debit increases expense, credit reduces it. Net = debits - credits.
        var revenue = new List<PnlAccountRow>();
        var expense = new List<PnlAccountRow>();
        foreach (var a in accounts)
        {
            var debits = grouped.Where(g => g.AccountId == a.Id && g.Side == LedgerSide.Debit).Sum(g => g.Total);
            var credits = grouped.Where(g => g.AccountId == a.Id && g.Side == LedgerSide.Credit).Sum(g => g.Total);
            var net = a.Class == AccountClass.Revenue ? credits - debits : debits - credits;
            if (net == 0m) continue;
            var row = new PnlAccountRow(a.Code, a.Name, net);
            (a.Class == AccountClass.Revenue ? revenue : expense).Add(row);
        }

        revenue = revenue.OrderBy(r => r.Code).ToList();
        expense = expense.OrderBy(r => r.Code).ToList();

        var totalRev = revenue.Sum(r => r.Amount);
        var totalExp = expense.Sum(e => e.Amount);

        return new ProfitAndLossDto(
            request.From, request.To,
            revenue, totalRev,
            expense, totalExp,
            totalRev - totalExp);
    }
}
