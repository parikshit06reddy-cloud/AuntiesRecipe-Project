using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Menu;
using AuntiesRecipe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuntiesRecipe.Infrastructure.Services;

public sealed class MenuService(IDbContextFactory<AppDbContext> dbFactory) : IMenuService
{
    private const string FeaturedCategoryName = "Featured Elixirs & Juices";

    public async Task<IReadOnlyList<CategoryMenuDto>> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

        var rows = await db.Categories
            .AsNoTracking()
            .Include(c => c.MenuItems)
            .Where(c => c.Name == FeaturedCategoryName)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryMenuDto(
                c.Id,
                c.Name,
                c.SortOrder,
                c.MenuItems
                    .OrderBy(m => m.Id)
                    .Select(m => new MenuItemDto(m.Id, m.Name, m.Description, m.Price))
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
            .Select(m => new MenuItemDto(m.Id, m.Name, m.Description, m.Price))
            .ToListAsync(cancellationToken);
    }

    public async Task AddFeaturedItemAsync(string name, string? description, decimal price, CancellationToken cancellationToken = default)
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
            Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero)
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateFeaturedItemAsync(int menuItemId, string name, string? description, decimal price, CancellationToken cancellationToken = default)
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

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<int> GetOrCreateFeaturedCategoryIdAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var existing = await db.Categories.FirstOrDefaultAsync(c => c.Name == FeaturedCategoryName, cancellationToken);
        if (existing is not null)
        {
            return existing.Id;
        }

        var category = new Domain.Entities.Category
        {
            Name = FeaturedCategoryName,
            SortOrder = 1
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
