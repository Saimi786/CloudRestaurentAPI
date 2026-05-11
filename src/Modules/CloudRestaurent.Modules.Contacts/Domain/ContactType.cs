namespace CloudRestaurent.Modules.Contacts.Domain;

/// <summary>
/// Mirrors UltimatePOS's <c>contacts.type</c>. Single Contact entity serves
/// customers, suppliers, and the default walk-in contact.
/// </summary>
public enum ContactType
{
    Customer = 0,
    Supplier = 1,
    Both = 2,
    WalkIn = 3
}
