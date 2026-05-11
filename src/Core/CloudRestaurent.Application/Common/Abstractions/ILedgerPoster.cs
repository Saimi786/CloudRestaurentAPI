namespace CloudRestaurent.Application.Common.Abstractions;

/// <summary>
/// Contract for posting balanced debit/credit entries to the ledger.
/// Implementation lives in Modules.Accounting.Infrastructure; Sales handlers call
/// it when an order closes so the chart-of-accounts stays in sync without taking
/// a hard dependency on the Accounting module.
/// </summary>
public interface ILedgerPoster
{
    /// <summary>
    /// Post a closed order to the ledger:
    /// debit Cash/Bank (per payment method), credit Sales Revenue + Tax Payable, debit Discount.
    /// Idempotent — calling twice for the same orderId is a no-op.
    /// </summary>
    Task PostOrderClosedAsync(Guid tenantId, Guid orderId, CancellationToken ct);

    /// <summary>
    /// Reverse-post a refund: credit Cash/Bank (per refund method), debit Sales Revenue + Tax Payable proportionally.
    /// Idempotent per refundId.
    /// </summary>
    Task PostRefundAsync(Guid tenantId, Guid refundId, CancellationToken ct);

    /// <summary>
    /// Post an expense: debit the chosen Expense account, credit Cash/Bank by payment method.
    /// Idempotent per expenseId.
    /// </summary>
    Task PostExpenseAsync(Guid tenantId, Guid expenseId, CancellationToken ct);

    /// <summary>
    /// Post a goods-receipt: debit Inventory account by received-line value, credit AP.
    /// Idempotent per (purchaseOrderId, batchKey) — caller passes the GRN batch id so partial receives don't double-post.
    /// </summary>
    Task PostGoodsReceiptAsync(Guid tenantId, Guid purchaseOrderId, Guid grnBatchId, decimal value, string currency, DateTimeOffset operationDate, CancellationToken ct);

    /// <summary>
    /// Post a supplier bill payment: debit AP, credit Cash/Bank by method. Idempotent per paymentId.
    /// </summary>
    Task PostSupplierBillPaymentAsync(Guid tenantId, Guid paymentId, CancellationToken ct);
}
