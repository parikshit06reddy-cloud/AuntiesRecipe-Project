using AuntiesRecipe.Application.Orders;

namespace AuntiesRecipe.Application.Abstractions;

public interface IOrderService
{
    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersForAdminAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersByStatusForAdminAsync(string statusBucket, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummaryDto>> GetOrderHistoryForAdminAsync(AdminOrderHistoryFilterDto filter, CancellationToken cancellationToken = default);
    Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default);
    Task<OrderSummaryDto?> GetOrderByIdAsync(int orderId, CancellationToken cancellationToken = default);
}
