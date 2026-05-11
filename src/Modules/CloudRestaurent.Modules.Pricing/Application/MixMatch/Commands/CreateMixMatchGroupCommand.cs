using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Pricing.Application.MixMatch.Dtos;
using CloudRestaurent.Modules.Pricing.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Pricing.Application.MixMatch.Commands;

public sealed record CreateMixMatchGroupCommand(
    string Name,
    MixMatchType Type,
    int Quantity,
    decimal DiscountValue,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DaysOfWeekFlags DaysOfWeek,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int Priority,
    bool Stackable,
    IReadOnlyList<Guid> ProductIds) : IRequest<MixMatchGroupDetailDto>;

public sealed class CreateMixMatchGroupValidator : AbstractValidator<CreateMixMatchGroupCommand>
{
    public CreateMixMatchGroupValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.DiscountValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100).When(x => x.Type == MixMatchType.PercentDiscount)
            .WithMessage("Percent discount must be 0-100.");
        RuleFor(x => x.ProductIds).NotNull();
    }
}

public sealed class CreateMixMatchGroupHandler(IAppDbContext db, ITenantContext tenantContext, IMediator mediator)
    : IRequestHandler<CreateMixMatchGroupCommand, MixMatchGroupDetailDto>
{
    public async Task<MixMatchGroupDetailDto> Handle(CreateMixMatchGroupCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        if (await db.Set<MixMatchGroup>().AnyAsync(g => g.Name == request.Name, ct))
            throw new ConflictException($"A mix-match group named '{request.Name}' already exists.");

        var group = new MixMatchGroup(
            Guid.NewGuid(), tenantId, request.Name,
            request.Type, request.Quantity, request.DiscountValue);
        group.SetDateWindow(request.StartDate, request.EndDate);
        group.SetTimeWindow(request.StartTime, request.EndTime);
        group.SetDaysOfWeek(request.DaysOfWeek == 0 ? DaysOfWeekFlags.All : request.DaysOfWeek);
        group.SetStackable(request.Stackable);
        group.Update(request.Name, request.Type, request.Quantity, request.DiscountValue, request.Priority);
        group.ReplaceProducts(request.ProductIds);

        db.Set<MixMatchGroup>().Add(group);
        await db.SaveChangesAsync(ct);

        return await mediator.Send(new Queries.GetMixMatchGroupByIdQuery(group.Id), ct);
    }
}
