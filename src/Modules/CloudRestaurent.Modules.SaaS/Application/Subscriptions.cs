using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Tenants;
using CloudRestaurent.Modules.SaaS.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.SaaS.Application;

public sealed record SubscriptionDto(
    Guid Id, Guid TenantId, string TenantName,
    Guid PackageId, string PackageName, string PackageCode,
    SubscriptionStatus Status, string StatusName,
    DateOnly? StartDate, DateOnly? EndDate, DateOnly? NextBillingDate,
    Guid? CouponId, decimal? AppliedDiscountPercent,
    Guid? RequestedByUserId, Guid? ApprovedByUserId, DateTimeOffset? ApprovedAt,
    string? Notes, DateTimeOffset CreatedAt);

public sealed record RequestSubscriptionCommand(
    Guid TenantId, Guid PackageId, string? CouponCode, string? Notes) : IRequest<SubscriptionDto>;

public sealed class RequestSubscriptionValidator : AbstractValidator<RequestSubscriptionCommand>
{
    public RequestSubscriptionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.PackageId).NotEmpty();
        RuleFor(x => x.CouponCode).MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class RequestSubscriptionHandler(
    IAppDbContext db, ICurrentUser user, IMediator mediator)
    : IRequestHandler<RequestSubscriptionCommand, SubscriptionDto>
{
    public async Task<SubscriptionDto> Handle(RequestSubscriptionCommand request, CancellationToken ct)
    {
        var userId = user.UserId
            ?? throw new UnauthorizedException("No authenticated user.");

        var pkg = await db.Set<Package>().FirstOrDefaultAsync(p => p.Id == request.PackageId, ct)
            ?? throw new NotFoundException("Package", request.PackageId);
        if (!pkg.IsActive)
            throw new BusinessRuleException($"Package '{pkg.Name}' is inactive.");

        if (await db.Set<Subscription>().IgnoreQueryFilters()
                .AnyAsync(s => s.TenantId == request.TenantId
                    && (s.Status == SubscriptionStatus.PendingApproval || s.Status == SubscriptionStatus.Active), ct))
            throw new ConflictException("This tenant already has an active or pending subscription.");

        Guid? couponId = null;
        decimal? discount = null;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var coupon = await db.Set<Coupon>()
                .FirstOrDefaultAsync(c => c.Code == request.CouponCode, ct)
                ?? throw new NotFoundException("Coupon", request.CouponCode);
            if (!coupon.IsRedeemable(DateOnly.FromDateTime(DateTime.UtcNow)))
                throw new BusinessRuleException("Coupon is not redeemable.");
            coupon.Redeem();
            couponId = coupon.Id;
            discount = coupon.DiscountPercent;
        }

        var sub = Subscription.Request(
            Guid.NewGuid(), request.TenantId, request.PackageId,
            couponId, discount, userId, request.Notes);
        db.Set<Subscription>().Add(sub);
        await db.SaveChangesAsync(ct);

        return await mediator.Send(new GetSubscriptionByIdQuery(sub.Id), ct);
    }
}

public sealed record ApproveSubscriptionCommand(
    Guid Id, DateOnly StartDate, DateOnly NextBillingDate) : IRequest<SubscriptionDto>;

public sealed class ApproveSubscriptionHandler(
    IAppDbContext db, ICurrentUser user, IMediator mediator)
    : IRequestHandler<ApproveSubscriptionCommand, SubscriptionDto>
{
    public async Task<SubscriptionDto> Handle(ApproveSubscriptionCommand request, CancellationToken ct)
    {
        var userId = user.UserId
            ?? throw new UnauthorizedException("No authenticated user.");

        var sub = await db.Set<Subscription>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct)
            ?? throw new NotFoundException("Subscription", request.Id);
        sub.Approve(userId, request.StartDate, request.NextBillingDate);

        // Reflect Plan onto Tenant so existing module-tier checks see the new tier.
        var pkg = await db.Set<Package>().FirstOrDefaultAsync(p => p.Id == sub.PackageId, ct);
        if (pkg is not null)
        {
            var tenant = await db.Set<Tenant>().IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == sub.TenantId, ct);
            tenant?.ChangePlan(pkg.Plan);
        }

        await db.SaveChangesAsync(ct);
        return await mediator.Send(new GetSubscriptionByIdQuery(sub.Id), ct);
    }
}

