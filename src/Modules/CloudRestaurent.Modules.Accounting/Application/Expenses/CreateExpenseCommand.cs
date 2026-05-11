using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Application.Common.Exceptions;
using CloudRestaurent.Domain.Companies;
using CloudRestaurent.Modules.Accounting.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Accounting.Application.Expenses;

public sealed record CreateExpenseCommand(
    Guid BranchId,
    Guid ExpenseAccountId,
    string Reference,
    string? Description,
    decimal Amount,
    string Currency,
    PaymentMethod Method,
    DateTimeOffset? OccurredAt) : IRequest<ExpenseDto>;

public sealed class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.ExpenseAccountId).NotEmpty();
        RuleFor(x => x.Reference).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3).Matches(@"^[A-Z]{3}$");
        RuleFor(x => x.Method).IsInEnum();
    }
}

public sealed class CreateExpenseHandler(
    IAppDbContext db,
    ITenantContext tenant,
    ICurrentUser user,
    ILedgerPoster ledger,
    IIdentityService identity)
    : IRequestHandler<CreateExpenseCommand, ExpenseDto>
{
    public async Task<ExpenseDto> Handle(CreateExpenseCommand request, CancellationToken ct)
    {
        var tenantId = tenant.TenantId
            ?? throw new UnauthorizedException("No tenant in current context.");
        var userId = user.UserId
            ?? throw new UnauthorizedException("No authenticated user.");

        var branch = await db.Set<Branch>().FirstOrDefaultAsync(b => b.Id == request.BranchId, ct)
            ?? throw new NotFoundException("Branch", request.BranchId);

        var account = await db.Set<Account>().FirstOrDefaultAsync(a => a.Id == request.ExpenseAccountId, ct)
            ?? throw new NotFoundException("Account", request.ExpenseAccountId);
        if (account.Class != AccountClass.Expense)
            throw new BusinessRuleException(
                $"Account '{account.Name}' is not an Expense account; it is {account.Class}.");

        var expense = new Expense(
            Guid.NewGuid(), tenantId, request.BranchId, request.ExpenseAccountId,
            request.Reference, request.Description, request.Amount,
            request.Currency.ToUpperInvariant(),
            request.Method, userId, request.OccurredAt);

        db.Set<Expense>().Add(expense);

        // Cash expense from an open till → register-side PaidOut movement
        if (request.Method == PaymentMethod.Cash)
        {
            var shift = await db.Set<CashRegisterShift>()
                .FirstOrDefaultAsync(s =>
                    s.Status == ShiftStatus.Open &&
                    s.OpenedByUserId == userId &&
                    s.BranchId == request.BranchId, ct);
            shift?.RecordMovement(ShiftMovementType.PaidOut, request.Amount, expense.Id,
                request.Reference, request.Description);
        }

        await db.SaveChangesAsync(ct);
        await ledger.PostExpenseAsync(tenantId, expense.Id, ct);

        var users = await identity.ListUsersAsync(tenantId, true, ct);
        var userName = users.FirstOrDefault(u => u.Id == userId)?.FullName ?? "—";

        return new ExpenseDto(
            expense.Id, expense.BranchId, branch.Name,
            expense.ExpenseAccountId, account.Code, account.Name,
            expense.Reference, expense.Description,
            expense.Amount, expense.Currency,
            expense.Method, expense.Method.ToString(),
            expense.OccurredAt, expense.CreatedByUserId, userName);
    }
}
