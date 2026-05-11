using CloudRestaurent.Application.Common.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CloudRestaurent.Api.Hubs;

[Authorize]
public sealed class KitchenHub : Hub
{
    /// <summary>Clients call this to subscribe to kitchen updates for a specific branch.</summary>
    public Task SubscribeToBranch(Guid branchId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(branchId));

    public Task UnsubscribeFromBranch(Guid branchId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(branchId));

    /// <summary>Group naming includes tenantId to avoid cross-tenant leakage.</summary>
    public static string GroupName(Guid branchId) => $"kitchen:{branchId:N}";
}

public sealed class SignalRKitchenNotifier(IHubContext<KitchenHub> hub) : IKitchenNotifier
{
    public Task TicketChangedAsync(Guid tenantId, Guid branchId, Guid ticketId, CancellationToken ct) =>
        hub.Clients.Group(KitchenHub.GroupName(branchId))
            .SendAsync("TicketChanged", new { ticketId }, ct);
}
