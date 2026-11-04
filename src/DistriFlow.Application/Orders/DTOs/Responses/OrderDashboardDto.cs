namespace DistriFlow.Application.Orders.DTOs.Responses;

public sealed record OrderDashboardDto(
    DateOnly Today,
    string TimeZone,
    DateTime NextDayStartsAtUtc,
    int CreatedToday,
    int DraftCount,
    int ConfirmedToday,
    IReadOnlyList<OrderListItemDto> RecentOrders);
