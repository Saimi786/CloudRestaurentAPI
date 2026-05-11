namespace CloudRestaurent.Modules.Sales.Domain;

public enum OrderType
{
    DineIn   = 0,
    Takeaway = 1,
    Delivery = 2
}

public enum OrderStatus
{
    Open   = 0,
    Closed = 1,
    Voided = 2
}

public enum PaymentMethod
{
    Cash         = 0,
    Card         = 1,
    BankTransfer = 2,
    Wallet       = 3
}
