using CloudRestaurent.Modules.Accounting.Domain;

namespace CloudRestaurent.Modules.Accounting.Application.Accounts.Dtos;

public sealed record AccountDto(
    Guid Id,
    string Code,
    string Name,
    AccountClass Class,
    string ClassName,
    string? Description,
    bool IsSystem,
    bool IsCashOrBank,
    bool IsActive,
    decimal Balance);

public sealed record AccountTransactionDto(
    Guid Id,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    LedgerSide Side,
    string SideName,
    decimal Amount,
    string Currency,
    DateTimeOffset OperationDate,
    string SourceType,
    Guid? SourceId,
    string? Description,
    string? BatchId);
