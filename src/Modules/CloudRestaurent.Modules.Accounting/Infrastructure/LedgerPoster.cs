using CloudRestaurent.Application.Common.Abstractions;
using CloudRestaurent.Modules.Accounting.Domain;
using CloudRestaurent.Modules.Inventory.Domain;
using CloudRestaurent.Modules.Sales.Domain;
using Microsoft.EntityFrameworkCore;

namespace CloudRestaurent.Modules.Accounting.Infrastructure;

public sealed class LedgerPoster(IAppDbContext db) : ILedgerPoster
{
    public async Task PostOrderClosedAsync(Guid tenantId, Guid orderId, CancellationToken ct)
    {
        var batchId = $"SALE-{orderId:N}";

        // Idempotent: skip if already posted.
        if (await db.Set<AccountTransaction>().AnyAsync(t => t.BatchId == batchId, ct))
            return;

        var order = await db.Set<Order>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null || order.Status != OrderStatus.Closed) return;

        var payments = await db.Set<Payment>().AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .ToListAsync(ct);

        // Resolve the canonical accounts. If anything's missing the tenant skipped
        // chart-of-accounts seeding — log silently for now and bail.
        var accounts = await db.Set<Account>().AsNoTracking()
            .Where(a => a.IsActive).ToListAsync(ct);
        var byCode = accounts.ToDictionary(a => a.Code);
        if (!byCode.ContainsKey("4000") || !byCode.ContainsKey("2200")) return;

        var revenueAcct = byCode["4000"];                      // Sales Revenue
        var taxPayableAcct = byCode["2200"];                   // Tax Payable
        var discountAcct = byCode.GetValueOrDefault("4500");   // Discounts Given (optional)
        var cashAcct = byCode.GetValueOrDefault("1000");
        var bankAcct = byCode.GetValueOrDefault("1010");
        var walletAcct = byCode.GetValueOrDefault("1020") ?? bankAcct ?? cashAcct;

        var lines = new List<AccountTransaction>();
        var operationDate = order.ClosedAt ?? DateTimeOffset.UtcNow;
        var ccy = order.Currency;

        // Debits: cash/bank/wallet for each payment received
        foreach (var p in payments)
        {
            var paymentAcct = p.Method switch
            {
                PaymentMethod.Cash => cashAcct,
                PaymentMethod.Card => bankAcct,
                PaymentMethod.BankTransfer => bankAcct,
                PaymentMethod.Wallet => walletAcct,
                _ => cashAcct
            };
            if (paymentAcct is null) continue;
            lines.Add(new AccountTransaction(
                Guid.NewGuid(), tenantId, paymentAcct.Id, LedgerSide.Debit,
                p.Amount.Amount, ccy, "Sale", orderId,
                $"Payment via {p.Method} for {order.OrderNumber}", batchId, operationDate));
        }

        // Debit: Discount (contra-revenue) if any
        if (order.DiscountAmount > 0 && discountAcct is not null)
        {
            lines.Add(new AccountTransaction(
                Guid.NewGuid(), tenantId, discountAcct.Id, LedgerSide.Debit,
                order.DiscountAmount, ccy, "Sale", orderId,
                $"Discount on {order.OrderNumber}", batchId, operationDate));
        }

        // Credits: Sales Revenue (subtotal), Tax Payable (tax)
        if (order.SubtotalAmount > 0)
        {
            lines.Add(new AccountTransaction(
                Guid.NewGuid(), tenantId, revenueAcct.Id, LedgerSide.Credit,
                order.SubtotalAmount, ccy, "Sale", orderId,
                $"Sales for {order.OrderNumber}", batchId, operationDate));
        }
        if (order.TaxAmount > 0)
        {
            lines.Add(new AccountTransaction(
                Guid.NewGuid(), tenantId, taxPayableAcct.Id, LedgerSide.Credit,
                order.TaxAmount, ccy, "Sale", orderId,
                $"Tax collected on {order.OrderNumber}", batchId, operationDate));
        }

        if (lines.Count == 0) return;

        // Sanity: debits should equal credits (within rounding).
        var debits = lines.Where(l => l.Side == LedgerSide.Debit).Sum(l => l.Amount);
        var credits = lines.Where(l => l.Side == LedgerSide.Credit).Sum(l => l.Amount);
        if (Math.Abs(debits - credits) > 0.01m)
        {
            // Out of balance — would corrupt the ledger. Bail rather than commit half-baked entries.
            // (In v2 this becomes an explicit "post failed" event the operator can replay.)
            return;
        }

