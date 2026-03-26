using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Menu;
using AuntiesRecipe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuntiesRecipe.Infrastructure.Services;

public sealed class MenuService(IDbContextFactory<AppDbContext> dbFactory) : IMenuService
{
    private const string JuicesCategoryName = "Featured Elixirs & Juices";

    public async Task<IReadOnlyList<CategoryMenuDto>> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var rows = await db.Categories
            .AsNoTracking()
            .Include(c => c.MenuItems)
            .Where(c => c.MenuItems.Any())
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryMenuDto(
                c.Id,
                c.Name,
                c.SortOrder,
                c.MenuItems
                    .OrderBy(m => m.Id)
                    .Select(m => new MenuItemDto(m.Id, m.Name, m.Description, m.Price, m.ImageUrl))
                    .ToList()))
            .ToListAsync(cancellationToken);

        // If not seeded yet, return empty. (Startup seeding should handle this.)
        return rows;
    }

    public async Task<IReadOnlyList<MenuItemDto>> GetFeaturedItemsForAdminAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var categoryId = await GetOrCreateFeaturedCategoryIdAsync(db, cancellationToken);

        return await db.MenuItems
            .AsNoTracking()
            .Where(m => m.CategoryId == categoryId)
            .OrderBy(m => m.Id)
            .Select(m => new MenuItemDto(m.Id, m.Name, m.Description, m.Price, m.ImageUrl))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuCategoryAdminDto>> GetCategoriesForAdminAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new MenuCategoryAdminDto(c.Id, c.Name, c.SortOrder, c.MenuItems.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task AddCategoryAsync(string categoryName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Category name is required.", nameof(categoryName));
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var trimmed = categoryName.Trim();
        var exists = await db.Categories.AnyAsync(c => c.Name == trimmed, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Category already exists.");
        }

        var maxSort = await db.Categories.MaxAsync(c => (int?)c.SortOrder, cancellationToken) ?? 0;
        db.Categories.Add(new Domain.Entities.Category
        {
            Name = trimmed,
            SortOrder = maxSort + 1
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RenameCategoryAsync(int categoryId, string categoryName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException("Category name is required.", nameof(categoryName));
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        if (cat is null)
        {
            throw new InvalidOperationException("Category not found.");
        }

        var trimmed = categoryName.Trim();
        var duplicate = await db.Categories.AnyAsync(c => c.Id != categoryId && c.Name == trimmed, cancellationToken);
        if (duplicate)
        {
            throw new InvalidOperationException("Another category with this name already exists.");
        }

        cat.Name = trimmed;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == categoryId, cancellationToken);
        if (cat is null)
        {
            return;
        }

        // If items from this category are in carts, remove those cart rows too.
        var itemIds = await db.MenuItems.Where(m => m.CategoryId == categoryId).Select(m => m.Id).ToListAsync(cancellationToken);
        if (itemIds.Count > 0)
        {
            var cartRows = await db.CartItems.Where(ci => itemIds.Contains(ci.MenuItemId)).ToListAsync(cancellationToken);
            if (cartRows.Count > 0)
            {
                db.CartItems.RemoveRange(cartRows);
            }
        }

        db.Categories.Remove(cat);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddFeaturedItemAsync(string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (price <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var categoryId = await GetOrCreateFeaturedCategoryIdAsync(db, cancellationToken);

        db.MenuItems.Add(new Domain.Entities.MenuItem
        {
            CategoryId = categoryId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero),
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim()
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMenuItemAsync(int categoryId, string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }
        if (price <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var categoryExists = await db.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new InvalidOperationException("Category not found.");
        }

        db.MenuItems.Add(new Domain.Entities.MenuItem
        {
            CategoryId = categoryId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero),
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim()
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateFeaturedItemAsync(int menuItemId, string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (price <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var categoryId = await GetOrCreateFeaturedCategoryIdAsync(db, cancellationToken);

        var item = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId && m.CategoryId == categoryId, cancellationToken);
        if (item is null)
        {
            throw new InvalidOperationException("Menu item not found.");
        }

        item.Name = name.Trim();
        item.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        item.Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        item.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMenuItemAsync(int menuItemId, int categoryId, string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }
        if (price <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
        }

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId, cancellationToken);
        if (item is null)
        {
            throw new InvalidOperationException("Menu item not found.");
        }

        var categoryExists = await db.Categories.AnyAsync(c => c.Id == categoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new InvalidOperationException("Category not found.");
        }

        item.CategoryId = categoryId;
        item.Name = name.Trim();
        item.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        item.Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        item.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMenuItemAsync(int menuItemId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var item = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == menuItemId, cancellationToken);
        if (item is null)
        {
            return;
        }

        var cartRows = await db.CartItems.Where(ci => ci.MenuItemId == menuItemId).ToListAsync(cancellationToken);
        if (cartRows.Count > 0)
        {
            db.CartItems.RemoveRange(cartRows);
        }

        db.MenuItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<int> GetOrCreateFeaturedCategoryIdAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var existing = await db.Categories.FirstOrDefaultAsync(c => c.Name == JuicesCategoryName, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var category = new Domain.Entities.Category
        {
            Name = JuicesCategoryName,
            SortOrder = 1
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
