using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Common;
using CloudRestaurent.Modules.Contacts.Application.Common;
using CloudRestaurent.Modules.Contacts.Application.Dtos;
using CloudRestaurent.Modules.Contacts.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Contacts.Application.Commands;

public sealed record UpdateCustomerCommand(
    Guid Id,
    string FullName,
    string? Phone,
    string? Email,
    string? Notes,
    ContactType Type = ContactType.Customer,
    string? SupplierBusinessName = null,
    string? TaxNumber = null,
    decimal? CreditLimitAmount = null,
    string? CreditLimitCurrency = null,
    AddressDto? BillingAddress = null,
    AddressDto? ShippingAddress = null,
    Guid? CustomerGroupId = null,
    DateOnly? DateOfBirth = null,
    Gender? Gender = null) : IRequest<CustomerDto>;

public sealed class UpdateCustomerValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(50);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .MaximumLength(256);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.SupplierBusinessName).MaximumLength(200);
        RuleFor(x => x.TaxNumber).MaximumLength(50);
        RuleFor(x => x.CreditLimitCurrency).Length(3).Matches("^[A-Z]{3}$")
            .When(x => !string.IsNullOrEmpty(x.CreditLimitCurrency));
        RuleFor(x => x.CreditLimitAmount).GreaterThanOrEqualTo(0).When(x => x.CreditLimitAmount.HasValue);
        RuleFor(x => x.SupplierBusinessName).NotEmpty()
            .When(x => x.Type == ContactType.Supplier || x.Type == ContactType.Both)
            .WithMessage("SupplierBusinessName is required for suppliers.");
    }
}

public sealed class UpdateCustomerHandler(IAppDbContext db) : IRequestHandler<UpdateCustomerCommand, CustomerDto>
{
    public async Task<CustomerDto> Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var customer = await db.Set<Customer>().FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException("Customer", request.Id);

        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();

        if (phone is not null &&
            await db.Set<Customer>().AnyAsync(c => c.Id != request.Id && c.Phone == phone, ct))
            throw new ConflictException($"A customer with phone '{phone}' already exists.");

        if (request.CustomerGroupId is { } gid &&
            !await db.Set<CustomerGroup>().AnyAsync(g => g.Id == gid, ct))
            throw new NotFoundException("CustomerGroup", gid);

        customer.Update(request.FullName.Trim(), phone, email,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim());

        customer.SetType(request.Type);
        customer.SetSupplierBusinessName(string.IsNullOrWhiteSpace(request.SupplierBusinessName)
            ? null : request.SupplierBusinessName.Trim());
        customer.SetTaxNumber(string.IsNullOrWhiteSpace(request.TaxNumber) ? null : request.TaxNumber.Trim());
        customer.SetCustomerGroup(request.CustomerGroupId);
        customer.SetDateOfBirth(request.DateOfBirth);
        customer.SetGender(request.Gender);

        if (request.BillingAddress is { } billing)
            customer.SetBillingAddress(new Address(
                billing.Line1, billing.Line2, billing.City, billing.State, billing.Country, billing.PostalCode));
        if (request.ShippingAddress is { } shipping)
            customer.SetShippingAddress(new Address(
                shipping.Line1, shipping.Line2, shipping.City, shipping.State, shipping.Country, shipping.PostalCode));

        if (request.CreditLimitAmount.HasValue && !string.IsNullOrEmpty(request.CreditLimitCurrency))
            customer.SetCreditLimit(new Money(request.CreditLimitAmount.Value, request.CreditLimitCurrency));
        else
            customer.SetCreditLimit(null);

        var entry = db.Entry(customer);
        entry.Property("CreditLimitAmount").CurrentValue = customer.CreditLimit?.Amount;
        entry.Property("CreditLimitCurrency").CurrentValue = customer.CreditLimit?.Currency;

        await db.SaveChangesAsync(ct);

        return CustomerDtoBuilder.Build(db, customer);
    }
}
