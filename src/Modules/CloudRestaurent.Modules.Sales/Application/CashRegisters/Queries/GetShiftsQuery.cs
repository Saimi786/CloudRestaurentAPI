using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Sales.Application.CashRegisters.Dtos;
using CloudRestaurent.Modules.Sales.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.CashRegisters.Queries;

public sealed record GetShiftsQuery(Guid? CashRegisterId, ShiftStatus? Status, int Take = 100)
    : IRequest<IReadOnlyList<CashRegisterShiftSummaryDto>>;

public sealed class GetShiftsHandler(
    IAppDbContext db,
    IIdentityService identity,
    ITenantContext tenant)
    : IRequestHandler<GetShiftsQuery, IReadOnlyList<CashRegisterShiftSummaryDto>>
{
    public async Task<IReadOnlyList<CashRegisterShiftSummaryDto>> Handle(GetShiftsQuery request, CancellationToken ct)
    {
        var q = db.Set<CashRegisterShift>().AsNoTracking();
        if (request.CashRegisterId.HasValue) q = q.Where(s => s.CashRegisterId == request.CashRegisterId.Value);
        if (request.Status.HasValue) q = q.Where(s => s.Status == request.Status.Value);

        var rows = await (
            from s in q.OrderByDescending(s => s.OpenedAt).Take(request.Take)
            join r in db.Set<CashRegister>().AsNoTracking() on s.CashRegisterId equals r.Id
            select new
            {
                s.Id, s.CashRegisterId, RegCode = r.Code,
                s.OpenedByUserId, s.OpenedAt, s.ClosedAt,
                s.OpeningAmount, s.DeclaredClosingAmount, s.OverShortAmount,
                s.Status
            }).ToListAsync(ct);

        var nameLookup = await BuildUserNameLookup(identity, tenant, ct);

        return rows.Select(r => new CashRegisterShiftSummaryDto(
            r.Id, r.CashRegisterId, r.RegCode,
            nameLookup.GetValueOrDefault(r.OpenedByUserId, "—"),
            r.OpenedAt, r.ClosedAt,
            r.OpeningAmount, r.DeclaredClosingAmount, r.OverShortAmount,
            r.Status, r.Status.ToString())).ToList();
    }

    internal static async Task<Dictionary<Guid, string>> BuildUserNameLookup(
        IIdentityService identity, ITenantContext tenant, CancellationToken ct)
    {
        if (tenant.TenantId is not { } tid) return new();
        var users = await identity.ListUsersAsync(tid, includeInactive: true, ct);
        return users.ToDictionary(u => u.Id, u => u.FullName);
    }
}

public sealed record GetShiftByIdQuery(Guid Id) : IRequest<CashRegisterShiftDto>;

public sealed class GetShiftByIdHandler(
    IAppDbContext db,
    IIdentityService identity,
    ITenantContext tenant)
    : IRequestHandler<GetShiftByIdQuery, CashRegisterShiftDto>
{
    public async Task<CashRegisterShiftDto> Handle(GetShiftByIdQuery request, CancellationToken ct)
    {
        var s = await db.Set<CashRegisterShift>().AsNoTracking()
            .Include(x => x.Movements)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException("CashRegisterShift", request.Id);

        var register = await db.Set<CashRegister>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == s.CashRegisterId, ct)
            ?? throw new NotFoundException("CashRegister", s.CashRegisterId);

        var branch = await db.Set<Branch>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == s.BranchId, ct);

        var nameLookup = await GetShiftsHandler.BuildUserNameLookup(identity, tenant, ct);

        var movements = s.Movements.OrderBy(m => m.CreatedAt).Select(m =>
            new CashRegisterShiftMovementDto(
                m.Id, m.Type, m.Type.ToString(), m.Amount,
                m.SourceId, m.Reference, m.Notes, m.CreatedAt)).ToList();

        decimal Total(ShiftMovementType t) => s.Movements.Where(m => m.Type == t).Sum(m => m.Amount);

        return new CashRegisterShiftDto(
            s.Id, s.CashRegisterId, register.Code, register.Name,
            s.BranchId, branch?.Name ?? "—",
            s.OpenedByUserId, nameLookup.GetValueOrDefault(s.OpenedByUserId, "—"),
            s.OpenedAt, s.OpeningAmount, s.Currency,
            s.ClosedByUserId,
            s.ClosedByUserId.HasValue ? nameLookup.GetValueOrDefault(s.ClosedByUserId.Value) : null,
            s.ClosedAt,
            s.DeclaredClosingAmount, s.ExpectedClosingAmount, s.OverShortAmount,
            s.Notes, s.Status, s.Status.ToString(),
            Total(ShiftMovementType.Sale),
            Total(ShiftMovementType.Refund),
            Total(ShiftMovementType.PaidOut),
            Total(ShiftMovementType.CashIn),
            Total(ShiftMovementType.CashOut),
            movements);
    }
}
