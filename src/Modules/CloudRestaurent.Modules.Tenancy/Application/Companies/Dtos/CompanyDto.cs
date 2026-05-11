namespace CloudRestaurent.Modules.Tenancy.Application.Companies.Dtos;

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string LegalName,
    string DefaultCurrency,
    string? TaxRegistrationNumber,
    bool IsActive);
