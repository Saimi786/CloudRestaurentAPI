using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Accounting.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Accounting.Application.Expenses;

public sealed record GetExpensesQuery(
    Guid? BranchId, DateTimeOffset? From, DateTimeOffset? To, int Take = 200)
    : IRequest<IReadOnlyList<ExpenseDto>>;

public sealed class GetExpensesHandler(IAppDbContext db, IIdentityService identity, ITenantContext tenant)
    : IRequestHandler<GetExpensesQuery, IReadOnlyList<ExpenseDto>>
{
    public async Task<IReadOnlyList<ExpenseDto>> Handle(GetExpensesQuery request, CancellationToken ct)
    {
        var q = db.Set<Expense>().AsNoTracking();
        if (request.BranchId.HasValue) q = q.Where(e => e.BranchId == request.BranchId.Value);
        if (request.From.HasValue) q = q.Where(e => e.OccurredAt >= request.From.Value);
        if (request.To.HasValue) q = q.Where(e => e.OccurredAt < request.To.Value);

        var rows = await (
            from e in q.OrderByDescending(e => e.OccurredAt).Take(request.Take)
            join b in db.Set<Branch>().AsNoTracking() on e.BranchId equals b.Id
            join a in db.Set<Account>().AsNoTracking() on e.ExpenseAccountId equals a.Id
            select new
            {
                e.Id, e.BranchId, BranchName = b.Name,
                e.ExpenseAccountId, AccountCode = a.Code, AccountName = a.Name,
                e.Reference, e.Description, e.Amount, e.Currency,
                e.Method, e.OccurredAt, e.CreatedByUserId
            }).ToListAsync(ct);

        var nameLookup = tenant.TenantId is { } tid
            ? (await identity.ListUsersAsync(tid, true, ct)).ToDictionary(u => u.Id, u => u.FullName)
            : new();

        return rows.Select(r => new ExpenseDto(
            r.Id, r.BranchId, r.BranchName,
            r.ExpenseAccountId, r.AccountCode, r.AccountName,
            r.Reference, r.Description, r.Amount, r.Currency,
            r.Method, r.Method.ToString(),
            r.OccurredAt, r.CreatedByUserId,
            nameLookup.GetValueOrDefault(r.CreatedByUserId, "—"))).ToList();
    }
}
