using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Tenants;
using CloudRestaurent.Modules.SaaS.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.SaaS.Application;

public sealed record PackageDto(
    Guid Id, string Code, string Name, SubscriptionPlan Plan, string PlanName,
    BillingInterval Interval, string IntervalName,
    decimal Price, string Currency,
    int MaxBranches, int MaxUsers, int? StorageGb,
    string? FeatureNotes, bool IsActive);

public sealed record GetPackagesQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<PackageDto>>;

public sealed class GetPackagesHandler(IAppDbContext db) : IRequestHandler<GetPackagesQuery, IReadOnlyList<PackageDto>>
{
    public async Task<IReadOnlyList<PackageDto>> Handle(GetPackagesQuery request, CancellationToken ct)
    {
        var q = db.Set<Package>().AsNoTracking();
        if (!request.IncludeInactive) q = q.Where(p => p.IsActive);
        var rows = await q.OrderBy(p => p.Plan).ThenBy(p => p.Interval).ToListAsync(ct);
        return rows.Select(p => new PackageDto(
            p.Id, p.Code, p.Name, p.Plan, p.Plan.ToString(),
            p.Interval, p.Interval.ToString(),
            p.Price, p.Currency, p.MaxBranches, p.MaxUsers, p.StorageGb,
            p.FeatureNotes, p.IsActive)).ToList();
    }
}

public sealed record CreatePackageCommand(
    string Code, string Name, SubscriptionPlan Plan, BillingInterval Interval,
    decimal Price, string Currency, int MaxBranches, int MaxUsers, int? StorageGb,
    string? FeatureNotes) : IRequest<PackageDto>;

public sealed class CreatePackageValidator : AbstractValidator<CreatePackageCommand>
{
    public CreatePackageValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches(@"^[A-Z]{3}$");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxBranches).GreaterThan(0);
        RuleFor(x => x.MaxUsers).GreaterThan(0);
        RuleFor(x => x.FeatureNotes).MaximumLength(2000);
    }
}

public sealed class CreatePackageHandler(IAppDbContext db) : IRequestHandler<CreatePackageCommand, PackageDto>
{
    public async Task<PackageDto> Handle(CreatePackageCommand request, CancellationToken ct)
    {
        if (await db.Set<Package>().AnyAsync(p => p.Code == request.Code, ct))
            throw new ConflictException($"Package code '{request.Code}' already exists.");

        var pkg = new Package(
            Guid.NewGuid(), request.Code, request.Name, request.Plan, request.Interval,
            request.Price, request.Currency.ToUpperInvariant(),
            request.MaxBranches, request.MaxUsers, request.StorageGb, request.FeatureNotes);
        db.Set<Package>().Add(pkg);
        await db.SaveChangesAsync(ct);
        return new PackageDto(
            pkg.Id, pkg.Code, pkg.Name, pkg.Plan, pkg.Plan.ToString(),
            pkg.Interval, pkg.Interval.ToString(),
            pkg.Price, pkg.Currency, pkg.MaxBranches, pkg.MaxUsers, pkg.StorageGb,
            pkg.FeatureNotes, pkg.IsActive);
    }
}
