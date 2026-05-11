using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Contacts.Domain;

public class Customer : AuditableEntity<Guid>, ITenantScoped
{
    public Guid TenantId { get; set; }
    public ContactType Type { get; private set; }
    public string FullName { get; private set; } = null!;
    public string? SupplierBusinessName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Notes { get; private set; }
    public Money OpeningBalance { get; private set; }
    public Money CurrentBalance { get; private set; }
    public Money? CreditLimit { get; private set; }
    public Address BillingAddress { get; private set; }
    public Address ShippingAddress { get; private set; }
    public Guid? CustomerGroupId { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public Gender? Gender { get; private set; }
    /// <summary>Current reward-points balance (UP's `total_rp`).</summary>
    public int TotalRewardPoints { get; private set; }

    /// <summary>Lifetime points redeemed by this customer (UP's `total_rp_used`).</summary>
    public int TotalRewardPointsUsed { get; private set; }

    /// <summary>Lifetime points expired without being redeemed (UP's `total_rp_expired`).</summary>
    public int TotalRewardPointsExpired { get; private set; }

    public bool IsActive { get; private set; }

    private Customer() { }

    public Customer(Guid id, Guid tenantId, string fullName, string? phone, string? email)
    {
        Id = id;
        TenantId = tenantId;
        Type = ContactType.Customer;
        FullName = fullName;
        Phone = phone;
        Email = email;
        OpeningBalance = Money.Zero("PKR");
        CurrentBalance = Money.Zero("PKR");
        BillingAddress = default;
        ShippingAddress = default;
        TotalRewardPoints = 0;
        TotalRewardPointsUsed = 0;
        TotalRewardPointsExpired = 0;
        IsActive = true;
    }

    public void Update(string fullName, string? phone, string? email, string? notes)
    {
        FullName = fullName;
        Phone = phone;
        Email = email;
        Notes = notes;
    }

    public void SetType(ContactType type) => Type = type;
    public void SetSupplierBusinessName(string? name) => SupplierBusinessName = name;
    public void SetTaxNumber(string? tax) => TaxNumber = tax;
    public void SetBillingAddress(Address address) => BillingAddress = address;
    public void SetShippingAddress(Address address) => ShippingAddress = address;
    public void SetCustomerGroup(Guid? groupId) => CustomerGroupId = groupId;
    public void SetDateOfBirth(DateOnly? dob) => DateOfBirth = dob;
    public void SetGender(Gender? gender) => Gender = gender;
    public void SetCreditLimit(Money? limit) => CreditLimit = limit;

    /// <summary>
    /// Set the opening balance once at migration / contact creation time. Updating
    /// this after transactions exist would silently corrupt A/R or A/P — guard.
    /// </summary>
    public void SetOpeningBalance(Money balance)
    {
        OpeningBalance = balance;
        // CurrentBalance starts at the opening balance; subsequent transactions adjust it.
        if (CurrentBalance.Amount == 0m)
            CurrentBalance = balance;
    }

    public void AdjustBalance(Money delta) => CurrentBalance = CurrentBalance.Add(delta);

    /// <summary>
    /// Add (or subtract, on refund) earned reward points. Delta can be negative; refusing
    /// to drop below zero protects against bugs in refund recalculation.
    /// </summary>
    public void ApplyEarnedDelta(int delta)
    {
        if (TotalRewardPoints + delta < 0)
            throw new InvalidOperationException(
                $"Earned delta {delta} would push reward balance negative (current {TotalRewardPoints}).");
        TotalRewardPoints += delta;
    }

    /// <summary>
    /// Deduct points spent on a redemption. Increments the lifetime-used counter and
    /// reduces the current balance — raises if the customer can't afford it.
    /// </summary>
    public void RedeemPoints(int points)
    {
        if (points <= 0) throw new ArgumentOutOfRangeException(nameof(points), "Redeem amount must be positive.");
        if (points > TotalRewardPoints)
            throw new InvalidOperationException(
                $"Insufficient reward balance. Has {TotalRewardPoints}, attempted to redeem {points}.");
        TotalRewardPoints -= points;
        TotalRewardPointsUsed += points;
    }

    /// <summary>
    /// Reverse a redemption (used during refunds). Adds back to the current balance and
    /// decrements the used counter; never goes negative.
    /// </summary>
    public void ReverseRedemption(int points)
    {
        if (points <= 0) throw new ArgumentOutOfRangeException(nameof(points));
        TotalRewardPoints += points;
        TotalRewardPointsUsed = Math.Max(0, TotalRewardPointsUsed - points);
    }

    /// <summary>
    /// Expire stale earned points — moves them out of the balance into the lifetime-expired
    /// counter. Called by the daily background job.
    /// </summary>
    public void ExpirePoints(int points)
    {
        if (points <= 0) return;
        var actual = Math.Min(points, TotalRewardPoints);
        TotalRewardPoints -= actual;
        TotalRewardPointsExpired += actual;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
