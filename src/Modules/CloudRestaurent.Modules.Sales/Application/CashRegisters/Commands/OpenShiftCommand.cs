using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.CashRegisters.Dtos;
using CloudRestaurent.Modules.Sales.Application.CashRegisters.Queries;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.CashRegisters.Commands;

public sealed record OpenShiftCommand(Guid CashRegisterId, decimal OpeningAmount, string Currency)
    : IRequest<CashRegisterShiftDto>;

public sealed class OpenShiftValidator : AbstractValidator<OpenShiftCommand>
{
    public OpenShiftValidator()
    {
        RuleFor(x => x.CashRegisterId).NotEmpty();
        RuleFor(x => x.OpeningAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches(@"^[A-Z]{3}$");
    }
}

public sealed class OpenShiftHandler(
    IAppDbContext db, ITenantContext tenant, ICurrentUser user, IMediator mediator)
    : IRequestHandler<OpenShiftCommand, CashRegisterShiftDto>
{
    public async Task<CashRegisterShiftDto> Handle(OpenShiftCommand request, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");
        var userId = user.UserId
            ?? throw new UnauthorizedException("No authenticated user.");

        var register = await db.Set<CashRegister>().FirstOrDefaultAsync(r => r.Id == request.CashRegisterId, ct)
            ?? throw new NotFoundException("CashRegister", request.CashRegisterId);
        if (!register.IsActive)
            throw new ConflictException("Register is inactive.");

        user.EnsureCanAccess(register.BranchId);

        if (await db.Set<CashRegisterShift>().AnyAsync(s =>
                s.CashRegisterId == register.Id && s.Status == ShiftStatus.Open, ct))
            throw new ConflictException("This register already has an open shift.");

        if (await db.Set<CashRegisterShift>().AnyAsync(s =>
                s.OpenedByUserId == userId && s.Status == ShiftStatus.Open, ct))
            throw new ConflictException("You already have an open shift on another register. Close it first.");

        var shift = new CashRegisterShift(
            Guid.NewGuid(), tenantId, register.Id, register.BranchId,
            userId, request.OpeningAmount, request.Currency.ToUpperInvariant());

        db.Set<CashRegisterShift>().Add(shift);
        await db.SaveChangesAsync(ct);

        return await mediator.Send(new GetShiftByIdQuery(shift.Id), ct);
    }
}
