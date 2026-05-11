using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Pricing.Application.MixMatch.Dtos;
using CloudRestaurent.Modules.Pricing.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Pricing.Application.MixMatch.Commands;

public sealed record UpdateMixMatchGroupCommand(
    Guid Id,
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

public sealed class UpdateMixMatchGroupValidator : AbstractValidator<UpdateMixMatchGroupCommand>
{
    public UpdateMixMatchGroupValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.DiscountValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiscountValue)
            .LessThanOrEqualTo(100).When(x => x.Type == MixMatchType.PercentDiscount);
        RuleFor(x => x.ProductIds).NotNull();
    }
}

public sealed class UpdateMixMatchGroupHandler(IAppDbContext db, IMediator mediator)
    : IRequestHandler<UpdateMixMatchGroupCommand, MixMatchGroupDetailDto>
{
    public async Task<MixMatchGroupDetailDto> Handle(UpdateMixMatchGroupCommand request, CancellationToken ct)
    {
        var group = await db.Set<MixMatchGroup>()
            .Include(g => g.Products)
            .FirstOrDefaultAsync(g => g.Id == request.Id, ct)
            ?? throw new NotFoundException("MixMatchGroup", request.Id);

        if (await db.Set<MixMatchGroup>()
                .AnyAsync(g => g.Id != request.Id && g.Name == request.Name, ct))
            throw new ConflictException($"A mix-match group named '{request.Name}' already exists.");

        group.Update(request.Name, request.Type, request.Quantity, request.DiscountValue, request.Priority);
        group.SetDateWindow(request.StartDate, request.EndDate);
        group.SetTimeWindow(request.StartTime, request.EndTime);
        group.SetDaysOfWeek(request.DaysOfWeek == 0 ? DaysOfWeekFlags.All : request.DaysOfWeek);
        group.SetStackable(request.Stackable);

        // Replace product attachments wholesale — easier than diffing add/remove for v1.
        await db.Set<MixMatchProduct>()
            .Where(p => p.MixMatchGroupId == group.Id)
            .ExecuteDeleteAsync(ct);
        foreach (var pid in request.ProductIds.Distinct())
            db.Set<MixMatchProduct>().Add(new MixMatchProduct(Guid.NewGuid(), group.Id, pid));

        await db.SaveChangesAsync(ct);
        return await mediator.Send(new Queries.GetMixMatchGroupByIdQuery(group.Id), ct);
    }
}