        foreach (var l in lines) db.Set<AccountTransaction>().Add(l);
        await db.SaveChangesAsync(ct);
    }

    public async Task PostRefundAsync(Guid tenantId, Guid refundId, CancellationToken ct)
    {
        var batchId = $"REFUND-{refundId:N}";
        if (await db.Set<AccountTransaction>().AnyAsync(t => t.BatchId == batchId, ct))
            return;

        var refund = await db.Set<Refund>().AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == refundId, ct);
        if (refund is null) return;

        var order = await db.Set<Order>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == refund.OrderId, ct);
        if (order is null) return;

        var accounts = await db.Set<Account>().AsNoTracking()
            .Where(a => a.IsActive).ToListAsync(ct);
        var byCode = accounts.ToDictionary(a => a.Code);
        if (!byCode.ContainsKey("4000") || !byCode.ContainsKey("2200")) return;

        var revenueAcct = byCode["4000"];
        var taxPayableAcct = byCode["2200"];
        var cashAcct = byCode.GetValueOrDefault("1000");
        var bankAcct = byCode.GetValueOrDefault("1010");
        var walletAcct = byCode.GetValueOrDefault("1020") ?? bankAcct ?? cashAcct;

        var refundAcct = refund.Method switch
        {
            PaymentMethod.Cash => cashAcct,
            PaymentMethod.Card => bankAcct,
            PaymentMethod.BankTransfer => bankAcct,
            PaymentMethod.Wallet => walletAcct,
            _ => cashAcct
        };
        if (refundAcct is null) return;

        // Apportion refund amount between subtotal and tax in same ratio as the order.
        var orderTotal = order.SubtotalAmount + order.TaxAmount;
        decimal refundSubtotal = refund.Amount, refundTax = 0m;
        if (orderTotal > 0)
        {
            refundSubtotal = Math.Round(refund.Amount * (order.SubtotalAmount / orderTotal), 4);
            refundTax = refund.Amount - refundSubtotal;
        }

        var lines = new List<AccountTransaction>();
        var op = refund.RefundedAt;
        var ccy = refund.Currency;

        // Credit Cash/Bank — money out
        lines.Add(new AccountTransaction(
            Guid.NewGuid(), tenantId, refundAcct.Id, LedgerSide.Credit,
            refund.Amount, ccy, "Refund", refundId,
            $"Refund on {order.OrderNumber}", batchId, op));

        // Debit Sales Revenue + Tax Payable — reverse what was booked
        if (refundSubtotal > 0)
        {
            lines.Add(new AccountTransaction(
                Guid.NewGuid(), tenantId, revenueAcct.Id, LedgerSide.Debit,
                refundSubtotal, ccy, "Refund", refundId,
                $"Sales reversal on {order.OrderNumber}", batchId, op));
        }
        if (refundTax > 0)
        {
            lines.Add(new AccountTransaction(
                Guid.NewGuid(), tenantId, taxPayableAcct.Id, LedgerSide.Debit,
                refundTax, ccy, "Refund", refundId,
                $"Tax reversal on {order.OrderNumber}", batchId, op));
        }

        var debits = lines.Where(l => l.Side == LedgerSide.Debit).Sum(l => l.Amount);
        var credits = lines.Where(l => l.Side == LedgerSide.Credit).Sum(l => l.Amount);
        if (Math.Abs(debits - credits) > 0.01m) return;

        foreach (var l in lines) db.Set<AccountTransaction>().Add(l);
        await db.SaveChangesAsync(ct);
    }

    public async Task PostGoodsReceiptAsync(
        Guid tenantId, Guid purchaseOrderId, Guid grnBatchId, decimal value, string currency,
        DateTimeOffset operationDate, CancellationToken ct)
    {
        var batchId = $"GRN-{grnBatchId:N}";
        if (await db.Set<AccountTransaction>().AnyAsync(t => t.BatchId == batchId, ct)) return;
        if (value <= 0) return;

        var accounts = await db.Set<Account>().AsNoTracking().Where(a => a.IsActive).ToListAsync(ct);
        var byCode = accounts.ToDictionary(a => a.Code);
        var inventory = byCode.GetValueOrDefault("1200");
        var ap = byCode.GetValueOrDefault("2100");
        if (inventory is null || ap is null) return;

        db.Set<AccountTransaction>().Add(new AccountTransaction(
            Guid.NewGuid(), tenantId, inventory.Id, LedgerSide.Debit,
            value, currency, "GoodsReceipt", purchaseOrderId,
            $"GRN value for PO {purchaseOrderId:N}"[..30], batchId, operationDate));
        db.Set<AccountTransaction>().Add(new AccountTransaction(
            Guid.NewGuid(), tenantId, ap.Id, LedgerSide.Credit,
            value, currency, "GoodsReceipt", purchaseOrderId,
            "AP for goods received", batchId, operationDate));

        await db.SaveChangesAsync(ct);
    }

    public async Task PostSupplierBillPaymentAsync(Guid tenantId, Guid paymentId, CancellationToken ct)
    {
        var batchId = $"APPAY-{paymentId:N}";
        if (await db.Set<AccountTransaction>().AnyAsync(t => t.BatchId == batchId, ct)) return;

        var payment = await db.Set<SupplierBillPayment>().AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment is null) return;

        var bill = await db.Set<SupplierBill>().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == payment.BillId, ct);
        if (bill is null) return;

        var accounts = await db.Set<Account>().AsNoTracking().Where(a => a.IsActive).ToListAsync(ct);
        var byCode = accounts.ToDictionary(a => a.Code);
        var ap = byCode.GetValueOrDefault("2100");
        if (ap is null) return;

        var cashAcct = byCode.GetValueOrDefault("1000");
        var bankAcct = byCode.GetValueOrDefault("1010");
        var walletAcct = byCode.GetValueOrDefault("1020") ?? bankAcct ?? cashAcct;

        var paymentAcct = payment.Method switch
        {
            SupplierBillPaymentMethod.Cash => cashAcct,
            SupplierBillPaymentMethod.Card => bankAcct,
            SupplierBillPaymentMethod.BankTransfer => bankAcct,
            SupplierBillPaymentMethod.Wallet => walletAcct,
            _ => cashAcct
        };
        if (paymentAcct is null) return;

        db.Set<AccountTransaction>().Add(new AccountTransaction(
            Guid.NewGuid(), tenantId, ap.Id, LedgerSide.Debit,
            payment.Amount, bill.Currency, "BillPayment", payment.Id,
            $"Pay bill {bill.Number}", batchId, payment.PaidAt));
        db.Set<AccountTransaction>().Add(new AccountTransaction(
            Guid.NewGuid(), tenantId, paymentAcct.Id, LedgerSide.Credit,
            payment.Amount, bill.Currency, "BillPayment", payment.Id,
            $"Paid {payment.Method}", batchId, payment.PaidAt));

        await db.SaveChangesAsync(ct);
    }

    public async Task PostExpenseAsync(Guid tenantId, Guid expenseId, CancellationToken ct)
    {
        var batchId = $"EXPENSE-{expenseId:N}";
        if (await db.Set<AccountTransaction>().AnyAsync(t => t.BatchId == batchId, ct))
            return;

        var expense = await db.Set<Expense>().AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == expenseId, ct);
        if (expense is null) return;

        var accounts = await db.Set<Account>().AsNoTracking()
            .Where(a => a.IsActive).ToListAsync(ct);
        var byCode = accounts.ToDictionary(a => a.Code);
        var cashAcct = byCode.GetValueOrDefault("1000");
        var bankAcct = byCode.GetValueOrDefault("1010");
        var walletAcct = byCode.GetValueOrDefault("1020") ?? bankAcct ?? cashAcct;

        var paymentAcct = expense.Method switch
        {
            PaymentMethod.Cash => cashAcct,
            PaymentMethod.Card => bankAcct,
            PaymentMethod.BankTransfer => bankAcct,
            PaymentMethod.Wallet => walletAcct,
            _ => cashAcct
        };
        if (paymentAcct is null) return;

        var expenseAcct = await db.Set<Account>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == expense.ExpenseAccountId, ct);
        if (expenseAcct is null) return;

        var lines = new List<AccountTransaction>
        {
            new(Guid.NewGuid(), tenantId, expenseAcct.Id, LedgerSide.Debit,
                expense.Amount, expense.Currency, "Expense", expenseId,
                expense.Description ?? "Expense", batchId, expense.OccurredAt),
            new(Guid.NewGuid(), tenantId, paymentAcct.Id, LedgerSide.Credit,
                expense.Amount, expense.Currency, "Expense", expenseId,
                $"Paid {expense.Method}", batchId, expense.OccurredAt)
        };

        foreach (var l in lines) db.Set<AccountTransaction>().Add(l);
        await db.SaveChangesAsync(ct);
    }
}
