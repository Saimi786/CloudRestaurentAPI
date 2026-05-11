using CloudRestaurent.Modules.Restaurant.Domain;
using CloudRestaurent.Modules.Sales.Domain;

namespace CloudRestaurent.Modules.Sales.Application.Kitchen.Dtos;

public sealed record KitchenTicketLineDto(
    string ProductName,
    decimal Quantity,
    string? Notes,
    Guid? KitchenStationId,
    string? KitchenStationName,
    IReadOnlyList<string> Modifiers);

public sealed record KitchenTicketDto(
    Guid Id,
    Guid OrderId,
    string? OrderNumber,
    Guid BranchId,
    string BranchName,
    string? TableCode,
    OrderType OrderType,
    string OrderTypeName,
    KitchenTicketStatus Status,
    string StatusName,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ReadyAt,
    DateTimeOffset? ServedAt,
    int MinutesOpen,
    IReadOnlyList<Guid> InvolvedStationIds,
    IReadOnlyList<Guid> BumpedStationIds,
    IReadOnlyList<KitchenTicketLineDto> Lines);
