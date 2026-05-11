using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Contacts.Application.Dtos;
using CloudRestaurent.Modules.Contacts.Domain;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace CloudRestaurent.Modules.Contacts.Application.Common;

internal static class CustomerDtoBuilder
{
    /// <summary>
    /// Build a CustomerDto from a tracked Customer entity. Reads shadow CreditLimit
    /// columns via the EntityEntry — Customer.CreditLimit is Ignore'd in EF
    /// (see CustomerConfiguration) because EF Core 10 doesn't model nullable Money? as
    /// a complex property cleanly.
    /// </summary>
    public static CustomerDto Build(IAppDbContext db, Customer c)
    {
        var entry = db.Entry(c);
        var creditAmount = entry.Property("CreditLimitAmount").CurrentValue as decimal?;
        var creditCurrency = entry.Property("CreditLimitCurrency").CurrentValue as string;

        return new CustomerDto(
            c.Id,
            (int)c.Type, c.Type.ToString(),
            c.FullName, c.SupplierBusinessName,
            c.Phone, c.Email, c.TaxNumber, c.Notes,
            c.OpeningBalance.Amount, c.OpeningBalance.Currency,
            c.CurrentBalance.Amount, c.CurrentBalance.Currency,
            creditAmount, creditCurrency,
            ToAddressDto(c.BillingAddress),
            ToAddressDto(c.ShippingAddress),
            c.CustomerGroupId,
            c.DateOfBirth,
            c.Gender.HasValue ? (int?)c.Gender.Value : null,
            c.Gender?.ToString(),
            c.TotalRewardPoints, c.TotalRewardPointsUsed, c.TotalRewardPointsExpired,
            c.IsActive, c.CreatedAt);
    }

    private static AddressDto ToAddressDto(Address a) =>
        new(a.Line1, a.Line2, a.City, a.State, a.Country, a.PostalCode);
}
