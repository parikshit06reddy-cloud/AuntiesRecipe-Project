using AuntiesRecipe.Application.Repositories;
using AuntiesRecipe.Domain.Entities;
using AuntiesRecipe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuntiesRecipe.Infrastructure.Repositories;

public sealed class OrderRepository(IDbContextFactory<AppDbContext> dbFactory) : IOrderRepository
{
    public async Task<List<Order>> GetAllWithItemsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Orders.AsNoTracking().Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAtUtc).ToListAsync(ct);
    }

    public async Task<List<Order>> GetByStatusWithItemsAsync(OrderStatus[] statuses, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => statuses.Contains(o.Status))
            .OrderByDescending(o => o.CreatedAtUtc).ToListAsync(ct);
    }

    public async Task<(List<Order> Items, int TotalCount)> GetFilteredWithItemsAsync(
        string? pickupName, string? pickupPhone, int? tokenNumber,
        DateTime? fromDate, DateTime? toDate, OrderStatus? status,
        int skip, int take, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var query = db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(pickupName))
            query = query.Where(o => o.PickupName.Contains(pickupName.Trim()));
        if (!string.IsNullOrWhiteSpace(pickupPhone))
            query = query.Where(o => o.PickupPhone.Contains(pickupPhone.Trim()));
        if (tokenNumber is not null)
            query = query.Where(o => o.DailyTokenNumber == tokenNumber.Value);
        if (fromDate is not null)
            query = query.Where(o => o.CreatedAtUtc >= fromDate.Value.Date);
        if (toDate is not null)
            query = query.Where(o => o.CreatedAtUtc < toDate.Value.Date.AddDays(1));
        if (status is not null)
            query = query.Where(o => o.Status == status.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query.OrderByDescending(o => o.CreatedAtUtc)
            .Skip(skip).Take(take).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<Order?> GetByIdWithItemsAsync(int orderId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Orders.AsNoTracking().Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
    }

    public async Task<int> GetNextDailyTokenAsync(DateTime tokenDateUtc, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var maxToken = await db.Orders.Where(o => o.TokenDateUtc == tokenDateUtc)
            .MaxAsync(o => (int?)o.DailyTokenNumber, ct) ?? 0;
        return maxToken + 1;
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(int orderId, OrderStatus newStatus, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order is null) return;
        order.Status = newStatus;
        await db.SaveChangesAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.SaveChangesAsync(ct);
    }
}
