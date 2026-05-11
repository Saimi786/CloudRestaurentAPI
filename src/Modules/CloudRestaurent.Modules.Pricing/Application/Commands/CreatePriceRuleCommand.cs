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
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Pricing.Application.Commands;

public sealed record CreatePriceRuleCommand(
    Guid ProductId,
    Guid? BranchId,
    string Name,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DaysOfWeekFlags DaysOfWeek,
    decimal OverridePriceAmount,
    string OverridePriceCurrency,
    int Priority) : IRequest<PriceRuleDto>;

public sealed class CreatePriceRuleValidator : AbstractValidator<CreatePriceRuleCommand>
{
    public CreatePriceRuleValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.OverridePriceAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OverridePriceCurrency).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(c => c.StartTime.HasValue == c.EndTime.HasValue)
            .WithMessage("Start and end times must both be set or both empty.");
    }
}

public sealed class CreatePriceRuleHandler(IAppDbContext db, ITenantContext tenantContext, IMediator mediator)
    : IRequestHandler<CreatePriceRuleCommand, PriceRuleDto>
{
    public async Task<PriceRuleDto> Handle(CreatePriceRuleCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (!await db.Set<Product>().AnyAsync(p => p.Id == request.ProductId, ct))
            throw new NotFoundException("Product", request.ProductId);

        if (request.BranchId is { } bid &&
            !await db.Set<Branch>().AnyAsync(b => b.Id == bid, ct))
            throw new NotFoundException("Branch", bid);

        var rule = new PriceRule(Guid.NewGuid(), tenantId, request.ProductId, request.BranchId,
            request.Name.Trim(), request.StartTime, request.EndTime, request.DaysOfWeek,
            new Money(request.OverridePriceAmount, request.OverridePriceCurrency.ToUpperInvariant()),
            request.Priority);

        db.Set<PriceRule>().Add(rule);
        await db.SaveChangesAsync(ct);

        return await mediator.Send(new GetPriceRuleByIdQuery(rule.Id), ct);
    }
}
