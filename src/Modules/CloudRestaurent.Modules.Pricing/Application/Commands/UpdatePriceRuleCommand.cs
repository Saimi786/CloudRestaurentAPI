using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Pricing.Application.Dtos;
using CloudRestaurent.Modules.Pricing.Application.Queries;
using CloudRestaurent.Domain.Common;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Pricing.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Pricing.Application.Commands;

public sealed record UpdatePriceRuleCommand(
    Guid Id,
    Guid? BranchId,
    string Name,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DaysOfWeekFlags DaysOfWeek,
    decimal OverridePriceAmount,
    string OverridePriceCurrency,
    int Priority) : IRequest<PriceRuleDto>;

public sealed class UpdatePriceRuleValidator : AbstractValidator<UpdatePriceRuleCommand>
{
    public UpdatePriceRuleValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.OverridePriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OverridePriceCurrency).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(c => c.StartTime.HasValue == c.EndTime.HasValue);
    }
}

public sealed class UpdatePriceRuleHandler(IAppDbContext db, IMediator mediator)
    : IRequestHandler<UpdatePriceRuleCommand, PriceRuleDto>
{
    public async Task<PriceRuleDto> Handle(UpdatePriceRuleCommand request, CancellationToken ct)
    {
        var rule = await db.Set<PriceRule>().FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            ?? throw new NotFoundException("PriceRule", request.Id);

        if (request.BranchId is { } bid &&
            !await db.Set<Branch>().AnyAsync(b => b.Id == bid, ct))
            throw new NotFoundException("Branch", bid);

        rule.Update(request.BranchId, request.Name.Trim(),
            request.StartTime, request.EndTime, request.DaysOfWeek,
            new Money(request.OverridePriceAmount, request.OverridePriceCurrency.ToUpperInvariant()),
            request.Priority);

        await db.SaveChangesAsync(ct);
        return await mediator.Send(new GetPriceRuleByIdQuery(rule.Id), ct);
    }
}
