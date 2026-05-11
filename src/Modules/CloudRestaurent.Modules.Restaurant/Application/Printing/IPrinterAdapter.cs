namespace CloudRestaurent.Modules.Restaurant.Application.Printing;

/// <summary>
/// Abstraction over a kitchen/receipt printer. v1 has one impl: NetworkEscPosAdapter
/// that opens a TCP connection to a station's IP on port 9100 (the de-facto raw
/// ESC/POS port). Future impls: USB direct (via a print-bridge daemon), bluetooth,
/// or a noop adapter for testing.
/// </summary>
public interface IPrinterAdapter
{
    /// <summary>
    /// Send a kitchen ticket to the configured printer. <paramref name="lines"/> is
    /// already-formatted per-line content (header + items + footer); the adapter
    /// is responsible for wrapping with paper-cut commands.
    /// </summary>
    Task<PrintResult> PrintTicketAsync(
        string ipAddress,
        int port,
        KitchenPrintPayload payload,
        CancellationToken ct = default);
}

public sealed record KitchenPrintPayload(
    string OrderNumber,
    string StationName,
    string? TableCode,
    string? CustomerName,
    DateTimeOffset OpenedAt,
    IReadOnlyList<KitchenPrintLine> Lines);

public sealed record KitchenPrintLine(
    int Quantity,
    string ProductName,
    IReadOnlyList<string> Modifiers,
    string? Notes);

public sealed record PrintResult(bool Success, string? Message = null);
