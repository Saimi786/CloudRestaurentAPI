namespace CloudRestaurent.Application.Common.Abstractions;

/// <summary>
/// Pushes kitchen-ticket events to subscribed clients. Implemented by SignalR in Infrastructure;
/// Application code stays oblivious to the transport.
/// </summary>
public interface IKitchenNotifier
{
    Task TicketChangedAsync(Guid tenantId, Guid branchId, Guid ticketId, CancellationToken ct);
}
