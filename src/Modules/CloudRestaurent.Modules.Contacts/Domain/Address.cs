namespace CloudRestaurent.Modules.Contacts.Domain;

/// <summary>
/// Owned value object for billing/shipping addresses on Contact.
/// All fields nullable so a partial address still maps cleanly.
/// </summary>
public record struct Address(
    string? Line1,
    string? Line2,
    string? City,
    string? State,
    string? Country,
    string? PostalCode);
