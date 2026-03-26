using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Orders;
using AuntiesRecipe.Application.Repositories;
using AuntiesRecipe.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AuntiesRecipe.Application.Services;

public sealed class OrderAppService(
    IOrderRepository orderRepo,
    ILogger<OrderAppService> logger) : IOrderService
{
    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersForAdminAsync(CancellationToken cancellationToken = default)
    {
        var orders = await orderRepo.GetAllWithItemsAsync(cancellationToken);
        return orders.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersByStatusForAdminAsync(string statusBucket, CancellationToken cancellationToken = default)
    {
        var statuses = statusBucket.ToLowerInvariant() switch
        {
            "todo" => new[] { OrderStatus.Placed },
            "inprogress" => new[] { OrderStatus.Preparing, OrderStatus.ReadyForPickup },
            "completed" => new[] { OrderStatus.Completed },
            _ => Array.Empty<OrderStatus>()
        };

        var orders = statuses.Length > 0
            ? await orderRepo.GetByStatusWithItemsAsync(statuses, cancellationToken)
            : await orderRepo.GetAllWithItemsAsync(cancellationToken);

        return orders.Select(MapToDto).ToList();
    }

    public async Task<PagedOrderHistoryDto> GetOrderHistoryForAdminAsync(AdminOrderHistoryFilterDto filter, CancellationToken cancellationToken = default)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize switch { <= 0 => 50, > 200 => 200, _ => filter.PageSize };

        OrderStatus? statusFilter = !string.IsNullOrWhiteSpace(filter.Status) &&
            Enum.TryParse<OrderStatus>(filter.Status, true, out var s) ? s : null;

        var (items, totalCount) = await orderRepo.GetFilteredWithItemsAsync(
            filter.PickupName, filter.PickupPhone, filter.TokenNumber,
            filter.FromDateUtc, filter.ToDateUtc, statusFilter,
            (page - 1) * pageSize, pageSize, cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);

        logger.LogInformation(
            "Admin order history query executed. Page={Page} PageSize={PageSize} Total={TotalCount}",
            page, pageSize, totalCount);

        return new PagedOrderHistoryDto(items.Select(MapToDto).ToList(), totalCount, page, pageSize, totalPages);
    }

    public async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var nextStatus))
            throw new InvalidOperationException("Invalid order status.");

        await orderRepo.UpdateStatusAsync(orderId, nextStatus, cancellationToken);
        logger.LogInformation("Order status updated. OrderId={OrderId} NewStatus={Status}", orderId, nextStatus);
    }

    public async Task<OrderSummaryDto?> GetOrderByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepo.GetByIdWithItemsAsync(orderId, cancellationToken);
        return order is null ? null : MapToDto(order);
    }

    private static OrderSummaryDto MapToDto(Order o) => new(
        o.Id, o.CreatedAtUtc, o.TokenDateUtc, o.DailyTokenNumber,
        o.PickupName, o.PickupPhone,
        o.Items.Select(i => new OrderItemDto(i.MenuItemId, i.MenuItemName, i.UnitPrice, i.Quantity)).ToList(),
        o.Items.Sum(i => i.UnitPrice * i.Quantity),
        o.Status.ToString());
}
