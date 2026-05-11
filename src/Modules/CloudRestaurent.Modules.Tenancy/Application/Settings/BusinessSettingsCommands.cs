using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Tenants;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Settings;

public sealed record BusinessSettingsDto(
    // General
    string DefaultCurrency,
    string DefaultTimezone,
    int FiscalYearStartMonth,
    int FiscalYearStartDay,
    // Tax
    string TaxLabel,
    Guid? DefaultTaxRateId,
    // Reward points (UP-aligned schema)
    bool RewardPointsEnabled,
    string RewardPointsName,
    decimal RewardPointsAmountPerPoint,
    decimal RewardPointsMinOrderForEarn,
    int? RewardPointsMaxPerOrder,
    decimal RewardPointsRedeemValue,
    decimal RewardPointsMinOrderForRedeem,
    int? RewardPointsMinRedeem,
    int? RewardPointsMaxRedeem,
    int? RewardPointsExpiryPeriod,
    int RewardPointsExpiryUnit,
    // Prefixes
    string SalesPrefix,
    string PurchasePrefix,
    string ExpensePrefix,
    string CustomerPrefix,
    // POS
    bool PosShowStockLevel);

public sealed record GetBusinessSettingsQuery : IRequest<BusinessSettingsDto>;

public sealed record UpdateBusinessSettingsCommand(
    string DefaultCurrency,
    string DefaultTimezone,
    int FiscalYearStartMonth,
    int FiscalYearStartDay,
    string TaxLabel,
    Guid? DefaultTaxRateId,
    bool RewardPointsEnabled,
    string RewardPointsName,
    decimal RewardPointsAmountPerPoint,
    decimal RewardPointsMinOrderForEarn,
    int? RewardPointsMaxPerOrder,
    decimal RewardPointsRedeemValue,
    decimal RewardPointsMinOrderForRedeem,
    int? RewardPointsMinRedeem,
    int? RewardPointsMaxRedeem,
    int? RewardPointsExpiryPeriod,
    int RewardPointsExpiryUnit,
    string SalesPrefix,
    string PurchasePrefix,
    string ExpensePrefix,
    string CustomerPrefix,
    bool PosShowStockLevel) : IRequest<BusinessSettingsDto>;

public sealed class UpdateBusinessSettingsValidator : AbstractValidator<UpdateBusinessSettingsCommand>
{
    public UpdateBusinessSettingsValidator()
    {
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3).Matches(@"^[A-Za-z]{3}$");
        RuleFor(x => x.DefaultTimezone).NotEmpty().MaximumLength(64);
        RuleFor(x => x.FiscalYearStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.FiscalYearStartDay).InclusiveBetween(1, 31);
        RuleFor(x => x.TaxLabel).NotEmpty().MaximumLength(32);
        RuleFor(x => x.RewardPointsName).NotEmpty().MaximumLength(32);
        RuleFor(x => x.RewardPointsAmountPerPoint).GreaterThan(0);
        RuleFor(x => x.RewardPointsMinOrderForEarn).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RewardPointsMaxPerOrder).GreaterThanOrEqualTo(0).When(x => x.RewardPointsMaxPerOrder.HasValue);
        RuleFor(x => x.RewardPointsRedeemValue).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RewardPointsMinOrderForRedeem).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RewardPointsMinRedeem).GreaterThanOrEqualTo(0).When(x => x.RewardPointsMinRedeem.HasValue);
        RuleFor(x => x.RewardPointsMaxRedeem).GreaterThanOrEqualTo(0).When(x => x.RewardPointsMaxRedeem.HasValue);
        RuleFor(x => x.RewardPointsExpiryPeriod).GreaterThanOrEqualTo(0).When(x => x.RewardPointsExpiryPeriod.HasValue);
        RuleFor(x => x.RewardPointsExpiryUnit).InclusiveBetween(0, 2);
        RuleFor(x => x.SalesPrefix).NotEmpty().MaximumLength(8);
        RuleFor(x => x.PurchasePrefix).NotEmpty().MaximumLength(8);
        RuleFor(x => x.ExpensePrefix).NotEmpty().MaximumLength(8);
        RuleFor(x => x.CustomerPrefix).NotEmpty().MaximumLength(8);
    }
}

public sealed class GetBusinessSettingsHandler(IAppDbContext db, ITenantContext tenant)
    : IRequestHandler<GetBusinessSettingsQuery, BusinessSettingsDto>
{
    public async Task<BusinessSettingsDto> Handle(GetBusinessSettingsQuery _, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var s = await GetOrCreateAsync(db, tenantId, ct);
        return ToDto(s);
    }

    internal static async Task<BusinessSettings> GetOrCreateAsync(
        IAppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var existing = await db.Set<BusinessSettings>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);
        if (existing is not null) return existing;

        // Lazy-seed default settings on first read so we don't have to backfill
        // a row for every existing tenant at migration time.
        var seeded = new BusinessSettings(Guid.NewGuid(), tenantId);
        db.Set<BusinessSettings>().Add(seeded);
        await db.SaveChangesAsync(ct);
        return seeded;
    }

    internal static BusinessSettingsDto ToDto(BusinessSettings s) =>
        new(s.DefaultCurrency, s.DefaultTimezone, s.FiscalYearStartMonth, s.FiscalYearStartDay,
            s.TaxLabel, s.DefaultTaxRateId,
            s.RewardPointsEnabled, s.RewardPointsName,
            s.RewardPointsAmountPerPoint, s.RewardPointsMinOrderForEarn, s.RewardPointsMaxPerOrder,
            s.RewardPointsRedeemValue, s.RewardPointsMinOrderForRedeem,
            s.RewardPointsMinRedeem, s.RewardPointsMaxRedeem,
            s.RewardPointsExpiryPeriod, (int)s.RewardPointsExpiryUnit,
            s.SalesPrefix, s.PurchasePrefix, s.ExpensePrefix, s.CustomerPrefix,
            s.PosShowStockLevel);
}

public sealed class UpdateBusinessSettingsHandler(IAppDbContext db, ITenantContext tenant)
    : IRequestHandler<UpdateBusinessSettingsCommand, BusinessSettingsDto>
{
    public async Task<BusinessSettingsDto> Handle(UpdateBusinessSettingsCommand req, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var s = await GetBusinessSettingsHandler.GetOrCreateAsync(db, tenantId, ct);

        s.UpdateGeneral(req.DefaultCurrency, req.DefaultTimezone,
            req.FiscalYearStartMonth, req.FiscalYearStartDay);
        s.UpdateTax(req.TaxLabel, req.DefaultTaxRateId);
        s.UpdateRewardPoints(
            req.RewardPointsEnabled, req.RewardPointsName,
            req.RewardPointsAmountPerPoint, req.RewardPointsMinOrderForEarn, req.RewardPointsMaxPerOrder,
            req.RewardPointsRedeemValue, req.RewardPointsMinOrderForRedeem,
            req.RewardPointsMinRedeem, req.RewardPointsMaxRedeem,
            req.RewardPointsExpiryPeriod, (RewardPointsExpiryUnit)req.RewardPointsExpiryUnit);
        s.UpdatePrefixes(req.SalesPrefix, req.PurchasePrefix, req.ExpensePrefix, req.CustomerPrefix);
        s.UpdatePos(req.PosShowStockLevel);

        await db.SaveChangesAsync(ct);
        return GetBusinessSettingsHandler.ToDto(s);
    }
}
