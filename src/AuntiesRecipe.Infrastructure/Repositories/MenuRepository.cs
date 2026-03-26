using AuntiesRecipe.Application.Repositories;
using AuntiesRecipe.Domain.Entities;
using AuntiesRecipe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuntiesRecipe.Infrastructure.Repositories;

public sealed class MenuRepository(IDbContextFactory<AppDbContext> dbFactory) : IMenuRepository
{
    private const string FeaturedCategoryName = "Featured Elixirs & Juices";

    public async Task<List<Category>> GetCategoriesWithItemsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Categories.AsNoTracking().Include(c => c.MenuItems)
            .Where(c => c.MenuItems.Any()).OrderBy(c => c.SortOrder).ToListAsync(ct);
    }

    public async Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Categories.AsNoTracking().OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<Category?> GetCategoryByIdAsync(int categoryId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);
    }

    public async Task<bool> CategoryExistsAsync(int categoryId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Categories.AnyAsync(c => c.Id == categoryId, ct);
    }

    public async Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var query = db.Categories.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        return await query.AnyAsync(c => c.Name == name, ct);
    }

    public async Task<int> GetMaxCategorySortOrderAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.Categories.MaxAsync(c => (int?)c.SortOrder, ct) ?? 0;
    }

    public async Task AddCategoryAsync(Category category, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
    }

    public async Task RenameCategoryAsync(int categoryId, string newName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, ct);
        if (cat is null) return;
        cat.Name = newName;
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveCategoryAsync(Category category, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tracked = await db.Categories.FirstOrDefaultAsync(c => c.Id == category.Id, ct);
        if (tracked is null) return;

        var itemIds = await db.MenuItems.Where(m => m.CategoryId == tracked.Id).Select(m => m.Id).ToListAsync(ct);
        if (itemIds.Count > 0)
        {
            var cartRows = await db.CartItems.Where(ci => itemIds.Contains(ci.MenuItemId)).ToListAsync(ct);
            if (cartRows.Count > 0) db.CartItems.RemoveRange(cartRows);
        }

        db.Categories.Remove(tracked);
        await db.SaveChangesAsync(ct);
    }

    public async Task<MenuItem?> GetMenuItemByIdAsync(int menuItemId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId, ct);
    }

    public async Task<decimal> GetMenuItemPriceAsync(int menuItemId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.MenuItems.Where(m => m.Id == menuItemId).Select(m => m.Price).FirstOrDefaultAsync(ct);
    }

    public async Task<Dictionary<int, string>> GetMenuItemNamesByIdsAsync(int[] ids, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.MenuItems.AsNoTracking().Where(m => ids.Contains(m.Id))
            .Select(m => new { m.Id, m.Name }).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
    }

    public async Task AddMenuItemAsync(MenuItem item, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        db.MenuItems.Add(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateMenuItemAsync(int menuItemId, int? categoryId, string name, string? description, decimal price, string? imageUrl, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var item = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId, ct);
        if (item is null) return;
        if (categoryId.HasValue) item.CategoryId = categoryId.Value;
        item.Name = name;
        item.Description = description;
        item.Price = price;
        item.ImageUrl = imageUrl;
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveMenuItemAsync(MenuItem item, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var tracked = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == item.Id, ct);
        if (tracked is null) return;

        var cartRows = await db.CartItems.Where(ci => ci.MenuItemId == tracked.Id).ToListAsync(ct);
        if (cartRows.Count > 0) db.CartItems.RemoveRange(cartRows);

        db.MenuItems.Remove(tracked);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> GetOrCreateFeaturedCategoryIdAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.Categories.FirstOrDefaultAsync(c => c.Name == FeaturedCategoryName, ct);
        if (existing is not null) return existing.Id;

        var category = new Category { Name = FeaturedCategoryName, SortOrder = 1 };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category.Id;
    }

    public async Task<int> GetTodaySpecialMenuItemIdAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var featuredCategoryId = await db.Categories
            .Where(c => c.Name == FeaturedCategoryName).Select(c => c.Id).SingleOrDefaultAsync(ct);

        var query = db.MenuItems.AsNoTracking().AsQueryable();
        if (featuredCategoryId != 0) query = query.Where(m => m.CategoryId == featuredCategoryId);

        var ids = await query.OrderBy(m => m.Id).Select(m => m.Id).ToListAsync(ct);
        if (ids.Count == 0) return 0;
        return ids[DateTime.UtcNow.DayOfYear % ids.Count];
    }

    public async Task<List<int>> GetMenuItemIdsByCategoryAsync(int categoryId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.MenuItems.Where(m => m.CategoryId == categoryId).Select(m => m.Id).ToListAsync(ct);
    }

    public async Task RemoveCartItemsForMenuItemsAsync(List<int> menuItemIds, CancellationToken ct = default)
    {
        if (menuItemIds.Count == 0) return;
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.CartItems.Where(ci => menuItemIds.Contains(ci.MenuItemId)).ToListAsync(ct);
        if (rows.Count > 0)
        {
            db.CartItems.RemoveRange(rows);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        await db.SaveChangesAsync(ct);
    }
}
