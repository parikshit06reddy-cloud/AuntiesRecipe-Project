namespace AuntiesRecipe.Application.Orders;

public sealed record OrderItemDto(int MenuItemId, string Name, decimal UnitPrice, int Quantity);

public sealed record OrderSummaryDto(
    int OrderId,
    DateTime CreatedAtUtc,
    DateTime TokenDateUtc,
    int DailyTokenNumber,
    string PickupName,
    string PickupPhone,
    IReadOnlyList<OrderItemDto> Items,
    decimal Total,
    string Status);

public sealed record AdminOrderHistoryFilterDto(
    string? PickupName,
    string? PickupPhone,
    int? TokenNumber,
    DateTime? FromDateUtc,
    DateTime? ToDateUtc,
    string? Status,
    int Page = 1,
    int PageSize = 50);
