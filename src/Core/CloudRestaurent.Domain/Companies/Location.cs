namespace CloudRestaurent.Domain.Companies;

public sealed record Location(
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    double? Latitude,
    double? Longitude,
    string TimeZone)
{
    public static Location Empty(string timeZone = "Asia/Karachi") =>
        new(null, null, null, null, null, null, null, null, timeZone);
}
