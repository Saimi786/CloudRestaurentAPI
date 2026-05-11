namespace CloudRestaurent.Modules.Accounting.Domain;

/// <summary>
/// Top-level chart-of-accounts classification — drives sign on the P&L / Balance Sheet.
/// </summary>
public enum AccountClass
{
    Asset = 0,
    Liability = 1,
    Equity = 2,
    Revenue = 3,
    Expense = 4
}

public enum LedgerSide
{
    Debit = 0,
    Credit = 1
}
