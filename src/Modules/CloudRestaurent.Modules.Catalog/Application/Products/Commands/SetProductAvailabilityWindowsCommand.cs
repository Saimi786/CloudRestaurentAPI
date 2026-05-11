using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Catalog.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Product = CloudRestaurent.Modules.Catalog.Domain.Product;

namespace CloudRestaurent.Modules.Catalog.Application.Products.Commands;

public sealed record AvailabilityWindowInput(
    string Name, DaysOfWeekFlags DaysOfWeek, TimeOnly StartTime, TimeOnly EndTime);

public sealed record SetProductAvailabilityWindowsCommand(
    Guid ProductId, IReadOnlyList<AvailabilityWindowInput> Windows) : IRequest;

public sealed class SetProductAvailabilityWindowsValidator : AbstractValidator<SetProductAvailabilityWindowsCommand>
{
    public SetProductAvailabilityWindowsValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Windows).NotNull();
        RuleForEach(x => x.Windows).ChildRules(w =>
        {
            w.RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
            w.RuleFor(x => x.DaysOfWeek).Must(d => d != DaysOfWeekFlags.None)
                .WithMessage("At least one day must be selected.");
        });
    }
}

public sealed class SetProductAvailabilityWindowsHandler(IAppDbContext db, ITenantContext tenant)
    : IRequestHandler<SetProductAvailabilityWindowsCommand>
{
    public async Task Handle(SetProductAvailabilityWindowsCommand request, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var product = await db.Set<Product>().FirstOrDefaultAsync(p => p.Id == request.ProductId, ct)
            ?? throw new NotFoundException("Product", request.ProductId);

        // Replace wholesale.
        await db.Set<ProductAvailabilityWindow>()
            .Where(w => w.ProductId == product.Id)
            .ExecuteDeleteAsync(ct);

        foreach (var w in request.Windows)
        {
            db.Set<ProductAvailabilityWindow>().Add(new ProductAvailabilityWindow(
                Guid.NewGuid(), tenantId, product.Id, w.Name,
                w.DaysOfWeek, w.StartTime, w.EndTime));
        }
        await db.SaveChangesAsync(ct);
    }
}

public sealed record GetProductAvailabilityWindowsQuery(Guid ProductId)
    : IRequest<IReadOnlyList<AvailabilityWindowDto>>;

public sealed record AvailabilityWindowDto(
    Guid Id, string Name, DaysOfWeekFlags DaysOfWeek,
    TimeOnly StartTime, TimeOnly EndTime, bool IsActive);

public sealed class GetProductAvailabilityWindowsHandler(IAppDbContext db)
    : IRequestHandler<GetProductAvailabilityWindowsQuery, IReadOnlyList<AvailabilityWindowDto>>
{
    public async Task<IReadOnlyList<AvailabilityWindowDto>> Handle(GetProductAvailabilityWindowsQuery request, CancellationToken ct)
    {
        return await db.Set<ProductAvailabilityWindow>().AsNoTracking()
            .Where(w => w.ProductId == request.ProductId)
            .OrderBy(w => w.StartTime)
            .Select(w => new AvailabilityWindowDto(
                w.Id, w.Name, w.DaysOfWeek, w.StartTime, w.EndTime, w.IsActive))
            .ToListAsync(ct);
    }
}
