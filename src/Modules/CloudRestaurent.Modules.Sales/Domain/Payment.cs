using CloudRestaurent.Domain.Common;

namespace CloudRestaurent.Modules.Sales.Domain;

public class Payment : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public Money Amount { get; private set; }
    public string? Reference { get; private set; }
    public DateTimeOffset PaidAt { get; private set; }

    private Payment() { }

    public Payment(Guid id, Guid orderId, PaymentMethod method, Money amount, string? reference, DateTimeOffset paidAt)
    {
        if (amount.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be > 0.");

        Id = id;
        OrderId = orderId;
        Method = method;
        Amount = amount;
        Reference = reference;
        PaidAt = paidAt;
    }
}