public sealed record CancelSubscriptionCommand(Guid Id, DateOnly EndDate, string? Reason) : IRequest;

public sealed class CancelSubscriptionHandler(IAppDbContext db) : IRequestHandler<CancelSubscriptionCommand>
{
    public async Task Handle(CancelSubscriptionCommand request, CancellationToken ct)
    {
        var sub = await db.Set<Subscription>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct)
            ?? throw new NotFoundException("Subscription", request.Id);
        sub.Cancel(request.EndDate, request.Reason);
        await db.SaveChangesAsync(ct);
    }
}

public sealed record GetSubscriptionsQuery(SubscriptionStatus? Status = null, int Take = 200)
    : IRequest<IReadOnlyList<SubscriptionDto>>;

public sealed class GetSubscriptionsHandler(IAppDbContext db) : IRequestHandler<GetSubscriptionsQuery, IReadOnlyList<SubscriptionDto>>
{
    public async Task<IReadOnlyList<SubscriptionDto>> Handle(GetSubscriptionsQuery request, CancellationToken ct)
    {
        var q = db.Set<Subscription>().IgnoreQueryFilters().AsNoTracking();
        if (request.Status is { } s) q = q.Where(x => x.Status == s);
        var rows = await (
            from sub in q.OrderByDescending(s => s.CreatedAt).Take(request.Take)
            join t in db.Set<Tenant>().IgnoreQueryFilters().AsNoTracking() on sub.TenantId equals t.Id
            join p in db.Set<Package>().AsNoTracking() on sub.PackageId equals p.Id
            select new
            {
                sub.Id, sub.TenantId, TenantName = t.Name,
                sub.PackageId, PackageName = p.Name, PackageCode = p.Code,
                sub.Status, sub.StartDate, sub.EndDate, sub.NextBillingDate,
                sub.CouponId, sub.AppliedDiscountPercent,
                sub.RequestedByUserId, sub.ApprovedByUserId, sub.ApprovedAt,
                sub.Notes, sub.CreatedAt
            }).ToListAsync(ct);

        return rows.Select(r => new SubscriptionDto(
            r.Id, r.TenantId, r.TenantName,
            r.PackageId, r.PackageName, r.PackageCode,
            r.Status, r.Status.ToString(),
            r.StartDate, r.EndDate, r.NextBillingDate,
            r.CouponId, r.AppliedDiscountPercent,
            r.RequestedByUserId, r.ApprovedByUserId, r.ApprovedAt,
            r.Notes, r.CreatedAt)).ToList();
    }
}

public sealed record GetSubscriptionByIdQuery(Guid Id) : IRequest<SubscriptionDto>;

public sealed class GetSubscriptionByIdHandler(IAppDbContext db) : IRequestHandler<GetSubscriptionByIdQuery, SubscriptionDto>
{
    public async Task<SubscriptionDto> Handle(GetSubscriptionByIdQuery request, CancellationToken ct)
    {
        var sub = await db.Set<Subscription>().IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.Id, ct)
            ?? throw new NotFoundException("Subscription", request.Id);
        var t = await db.Set<Tenant>().IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sub.TenantId, ct);
        var p = await db.Set<Package>().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sub.PackageId, ct);
        return new SubscriptionDto(
            sub.Id, sub.TenantId, t?.Name ?? "—",
            sub.PackageId, p?.Name ?? "—", p?.Code ?? "—",
            sub.Status, sub.Status.ToString(),
            sub.StartDate, sub.EndDate, sub.NextBillingDate,
            sub.CouponId, sub.AppliedDiscountPercent,
            sub.RequestedByUserId, sub.ApprovedByUserId, sub.ApprovedAt,
            sub.Notes, sub.CreatedAt);
    }
}
