using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Application.Dtos;
using CloudRestaurent.Modules.Contacts.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.Queries;

/// <summary>
/// Optimized lookup for the POS hot path: cashier types phone, gets the customer.
/// Returns 404 if no match — uniqueness on (TenantId, Phone) guarantees at most one.
/// </summary>
public sealed record GetCustomerByPhoneQuery(string Phone) : IRequest<CustomerDto>;

public sealed class GetCustomerByPhoneHandler(IAppDbContext db)
    : IRequestHandler<GetCustomerByPhoneQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerByPhoneQuery request, CancellationToken ct)
    {
        var phone = request.Phone.Trim();
        var dto = await db.Set<Customer>().AsNoTracking()
            .Where(c => c.IsActive && c.Phone == phone)
            .Select(c => new CustomerDto(
                c.Id,
                (int)c.Type, c.Type.ToString(),
                c.FullName, c.SupplierBusinessName,
                c.Phone, c.Email, c.TaxNumber, c.Notes,
                c.OpeningBalance.Amount, c.OpeningBalance.Currency,
                c.CurrentBalance.Amount, c.CurrentBalance.Currency,
                EF.Property<decimal?>(c, "CreditLimitAmount"),
                EF.Property<string?>(c, "CreditLimitCurrency"),
                new AddressDto(c.BillingAddress.Line1, c.BillingAddress.Line2,
                    c.BillingAddress.City, c.BillingAddress.State,
                    c.BillingAddress.Country, c.BillingAddress.PostalCode),
                new AddressDto(c.ShippingAddress.Line1, c.ShippingAddress.Line2,
                    c.ShippingAddress.City, c.ShippingAddress.State,
                    c.ShippingAddress.Country, c.ShippingAddress.PostalCode),
                c.CustomerGroupId,
                c.DateOfBirth,
                c.Gender == null ? null : (int?)c.Gender,
                c.Gender == null ? null : c.Gender.ToString(),
                c.TotalRewardPoints, c.TotalRewardPointsUsed, c.TotalRewardPointsExpired,
                c.IsActive, c.CreatedAt))
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Customer (by phone)", phone);
        return dto;
    }
}
