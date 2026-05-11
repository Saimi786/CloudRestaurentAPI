using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Domain.Tenants;

/// <summary>
/// Tenant-wide configuration knobs that previously lived as hardcoded constants
/// — currency / timezone / fiscal year / reference number prefixes / reward points.
/// One row per Tenant (TenantId is the primary key). Seeded with sensible Pakistan
/// defaults on tenant creation; tenant admins edit through the Settings page.
/// </summary>
public class BusinessSettings : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }

    // ---- General ----
    public string DefaultCurrency { get; private set; } = "PKR";
    public string DefaultTimezone { get; private set; } = "Asia/Karachi";
    public int FiscalYearStartMonth { get; private set; } = 7;
    public int FiscalYearStartDay { get; private set; } = 1;

    // ---- Tax ----
    public string TaxLabel { get; private set; } = "Tax";
    public Guid? DefaultTaxRateId { get; private set; }

    // ---- Reward Points (mirrors UltimatePOS's business table layout) ----
    public bool RewardPointsEnabled { get; private set; }
    public string RewardPointsName { get; private set; } = "Points";

    /// <summary>
    /// Currency amount that earns 1 reward point (UP's `amount_for_unit_rp`).
    /// E.g. 100 means a 1,500 order earns 15 points.
    /// </summary>
    public decimal RewardPointsAmountPerPoint { get; private set; } = 1m;

    /// <summary>Minimum order grand-total for the customer to earn points on it.</summary>
    public decimal RewardPointsMinOrderForEarn { get; private set; }

    /// <summary>Cap on points earned per single order (null = no cap).</summary>
    public int? RewardPointsMaxPerOrder { get; private set; }

    /// <summary>Currency value of 1 point at redemption (UP's `redeem_amount_per_unit_rp`).</summary>
    public decimal RewardPointsRedeemValue { get; private set; } = 0.01m;

    /// <summary>Minimum order grand-total before redemption is allowed.</summary>
    public decimal RewardPointsMinOrderForRedeem { get; private set; }

    /// <summary>Customer needs at least this many points to start redeeming.</summary>
    public int? RewardPointsMinRedeem { get; private set; }

    /// <summary>Cap on points used in a single redemption (null = unlimited).</summary>
    public int? RewardPointsMaxRedeem { get; private set; }

    /// <summary>Expiry period — combined with <see cref="RewardPointsExpiryUnit"/>. Null = no expiry.</summary>
    public int? RewardPointsExpiryPeriod { get; private set; }
    public RewardPointsExpiryUnit RewardPointsExpiryUnit { get; private set; } = RewardPointsExpiryUnit.Year;

    // ---- Reference number prefixes ----
    public string SalesPrefix { get; private set; } = "SAL";
    public string PurchasePrefix { get; private set; } = "PO";
    public string ExpensePrefix { get; private set; } = "EXP";
    public string CustomerPrefix { get; private set; } = "CUS";

    // ---- POS behavior toggles ----
    public bool PosShowStockLevel { get; private set; } = true;

    private BusinessSettings() { }

    public BusinessSettings(Guid id, Guid tenantId)
    {
        Id = id;
        TenantId = tenantId;
    }

    public void UpdateGeneral(string currency, string timezone, int fyMonth, int fyDay)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code.", nameof(currency));
        if (fyMonth is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(fyMonth));
        if (fyDay is < 1 or > 31) throw new ArgumentOutOfRangeException(nameof(fyDay));

        DefaultCurrency = currency.ToUpperInvariant();
        DefaultTimezone = timezone;
        FiscalYearStartMonth = fyMonth;
        FiscalYearStartDay = fyDay;
    }

    public void UpdateTax(string label, Guid? defaultTaxRateId)
    {
        TaxLabel = string.IsNullOrWhiteSpace(label) ? "Tax" : label.Trim();
        DefaultTaxRateId = defaultTaxRateId;
    }

    public void UpdateRewardPoints(
        bool enabled, string name,
        decimal amountPerPoint, decimal minOrderForEarn, int? maxPerOrder,
        decimal redeemValue, decimal minOrderForRedeem, int? minRedeem, int? maxRedeem,
        int? expiryPeriod, RewardPointsExpiryUnit expiryUnit)
    {
        if (amountPerPoint <= 0) throw new ArgumentOutOfRangeException(nameof(amountPerPoint),
            "Amount per point must be > 0 — otherwise every order earns infinite points.");
        if (redeemValue < 0) throw new ArgumentOutOfRangeException(nameof(redeemValue));
        if (minOrderForEarn < 0) throw new ArgumentOutOfRangeException(nameof(minOrderForEarn));
        if (minOrderForRedeem < 0) throw new ArgumentOutOfRangeException(nameof(minOrderForRedeem));
        if (maxPerOrder is < 0) throw new ArgumentOutOfRangeException(nameof(maxPerOrder));
        if (minRedeem is < 0) throw new ArgumentOutOfRangeException(nameof(minRedeem));
        if (maxRedeem is < 0) throw new ArgumentOutOfRangeException(nameof(maxRedeem));
        if (expiryPeriod is < 0) throw new ArgumentOutOfRangeException(nameof(expiryPeriod));

        RewardPointsEnabled = enabled;
        RewardPointsName = string.IsNullOrWhiteSpace(name) ? "Points" : name.Trim();
        RewardPointsAmountPerPoint = amountPerPoint;
        RewardPointsMinOrderForEarn = minOrderForEarn;
        RewardPointsMaxPerOrder = maxPerOrder;
        RewardPointsRedeemValue = redeemValue;
        RewardPointsMinOrderForRedeem = minOrderForRedeem;
        RewardPointsMinRedeem = minRedeem;
        RewardPointsMaxRedeem = maxRedeem;
        RewardPointsExpiryPeriod = expiryPeriod;
        RewardPointsExpiryUnit = expiryUnit;
    }

    public void UpdatePrefixes(string sales, string purchase, string expense, string customer)
    {
        SalesPrefix = NormalizePrefix(sales, "SAL");
        PurchasePrefix = NormalizePrefix(purchase, "PO");
        ExpensePrefix = NormalizePrefix(expense, "EXP");
        CustomerPrefix = NormalizePrefix(customer, "CUS");
    }

    public void UpdatePos(bool showStockLevel) => PosShowStockLevel = showStockLevel;

    private static string NormalizePrefix(string? raw, string fallback)
    {
        var v = raw?.Trim();
        if (string.IsNullOrWhiteSpace(v)) return fallback;
        if (v.Length > 8) throw new ArgumentException("Prefix cannot exceed 8 characters.");
        return v.ToUpperInvariant();
    }
}

public enum RewardPointsExpiryUnit
{
    Day = 0,
    Month = 1,
    Year = 2
}
