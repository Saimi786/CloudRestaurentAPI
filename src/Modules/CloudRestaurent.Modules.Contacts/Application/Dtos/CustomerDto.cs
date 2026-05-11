namespace CloudRestaurent.Modules.Contacts.Application.Dtos;

public sealed record AddressDto(
    string? Line1,
    string? Line2,
    string? City,
    string? State,
    string? Country,
    string? PostalCode);

public sealed record CustomerDto(
    Guid Id,
    int Type,
    string TypeName,
    string FullName,
    string? SupplierBusinessName,
    string? Phone,
    string? Email,
    string? TaxNumber,
    string? Notes,
    decimal OpeningBalanceAmount,
    string OpeningBalanceCurrency,
    decimal CurrentBalanceAmount,
    string CurrentBalanceCurrency,
    decimal? CreditLimitAmount,
    string? CreditLimitCurrency,
    AddressDto BillingAddress,
    AddressDto ShippingAddress,
    Guid? CustomerGroupId,
    DateOnly? DateOfBirth,
    int? Gender,
    string? GenderName,
    int TotalRewardPoints,
    int TotalRewardPointsUsed,
    int TotalRewardPointsExpired,
    bool IsActive,
    DateTimeOffset CreatedAt);
