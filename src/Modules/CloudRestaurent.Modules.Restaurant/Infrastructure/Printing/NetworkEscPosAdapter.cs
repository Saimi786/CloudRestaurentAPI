using System.Net.Sockets;
using System.Text;
using CloudRestaurent.Modules.Restaurant.Application.Printing;
using Microsoft.Extensions.Logging;

namespace CloudRestaurent.Modules.Restaurant.Infrastructure.Printing;

/// <summary>
/// Sends raw ESC/POS commands over TCP to a network-attached thermal printer.
/// Most kitchen printers (Epson TM-series, Star TSP, Bixolon, no-name clones)
/// listen on port 9100 and accept Epson ESC/POS as their lowest common
/// denominator. This impl deliberately avoids any dependency — just bytes
/// over a socket — so deployments don't need a print server installed.
/// </summary>
public sealed class NetworkEscPosAdapter(ILogger<NetworkEscPosAdapter> logger) : IPrinterAdapter
{
    // ESC/POS control sequences. Values from the Epson "ESC/POS Application Programming Guide".
    private static readonly byte[] InitPrinter      = [0x1B, 0x40];                 // ESC @
    private static readonly byte[] CenterAlign      = [0x1B, 0x61, 0x01];           // ESC a 1
    private static readonly byte[] LeftAlign        = [0x1B, 0x61, 0x00];           // ESC a 0
    private static readonly byte[] BoldOn           = [0x1B, 0x45, 0x01];           // ESC E 1
    private static readonly byte[] BoldOff          = [0x1B, 0x45, 0x00];           // ESC E 0
    private static readonly byte[] DoubleSizeOn     = [0x1D, 0x21, 0x11];           // GS ! 17 (double height + width)
    private static readonly byte[] DoubleSizeOff    = [0x1D, 0x21, 0x00];           // GS ! 0
    private static readonly byte[] FullCut          = [0x1D, 0x56, 0x00];           // GS V 0 (paper cut)
    private static readonly byte[] FeedThree        = [0x1B, 0x64, 0x03];           // ESC d 3 (feed 3 lines before cut)

    public async Task<PrintResult> PrintTicketAsync(
        string ipAddress,
        int port,
        KitchenPrintPayload payload,
        CancellationToken ct = default)
    {
        try
        {
            using var client = new TcpClient();
            // Hard cap connect time — a printer that's offline shouldn't block the request.
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            connectCts.CancelAfter(TimeSpan.FromSeconds(3));
            await client.ConnectAsync(ipAddress, port, connectCts.Token);

            await using var stream = client.GetStream();
            var bytes = BuildEscPosBytes(payload);
            await stream.WriteAsync(bytes, ct);
            await stream.FlushAsync(ct);

            return new PrintResult(true);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to print kitchen ticket to {Ip}:{Port}", ipAddress, port);
            return new PrintResult(false, ex.Message);
        }
    }

    private static byte[] BuildEscPosBytes(KitchenPrintPayload p)
    {
        using var ms = new MemoryStream();

        ms.Write(InitPrinter);

        // Header — station + order — bold and centered for visibility from across the kitchen.
        ms.Write(CenterAlign);
        ms.Write(BoldOn);
        ms.Write(DoubleSizeOn);
        ms.Write(Encoding.UTF8.GetBytes($"{p.StationName}\n"));
        ms.Write(DoubleSizeOff);
        ms.Write(Encoding.UTF8.GetBytes($"Order #{p.OrderNumber}\n"));
        ms.Write(BoldOff);

        // Meta — table / customer / time. Plain weight, still centered.
        if (!string.IsNullOrEmpty(p.TableCode))
            ms.Write(Encoding.UTF8.GetBytes($"Table {p.TableCode}\n"));
        if (!string.IsNullOrEmpty(p.CustomerName))
            ms.Write(Encoding.UTF8.GetBytes($"{p.CustomerName}\n"));
        ms.Write(Encoding.UTF8.GetBytes($"{p.OpenedAt:HH:mm}\n"));

        // Divider.
        ms.Write(LeftAlign);
        ms.Write(Encoding.UTF8.GetBytes("--------------------------------\n"));

        // Lines. Big quantity, then product, modifiers indented with leading "+".
        foreach (var line in p.Lines)
        {
            ms.Write(BoldOn);
            ms.Write(Encoding.UTF8.GetBytes($"{line.Quantity} x {line.ProductName}\n"));
            ms.Write(BoldOff);
            foreach (var m in line.Modifiers)
                ms.Write(Encoding.UTF8.GetBytes($"  + {m}\n"));
            if (!string.IsNullOrWhiteSpace(line.Notes))
                ms.Write(Encoding.UTF8.GetBytes($"  ! {line.Notes}\n"));
        }

        // Cut.
        ms.Write(FeedThree);
        ms.Write(FullCut);

        return ms.ToArray();
    }
}
