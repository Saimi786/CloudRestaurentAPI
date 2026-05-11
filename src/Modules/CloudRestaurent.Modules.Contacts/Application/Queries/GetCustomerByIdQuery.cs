using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Contacts.Application.Dtos;
using CloudRestaurent.Modules.Contacts.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.Queries;

public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;

public sealed class GetCustomerByIdHandler(IAppDbContext db)
    : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var dto = await db.Set<Customer>().AsNoTracking()
            .Where(c => c.Id == request.Id)
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
            ?? throw new NotFoundException("Customer", request.Id);
        return dto;
    }
}
