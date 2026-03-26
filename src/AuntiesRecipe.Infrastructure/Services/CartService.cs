using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Cart;
using AuntiesRecipe.Application.Orders;
using AuntiesRecipe.Domain.Entities;
using AuntiesRecipe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuntiesRecipe.Infrastructure.Services;

public sealed class CartService : ICartService, IOrderService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    private const string FeaturedCategoryName = "Featured Elixirs & Juices";
    private const decimal DailySpecialPrice = 7.99m;

    public CartService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<CartDto> GetCartAsync(string cartId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            throw new ArgumentException("cartId is required", nameof(cartId));
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var cart = await db.Carts.FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);
        if (cart is null)
        {
            cart = new Cart { Id = cartId, CreatedAtUtc = DateTime.UtcNow };
            db.Carts.Add(cart);
            await db.SaveChangesAsync(cancellationToken);
        }

        var cartItemEntities = await db.CartItems
            .AsNoTracking()
            .Where(i => i.CartId == cartId)
            .Select(i => new { i.MenuItemId, i.Quantity, i.UnitPrice })
            .ToListAsync(cancellationToken);

        if (cartItemEntities.Count == 0)
        {
            return new CartDto(cartId, Array.Empty<CartItemDto>(), 0m);
        }

        var menuItemIds = cartItemEntities.Select(i => i.MenuItemId).Distinct().ToArray();

        var menuRows = await db.MenuItems
            .AsNoTracking()
            .Where(m => menuItemIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Name, m.Description })
            .ToListAsync(cancellationToken);

        var menuById = menuRows.ToDictionary(x => x.Id, x => (x.Name, x.Description));

        var items = new List<CartItemDto>(cartItemEntities.Count);
        foreach (var ci in cartItemEntities)
        {
            if (!menuById.TryGetValue(ci.MenuItemId, out var menu))
            {
                // If a menu item was removed from the seed, skip it.
                continue;
            }

            items.Add(new CartItemDto(ci.MenuItemId, menu.Name, menu.Description, ci.UnitPrice, ci.Quantity));
        }

        var subtotal = items.Sum(i => i.UnitPrice * i.Quantity);
        return new CartDto(cartId, items, subtotal);
    }

    public async Task AddToCartAsync(string cartId, int menuItemId, int quantity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            throw new ArgumentException("cartId is required", nameof(cartId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "quantity must be > 0");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var cart = await db.Carts.FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);
        if (cart is null)
        {
            cart = new Cart { Id = cartId, CreatedAtUtc = DateTime.UtcNow };
            db.Carts.Add(cart);
        }

        var todaySpecialMenuItemId = await GetTodaySpecialMenuItemIdAsync(db, cancellationToken);

        var menuItemPrice = await db.MenuItems
            .Where(m => m.Id == menuItemId)
            .Select(m => m.Price)
            .FirstOrDefaultAsync(cancellationToken);

        if (menuItemPrice <= 0m)
        {
            menuItemPrice = 15.99m;
        }

        var unitPrice = menuItemId == todaySpecialMenuItemId ? DailySpecialPrice : menuItemPrice;

        var existing = await db.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cartId && i.MenuItemId == menuItemId, cancellationToken);

        if (existing is null)
        {
            db.CartItems.Add(new CartItem
            {
                CartId = cartId,
                MenuItemId = menuItemId,
                Quantity = quantity,
                UnitPrice = unitPrice,
            });
        }
        else
        {
            existing.Quantity += quantity;
            // Update today's price snapshot.
            existing.UnitPrice = unitPrice;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetQuantityAsync(string cartId, int menuItemId, int quantity, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var cart = await db.Carts.FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);
        if (cart is null)
        {
            return;
        }

        var existing = await db.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cartId && i.MenuItemId == menuItemId, cancellationToken);

        if (existing is null)
        {
            if (quantity <= 0)
            {
                return;
            }

            var todaySpecialMenuItemId = await GetTodaySpecialMenuItemIdAsync(db, cancellationToken);

            var menuItemPrice = await db.MenuItems
                .Where(m => m.Id == menuItemId)
                .Select(m => m.Price)
                .FirstOrDefaultAsync(cancellationToken);

            if (menuItemPrice <= 0m)
            {
                menuItemPrice = 15.99m;
            }

            var unitPrice = menuItemId == todaySpecialMenuItemId ? DailySpecialPrice : menuItemPrice;

            db.CartItems.Add(new CartItem
            {
                CartId = cartId,
                MenuItemId = menuItemId,
                Quantity = quantity,
                UnitPrice = unitPrice,
            });
        }
        else
        {
            if (quantity <= 0)
            {
                db.CartItems.Remove(existing);
            }
            else
            {
                existing.Quantity = quantity;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveFromCartAsync(string cartId, int menuItemId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.CartItems
            .FirstOrDefaultAsync(i => i.CartId == cartId && i.MenuItemId == menuItemId, cancellationToken);

        if (existing is null)
        {
            return;
        }

        db.CartItems.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CheckoutAsync(string cartId, CheckoutRequestDto request, string? customerUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            throw new ArgumentException("cartId is required", nameof(cartId));
        }

        if (string.IsNullOrWhiteSpace(request.PickupName))
        {
            throw new ArgumentException("PickupName is required", nameof(request));
        }
        if (string.IsNullOrWhiteSpace(request.PickupPhone))
        {
            throw new ArgumentException("PickupPhone is required", nameof(request));
        }

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        // Token resets daily (UTC day) and starts at 1.
        // For a demo app this is sufficient; the unique index on (TokenDateUtc, DailyTokenNumber)
        // provides a safety net if two checkouts race.
        var tokenDateUtc = DateTime.UtcNow.Date;

        var cartItemEntities = await db.CartItems
            .Where(i => i.CartId == cartId)
            .ToListAsync(cancellationToken);

        if (cartItemEntities.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty");
        }

        var menuItemIds = cartItemEntities.Select(i => i.MenuItemId).Distinct().ToArray();
        var menuById = await db.MenuItems
            .AsNoTracking()
            .Where(m => menuItemIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);

        var nextToken = (await db.Orders
            .Where(o => o.TokenDateUtc == tokenDateUtc)
            .MaxAsync(o => (int?)o.DailyTokenNumber, cancellationToken)) ?? 0;

        var order = new Order
        {
            CustomerUserId = customerUserId,
            TokenDateUtc = tokenDateUtc,
            DailyTokenNumber = nextToken + 1,
            PickupName = request.PickupName,
            PickupPhone = request.PickupPhone,
            Notes = request.Notes,
            Status = OrderStatus.Placed,
            CreatedAtUtc = DateTime.UtcNow,
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var cartItem in cartItemEntities)
        {
            var name = menuById.TryGetValue(cartItem.MenuItemId, out var n) ? n : "Item";

            db.OrderItems.Add(new OrderItem
            {
                OrderId = order.Id,
                MenuItemId = cartItem.MenuItemId,
                MenuItemName = name,
                UnitPrice = cartItem.UnitPrice,
                Quantity = cartItem.Quantity,
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        db.CartItems.RemoveRange(cartItemEntities);
        await db.SaveChangesAsync(cancellationToken);

        return order.Id;
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> GetOrdersForAdminAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var rows = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
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
                o.Status.ToString()))
            .ToListAsync(cancellationToken);

        return rows;
    }

    public async Task<OrderSummaryDto?> GetOrderByIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

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

    private async Task<int> GetTodaySpecialMenuItemIdAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var featuredCategoryId = await db.Categories
            .Where(c => c.Name == FeaturedCategoryName)
            .Select(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        var menuItemsQuery = db.MenuItems.AsNoTracking().AsQueryable();

        if (featuredCategoryId != 0)
        {
            menuItemsQuery = menuItemsQuery.Where(m => m.CategoryId == featuredCategoryId);
        }

        var ids = await menuItemsQuery
            .OrderBy(m => m.Id)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            return 0;
        }

        var idx = DateTime.UtcNow.DayOfYear % ids.Count;
        return ids[idx];
    }
}
