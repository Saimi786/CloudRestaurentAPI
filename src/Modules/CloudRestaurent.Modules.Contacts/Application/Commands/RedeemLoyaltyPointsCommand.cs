using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Application.Common;
using CloudRestaurent.Modules.Contacts.Application.Dtos;
using CloudRestaurent.Modules.Contacts.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.Commands;

public sealed record RedeemLoyaltyPointsCommand(Guid Id, int Points) : IRequest<CustomerDto>;

public sealed class RedeemLoyaltyPointsValidator : AbstractValidator<RedeemLoyaltyPointsCommand>
{
    public RedeemLoyaltyPointsValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Points).GreaterThan(0);
    }
}

public sealed class RedeemLoyaltyPointsHandler(IAppDbContext db)
    : IRequestHandler<RedeemLoyaltyPointsCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(RedeemLoyaltyPointsCommand request, CancellationToken ct)
    {
        var customer = await db.Set<Customer>().FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException("Customer", request.Id);

        if (!customer.IsActive)
            throw new BusinessRuleException("Cannot adjust loyalty points on an inactive customer.");

        if (request.Points > customer.TotalRewardPoints)
            throw new BusinessRuleException(
                $"Insufficient reward balance. Has {customer.TotalRewardPoints}, attempted to redeem {request.Points}.");

        customer.RedeemPoints(request.Points);
        await db.SaveChangesAsync(ct);

        return CustomerDtoBuilder.Build(db, customer);
    }
}
