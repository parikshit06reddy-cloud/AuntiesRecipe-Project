using AuntiesRecipe.Application.Repositories;
using AuntiesRecipe.Domain.Entities;
using AuntiesRecipe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuntiesRecipe.Infrastructure.Repositories;

public sealed class CartRepository(IDbContextFactory<AppDbContext> dbFactory) : ICartRepository
{
    public async Task<Cart> GetOrCreateAsync(string cartId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var cart = await db.Carts.FirstOrDefaultAsync(c => c.Id == cartId, ct);
        if (cart is null)
        {
            cart = new Cart { Id = cartId, CreatedAtUtc = DateTime.UtcNow };
            db.Carts.Add(cart);
            await db.SaveChangesAsync(ct);
        }
        return cart;
    }

    public async Task<List<CartItem>> GetItemsAsync(string cartId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.CartItems.Where(i => i.CartId == cartId).ToListAsync(ct);
    }

    public async Task<CartItem?> GetItemAsync(string cartId, int menuItemId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.CartItems.FirstOrDefaultAsync(i => i.CartId == cartId && i.MenuItemId == menuItemId, ct);
    }

    public async Task AddItemAsync(CartItem item, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.CartItems.Add(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateItemQuantityAsync(string cartId, int menuItemId, int quantity, decimal unitPrice, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.CartItems.FirstOrDefaultAsync(i => i.CartId == cartId && i.MenuItemId == menuItemId, ct);
        if (existing is null) return;
        existing.Quantity = quantity;
        existing.UnitPrice = unitPrice;
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveItemAsync(string cartId, int menuItemId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.CartItems.FirstOrDefaultAsync(i => i.CartId == cartId && i.MenuItemId == menuItemId, ct);
        if (existing is not null)
        {
            db.CartItems.Remove(existing);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task RemoveAllItemsAsync(string cartId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var items = await db.CartItems.Where(i => i.CartId == cartId).ToListAsync(ct);
        if (items.Count > 0)
        {
            db.CartItems.RemoveRange(items);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.SaveChangesAsync(ct);
    }
}
