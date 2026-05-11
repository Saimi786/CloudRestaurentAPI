using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Application.Common;
using CloudRestaurent.Modules.Contacts.Application.Dtos;
using CloudRestaurent.Modules.Contacts.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.Commands;

public sealed record EarnLoyaltyPointsCommand(Guid Id, int Points) : IRequest<CustomerDto>;

public sealed class EarnLoyaltyPointsValidator : AbstractValidator<EarnLoyaltyPointsCommand>
{
    public EarnLoyaltyPointsValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Points).GreaterThan(0).LessThanOrEqualTo(100_000)
            .WithMessage("Earn amount must be between 1 and 100,000.");
    }
}

public sealed class EarnLoyaltyPointsHandler(IAppDbContext db)
    : IRequestHandler<EarnLoyaltyPointsCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(EarnLoyaltyPointsCommand request, CancellationToken ct)
    {
        var customer = await db.Set<Customer>().FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException("Customer", request.Id);

        if (!customer.IsActive)
            throw new BusinessRuleException("Cannot adjust loyalty points on an inactive customer.");

        customer.ApplyEarnedDelta(request.Points);
        await db.SaveChangesAsync(ct);

        return CustomerDtoBuilder.Build(db, customer);
    }
}
