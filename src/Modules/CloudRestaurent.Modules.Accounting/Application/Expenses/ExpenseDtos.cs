using CloudRestaurent.Modules.Sales.Domain;

namespace CloudRestaurent.Modules.Accounting.Application.Expenses;

public sealed record ExpenseDto(
    Guid Id,
    Guid BranchId,
    string BranchName,
    Guid ExpenseAccountId,
    string ExpenseAccountCode,
    string ExpenseAccountName,
    string Reference,
    string? Description,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    string MethodName,
    DateTimeOffset OccurredAt,
    Guid CreatedByUserId,
    string CreatedByUserName);
