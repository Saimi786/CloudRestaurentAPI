using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Contacts.Application.Dtos;
using CloudRestaurent.Modules.Contacts.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.Queries;

public sealed record GetCustomersQuery(
    string? Search = null,
    ContactType? Type = null,
    Guid? CustomerGroupId = null,
    bool IncludeInactive = false) : IRequest<IReadOnlyList<CustomerDto>>;

public sealed class GetCustomersHandler(IAppDbContext db)
    : IRequestHandler<GetCustomersQuery, IReadOnlyList<CustomerDto>>
{
    public async Task<IReadOnlyList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var customers = db.Set<Customer>().AsNoTracking();
        if (!request.IncludeInactive) customers = customers.Where(c => c.IsActive);
        if (request.Type is { } t) customers = customers.Where(c => c.Type == t);
        if (request.CustomerGroupId is { } gid) customers = customers.Where(c => c.CustomerGroupId == gid);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            customers = customers.Where(c =>
                c.FullName.Contains(s) ||
                (c.Phone != null && c.Phone.Contains(s)) ||
                (c.Email != null && c.Email.Contains(s)) ||
                (c.SupplierBusinessName != null && c.SupplierBusinessName.Contains(s)));
        }

        return await customers
            .OrderBy(c => c.FullName)
            .Take(200)
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
            .ToListAsync(ct);
    }
}
