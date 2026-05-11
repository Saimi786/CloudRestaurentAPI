using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.CashRegisters.Dtos;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.CashRegisters.Commands;

public sealed record CreateCashRegisterCommand(Guid BranchId, string Code, string Name)
    : IRequest<CashRegisterDto>;

public sealed class CreateCashRegisterValidator : AbstractValidator<CreateCashRegisterCommand>
{
    public CreateCashRegisterValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
    }
}

public sealed class CreateCashRegisterHandler(IAppDbContext db, ITenantContext tenant)
    : IRequestHandler<CreateCashRegisterCommand, CashRegisterDto>
{
    public async Task<CashRegisterDto> Handle(CreateCashRegisterCommand request, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (await db.Set<CashRegister>().AnyAsync(r =>
                r.BranchId == request.BranchId && r.Code == request.Code, ct))
            throw new ConflictException($"Register code '{request.Code}' is already used at this branch.");

        var register = new CashRegister(Guid.NewGuid(), tenantId, request.BranchId, request.Code, request.Name);
        db.Set<CashRegister>().Add(register);
        await db.SaveChangesAsync(ct);

        return new CashRegisterDto(
            register.Id, register.BranchId, "", register.Code, register.Name, register.IsActive, null);
    }
}

public sealed record UpdateCashRegisterCommand(Guid Id, string Code, string Name) : IRequest;

public sealed class UpdateCashRegisterHandler(IAppDbContext db) : IRequestHandler<UpdateCashRegisterCommand>
{
    public async Task Handle(UpdateCashRegisterCommand request, CancellationToken ct)
    {
        var register = await db.Set<CashRegister>().FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException("CashRegister", request.Id);
        register.Update(request.Code, request.Name);
        await db.SaveChangesAsync(ct);
    }
}

public sealed record DeactivateCashRegisterCommand(Guid Id) : IRequest;

public sealed class DeactivateCashRegisterHandler(IAppDbContext db)
    : IRequestHandler<DeactivateCashRegisterCommand>
{
    public async Task Handle(DeactivateCashRegisterCommand request, CancellationToken ct)
    {
        var register = await db.Set<CashRegister>().FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException("CashRegister", request.Id);

        if (await db.Set<CashRegisterShift>().AnyAsync(s =>
                s.CashRegisterId == register.Id && s.Status == ShiftStatus.Open, ct))
            throw new ConflictException("Cannot deactivate: register has an open shift. Close it first.");

        register.Deactivate();
        await db.SaveChangesAsync(ct);
    }
}
