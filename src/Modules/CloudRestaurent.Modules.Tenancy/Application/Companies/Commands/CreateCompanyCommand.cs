using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tenancy.Application.Companies.Dtos;
using CloudRestaurent.Domain.Companies;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Companies.Commands;

public sealed record CreateCompanyCommand(
    string Name,
    string LegalName,
    string DefaultCurrency,
    string? TaxRegistrationNumber) : IRequest<CompanyDto>;

public sealed class CreateCompanyValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3).Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a 3-letter ISO 4217 code (e.g. PKR, USD).");
        RuleFor(x => x.TaxRegistrationNumber).MaximumLength(50);
    }
}

public sealed class CreateCompanyHandler(IAppDbContext db, ITenantContext tenantContext)
    : IRequestHandler<CreateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(CreateCompanyCommand request, CancellationToken ct)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");

        var nameTaken = await db.Set<Company>()
            .AnyAsync(c => c.Name == request.Name, ct);
        if (nameTaken)
            throw new ConflictException($"A company named '{request.Name}' already exists.");

        var company = new Company(
            id: Guid.NewGuid(),
            tenantId: tenantId,
            name: request.Name,
            legalName: request.LegalName,
            defaultCurrency: request.DefaultCurrency);
        company.SetTaxRegistration(request.TaxRegistrationNumber);

        db.Set<Company>().Add(company);
        await db.SaveChangesAsync(ct);

        return new CompanyDto(company.Id, company.Name, company.LegalName,
            company.DefaultCurrency, company.TaxRegistrationNumber, company.IsActive);
    }
}
