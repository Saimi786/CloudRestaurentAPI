using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Accounting.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Accounting.Application.Reports;

public sealed record ExpenseCategoryRow(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    int Count,
    decimal Total);

public sealed record ExpenseByCategoryDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalCount,
    decimal Grand,
    IReadOnlyList<ExpenseCategoryRow> ByCategory);

/// <summary>
/// Expense Report — groups Expense rows by their ExpenseAccount. We use the actual
/// Expense entity (not GL postings) so Reference / Description / Method context is
/// available when the user drills down — but the summary numbers will agree with
/// the P&amp;L because both flow through the same ledger.
/// </summary>
public sealed record GetExpenseByCategoryQuery(
    DateTimeOffset From, DateTimeOffset To, Guid? BranchId)
    : IRequest<ExpenseByCategoryDto>;

public sealed class GetExpenseByCategoryHandler(IAppDbContext db)
    : IRequestHandler<GetExpenseByCategoryQuery, ExpenseByCategoryDto>
{
    public async Task<ExpenseByCategoryDto> Handle(GetExpenseByCategoryQuery request, CancellationToken ct)
    {
        var query = db.Set<Expense>().AsNoTracking()
            .Where(e => e.OccurredAt >= request.From && e.OccurredAt < request.To);
        if (request.BranchId.HasValue)
            query = query.Where(e => e.BranchId == request.BranchId.Value);

        var rows = await query
            .GroupBy(e => e.ExpenseAccountId)
            .Select(g => new { AccountId = g.Key, Count = g.Count(), Total = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var accountIds = rows.Select(r => r.AccountId).ToHashSet();
        var accounts = await db.Set<Account>().AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Code, a.Name })
            .ToDictionaryAsync(a => a.Id, ct);

        var byCategory = rows
            .Select(r =>
            {
                var a = accounts.GetValueOrDefault(r.AccountId);
                return new ExpenseCategoryRow(
                    r.AccountId,
                    a?.Code ?? "?",
                    a?.Name ?? "(unknown)",
                    r.Count,
                    r.Total);
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        return new ExpenseByCategoryDto(
            request.From, request.To,
            byCategory.Sum(r => r.Count),
            byCategory.Sum(r => r.Total),
            byCategory);
    }
}
