using AuntiesRecipe.Application.Orders;

namespace AuntiesRecipe.Application.Abstractions;

public interface IOrderService
{
    Task<IReadOnlyList<OrderSummaryDto>> GetOrdersForAdminAsync(CancellationToken cancellationToken = default);
    Task<OrderSummaryDto?> GetOrderByIdAsync(int orderId, CancellationToken cancellationToken = default);
}
