using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.CashRegisters.Dtos;
using CloudRestaurent.Modules.Sales.Application.CashRegisters.Queries;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.CashRegisters.Commands;

public sealed record CloseShiftCommand(Guid Id, decimal DeclaredClosingAmount, string? Notes)
    : IRequest<CashRegisterShiftDto>;

public sealed class CloseShiftValidator : AbstractValidator<CloseShiftCommand>
{
    public CloseShiftValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.DeclaredClosingAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class CloseShiftHandler(IAppDbContext db, ICurrentUser user, IMediator mediator)
    : IRequestHandler<CloseShiftCommand, CashRegisterShiftDto>
{
    public async Task<CashRegisterShiftDto> Handle(CloseShiftCommand request, CancellationToken ct)
    {
        var userId = user.UserId
            ?? throw new UnauthorizedException("No authenticated user.");

        var shift = await db.Set<CashRegisterShift>()
            .Include(s => s.Movements)
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct)
            ?? throw new NotFoundException("CashRegisterShift", request.Id);

        shift.Close(userId, request.DeclaredClosingAmount, request.Notes);
        await db.SaveChangesAsync(ct);

        return await mediator.Send(new GetShiftByIdQuery(shift.Id), ct);
    }
}

public sealed record AddShiftMovementCommand(
    Guid ShiftId, ShiftMovementType Type, decimal Amount, string? Reference, string? Notes)
    : IRequest;

public sealed class AddShiftMovementValidator : AbstractValidator<AddShiftMovementCommand>
{
    public AddShiftMovementValidator()
    {
        RuleFor(x => x.ShiftId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reference).MaximumLength(120);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.Type).Must(t => t is ShiftMovementType.PaidOut
                                            or ShiftMovementType.CashIn
                                            or ShiftMovementType.CashOut)
            .WithMessage("Sale and Refund movements are recorded automatically — only manual types are allowed here.");
    }
}

public sealed class AddShiftMovementHandler(IAppDbContext db) : IRequestHandler<AddShiftMovementCommand>
{
    public async Task Handle(AddShiftMovementCommand request, CancellationToken ct)
    {
        var shift = await db.Set<CashRegisterShift>()
            .Include(s => s.Movements)
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
            ?? throw new NotFoundException("CashRegisterShift", request.ShiftId);

        shift.RecordMovement(request.Type, request.Amount, sourceId: null, request.Reference, request.Notes);
        await db.SaveChangesAsync(ct);
    }
}
