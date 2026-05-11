using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Sales.Application.CashRegisters.Dtos;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.CashRegisters.Queries;

public sealed record GetCashRegistersQuery(Guid? BranchId, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<CashRegisterDto>>;

public sealed class GetCashRegistersHandler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetCashRegistersQuery, IReadOnlyList<CashRegisterDto>>
{
    public async Task<IReadOnlyList<CashRegisterDto>> Handle(GetCashRegistersQuery request, CancellationToken ct)
    {
        var query = db.Set<CashRegister>().AsNoTracking();
        if (request.BranchId.HasValue)
        {
            currentUser.EnsureCanAccess(request.BranchId.Value);
            query = query.Where(r => r.BranchId == request.BranchId.Value);
        }
        else if (!currentUser.CanAccessAllBranches)
        {
            var allowed = currentUser.BranchIds;
            query = query.Where(r => allowed.Contains(r.BranchId));
        }
        if (!request.IncludeInactive) query = query.Where(r => r.IsActive);

        var rows = await (
            from r in query
            join b in db.Set<Branch>().AsNoTracking() on r.BranchId equals b.Id
            orderby b.Name, r.Code
            select new { r.Id, r.BranchId, BranchName = b.Name, r.Code, r.Name, r.IsActive })
            .ToListAsync(ct);

        // Resolve active shift per register in one pass.
        var registerIds = rows.Select(r => r.Id).ToList();
        var activeShifts = await db.Set<CashRegisterShift>().AsNoTracking()
            .Where(s => s.Status == ShiftStatus.Open && registerIds.Contains(s.CashRegisterId))
            .Select(s => new { s.CashRegisterId, s.Id })
            .ToDictionaryAsync(x => x.CashRegisterId, x => (Guid?)x.Id, ct);

        return rows.Select(r => new CashRegisterDto(
            r.Id, r.BranchId, r.BranchName, r.Code, r.Name, r.IsActive,
            activeShifts.GetValueOrDefault(r.Id))).ToList();
    }
}
