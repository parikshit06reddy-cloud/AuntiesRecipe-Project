using AuntiesRecipe.Domain.Entities;

namespace AuntiesRecipe.Application.Repositories;

public interface IOrderRepository
{
    Task<List<Order>> GetAllWithItemsAsync(CancellationToken ct = default);
    Task<List<Order>> GetByStatusWithItemsAsync(OrderStatus[] statuses, CancellationToken ct = default);
    Task<(List<Order> Items, int TotalCount)> GetFilteredWithItemsAsync(
        string? pickupName, string? pickupPhone, int? tokenNumber,
        DateTime? fromDate, DateTime? toDate, OrderStatus? status,
        int skip, int take, CancellationToken ct = default);
    Task<Order?> GetByIdWithItemsAsync(int orderId, CancellationToken ct = default);
    Task<int> GetNextDailyTokenAsync(DateTime tokenDateUtc, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);
    Task UpdateStatusAsync(int orderId, OrderStatus newStatus, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
