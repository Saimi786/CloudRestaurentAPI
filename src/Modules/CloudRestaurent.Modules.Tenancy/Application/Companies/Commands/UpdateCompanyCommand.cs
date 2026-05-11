using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Modules.Tenancy.Application.Companies.Dtos;
using CloudRestaurent.Domain.Companies;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Tenancy.Application.Companies.Commands;

public sealed record UpdateCompanyCommand(
    Guid Id,
    string Name,
    string LegalName,
    string DefaultCurrency,
    string? TaxRegistrationNumber) : IRequest<CompanyDto>;

public sealed class UpdateCompanyValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DefaultCurrency).NotEmpty().Length(3).Matches("^[A-Z]{3}$");
        RuleFor(x => x.TaxRegistrationNumber).MaximumLength(50);
    }
}

public sealed class UpdateCompanyHandler(IAppDbContext db)
    : IRequestHandler<UpdateCompanyCommand, CompanyDto>
{
    public async Task<CompanyDto> Handle(UpdateCompanyCommand request, CancellationToken ct)
    {
        // SuperAdmin-only endpoint; cross-tenant edits are intentional, so bypass the
        // per-tenant query filter. Uniqueness check is scoped to the company's tenant.
        var company = await db.Set<Company>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException("Company", request.Id);

        var nameTaken = await db.Set<Company>().IgnoreQueryFilters()
            .AnyAsync(c => c.Id != request.Id
                && c.TenantId == company.TenantId
                && c.Name == request.Name, ct);
        if (nameTaken)
            throw new ConflictException($"A company named '{request.Name}' already exists in this tenant.");

        company.Update(request.Name, request.LegalName, request.DefaultCurrency, request.TaxRegistrationNumber);
        await db.SaveChangesAsync(ct);

        return new CompanyDto(company.Id, company.Name, company.LegalName,
            company.DefaultCurrency, company.TaxRegistrationNumber, company.IsActive);
    }
}
