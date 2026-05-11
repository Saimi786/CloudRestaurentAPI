using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Sales.Application.Common;
using CloudRestaurent.Modules.Sales.Application.Dtos;
using CloudRestaurent.Domain.Common;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Sales.Application.Commands;

public sealed record AddPaymentCommand(
    Guid OrderId,
    PaymentMethod Method,
    decimal Amount,
    string? Reference) : IRequest<OrderDto>;

public sealed class AddPaymentValidator : AbstractValidator<AddPaymentCommand>
{
    public AddPaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Method).IsInEnum();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reference).MaximumLength(100);
    }
}

public sealed class AddPaymentHandler(IAppDbContext db, ICurrentUser user)
    : IRequestHandler<AddPaymentCommand, OrderDto>
{
    public async Task<OrderDto> Handle(AddPaymentCommand request, CancellationToken ct)
    {
        var order = await db.Set<Order>().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            ?? throw new NotFoundException("Order", request.OrderId);

        if (order.Status != OrderStatus.Open)
            throw new BusinessRuleException("Cannot add payment to a non-open order.");

        var paymentId = Guid.NewGuid();
        db.Set<Payment>().Add(new Payment(
            paymentId, order.Id, request.Method,
            new Money(request.Amount, order.Currency),
            string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            DateTimeOffset.UtcNow));

        // If this user has an open shift on a register at this branch, record a Sale movement
        // so the till's expected balance reflects the cash collected. Cash-only — card/wallet
        // don't touch the cash drawer.
        if (request.Method == PaymentMethod.Cash && user.UserId is { } userId)
        {
            var shift = await db.Set<CashRegisterShift>()
                .FirstOrDefaultAsync(s =>
                    s.Status == ShiftStatus.Open &&
                    s.OpenedByUserId == userId &&
                    s.BranchId == order.BranchId, ct);
            shift?.RecordMovement(
                ShiftMovementType.Sale, request.Amount, paymentId,
                order.OrderNumber, $"Order {order.OrderNumber ?? order.Id.ToString()}");
        }

        await db.SaveChangesAsync(ct);
        return await OrderDtoBuilder.BuildAsync(db, order, ct);
    }
}
