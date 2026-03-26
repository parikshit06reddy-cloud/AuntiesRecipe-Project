using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Orders;
using AuntiesRecipe.Domain.Entities;
using AuntiesRecipe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuntiesRecipe.Infrastructure.Services;

public sealed class OrderService(
    IDbContextFactory<AppDbContext> dbFactory,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersForAdminAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await ProjectOrderSummaries(db.Orders.AsNoTracking().Include(o => o.Items)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersByStatusForAdminAsync(string statusBucket, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();
        query = statusBucket.ToLowerInvariant() switch
        {
            "todo" => query.Where(o => o.Status == OrderStatus.Placed),
            "inprogress" => query.Where(o => o.Status == OrderStatus.Preparing || o.Status == OrderStatus.ReadyForPickup),
            "completed" => query.Where(o => o.Status == OrderStatus.Completed),
            _ => query
        };

        return await ProjectOrderSummaries(query).ToListAsync(cancellationToken);
    }

    public async Task<PagedOrderHistoryDto> GetOrderHistoryForAdminAsync(AdminOrderHistoryFilterDto filter, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Orders.AsNoTracking().Include(o => o.Items).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.PickupName))
        {
            var name = filter.PickupName.Trim();
            query = query.Where(o => o.PickupName.Contains(name));
        }
        if (!string.IsNullOrWhiteSpace(filter.PickupPhone))
        {
            var phone = filter.PickupPhone.Trim();
            query = query.Where(o => o.PickupPhone.Contains(phone));
        }
        if (filter.TokenNumber is not null)
        {
            query = query.Where(o => o.DailyTokenNumber == filter.TokenNumber.Value);
        }
        if (filter.FromDateUtc is not null)
        {
            var from = filter.FromDateUtc.Value.Date;
            query = query.Where(o => o.CreatedAtUtc >= from);
        }
        if (filter.ToDateUtc is not null)
        {
            var toExclusive = filter.ToDateUtc.Value.Date.AddDays(1);
            query = query.Where(o => o.CreatedAtUtc < toExclusive);
        }
        if (!string.IsNullOrWhiteSpace(filter.Status) &&
            Enum.TryParse<OrderStatus>(filter.Status, true, out var status))
        {
            query = query.Where(o => o.Status == status);
        }

        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize switch
        {
            <= 0 => 50,
            > 200 => 200,
            _ => filter.PageSize
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / pageSize);
        var items = await ProjectOrderSummaries(query)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        logger.LogInformation(
            "Admin order history query executed. Name={PickupName} Phone={PickupPhone} Token={TokenNumber} From={FromDate} To={ToDate} Status={Status} Page={Page} PageSize={PageSize} Total={TotalCount}",
            filter.PickupName,
            filter.PickupPhone,
            filter.TokenNumber,
            filter.FromDateUtc?.ToString("yyyy-MM-dd"),
            filter.ToDateUtc?.ToString("yyyy-MM-dd"),
            filter.Status,
            page,
            pageSize,
            totalCount);
        return new PagedOrderHistoryDto(items, totalCount, page, pageSize, totalPages);
    }

    public async Task UpdateOrderStatusAsync(int orderId, string status, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<OrderStatus>(status, true, out var nextStatus))
        {
            throw new InvalidOperationException("Invalid order status.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
        {
            throw new InvalidOperationException("Order not found.");
        }

        order.Status = nextStatus;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order status updated. OrderId={OrderId} NewStatus={Status}", orderId, nextStatus);
    }

    public async Task<OrderSummaryDto?> GetOrderByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var o = await db.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        if (o is null)
        {
            return null;
        }

        return new OrderSummaryDto(
            o.Id,
            o.CreatedAtUtc,
            o.TokenDateUtc,
            o.DailyTokenNumber,
            o.PickupName,
            o.PickupPhone,
            o.Items.Select(i => new OrderItemDto(i.MenuItemId, i.MenuItemName, i.UnitPrice, i.Quantity)).ToList(),
            o.Items.Sum(i => i.UnitPrice * i.Quantity),
            o.Status.ToString());
    }

    private static IQueryable<OrderSummaryDto> ProjectOrderSummaries(IQueryable<Order> query)
    {
        return query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.CreatedAtUtc,
                o.TokenDateUtc,
                o.DailyTokenNumber,
                o.PickupName,
                o.PickupPhone,
                o.Items.Select(i => new OrderItemDto(i.MenuItemId, i.MenuItemName, i.UnitPrice, i.Quantity)).ToList(),
                o.Items.Sum(i => i.UnitPrice * i.Quantity),
                o.Status.ToString()));
    }
}
