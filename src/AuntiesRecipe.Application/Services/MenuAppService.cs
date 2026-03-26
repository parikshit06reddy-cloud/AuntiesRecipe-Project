using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Menu;
using AuntiesRecipe.Application.Repositories;
using AuntiesRecipe.Domain.Entities;

namespace AuntiesRecipe.Application.Services;

public sealed class MenuAppService(IMenuRepository menuRepo) : IMenuService
{
    public async Task<IReadOnlyList<CategoryMenuDto>> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        var categories = await menuRepo.GetCategoriesWithItemsAsync(cancellationToken);
        return categories.Select(c => new CategoryMenuDto(
            c.Id, c.Name, c.SortOrder,
            c.MenuItems.OrderBy(m => m.Id)
                .Select(m => new MenuItemDto(m.Id, m.Name, m.Description, m.Price, m.ImageUrl)).ToList()
        )).ToList();
    }

    public async Task<IReadOnlyList<MenuItemDto>> GetFeaturedItemsForAdminAsync(CancellationToken cancellationToken = default)
    {
        var categoryId = await menuRepo.GetOrCreateFeaturedCategoryIdAsync(cancellationToken);
        var categories = await menuRepo.GetCategoriesWithItemsAsync(cancellationToken);
        var featured = categories.FirstOrDefault(c => c.Id == categoryId);
        if (featured is null) return Array.Empty<MenuItemDto>();
        return featured.MenuItems.OrderBy(m => m.Id)
            .Select(m => new MenuItemDto(m.Id, m.Name, m.Description, m.Price, m.ImageUrl)).ToList();
    }

    public async Task<IReadOnlyList<MenuCategoryAdminDto>> GetCategoriesForAdminAsync(CancellationToken cancellationToken = default)
    {
        var categories = await menuRepo.GetCategoriesAsync(cancellationToken);
        return categories.Select(c => new MenuCategoryAdminDto(c.Id, c.Name, c.SortOrder, c.MenuItems.Count)).ToList();
    }

    public async Task AddCategoryAsync(string categoryName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException("Category name is required.", nameof(categoryName));

        var trimmed = categoryName.Trim();
        if (await menuRepo.CategoryNameExistsAsync(trimmed, ct: cancellationToken))
            throw new InvalidOperationException("Category already exists.");

        var maxSort = await menuRepo.GetMaxCategorySortOrderAsync(cancellationToken);
        await menuRepo.AddCategoryAsync(new Category { Name = trimmed, SortOrder = maxSort + 1 }, cancellationToken);
    }

    public async Task RenameCategoryAsync(int categoryId, string categoryName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException("Category name is required.", nameof(categoryName));

        if (!await menuRepo.CategoryExistsAsync(categoryId, cancellationToken))
            throw new InvalidOperationException("Category not found.");

        var trimmed = categoryName.Trim();
        if (await menuRepo.CategoryNameExistsAsync(trimmed, categoryId, cancellationToken))
            throw new InvalidOperationException("Another category with this name already exists.");

        await menuRepo.RenameCategoryAsync(categoryId, trimmed, cancellationToken);
    }

    public async Task DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var cat = await menuRepo.GetCategoryByIdAsync(categoryId, cancellationToken);
        if (cat is null) return;
        await menuRepo.RemoveCategoryAsync(cat, cancellationToken);
    }

    public async Task AddFeaturedItemAsync(string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default)
    {
        ValidateMenuItem(name, price);
        var categoryId = await menuRepo.GetOrCreateFeaturedCategoryIdAsync(cancellationToken);
        await menuRepo.AddMenuItemAsync(BuildMenuItem(categoryId, name, description, price, imageUrl), cancellationToken);
    }

    public async Task AddMenuItemAsync(int categoryId, string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default)
    {
        ValidateMenuItem(name, price);
        if (!await menuRepo.CategoryExistsAsync(categoryId, cancellationToken))
            throw new InvalidOperationException("Category not found.");
        await menuRepo.AddMenuItemAsync(BuildMenuItem(categoryId, name, description, price, imageUrl), cancellationToken);
    }

    public async Task UpdateFeaturedItemAsync(int menuItemId, string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default)
    {
        ValidateMenuItem(name, price);
        if (await menuRepo.GetMenuItemByIdAsync(menuItemId, cancellationToken) is null)
            throw new InvalidOperationException("Menu item not found.");
        await menuRepo.UpdateMenuItemAsync(menuItemId, null,
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            decimal.Round(price, 2, MidpointRounding.AwayFromZero),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            cancellationToken);
    }

    public async Task UpdateMenuItemAsync(int menuItemId, int categoryId, string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default)
    {
        ValidateMenuItem(name, price);
        if (await menuRepo.GetMenuItemByIdAsync(menuItemId, cancellationToken) is null)
            throw new InvalidOperationException("Menu item not found.");
        if (!await menuRepo.CategoryExistsAsync(categoryId, cancellationToken))
            throw new InvalidOperationException("Category not found.");
        await menuRepo.UpdateMenuItemAsync(menuItemId, categoryId,
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            decimal.Round(price, 2, MidpointRounding.AwayFromZero),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            cancellationToken);
    }

    public async Task DeleteMenuItemAsync(int menuItemId, CancellationToken cancellationToken = default)
    {
        var item = await menuRepo.GetMenuItemByIdAsync(menuItemId, cancellationToken);
        if (item is null) return;
        await menuRepo.RemoveMenuItemAsync(item, cancellationToken);
    }

    private static void ValidateMenuItem(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (price <= 0m)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");
    }

    private static MenuItem BuildMenuItem(int categoryId, string name, string? description, decimal price, string? imageUrl) =>
        new()
        {
            CategoryId = categoryId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero),
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim()
        };

    private static void ApplyMenuItemUpdate(MenuItem item, string name, string? description, decimal price, string? imageUrl)
    {
        item.Name = name.Trim();
        item.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        item.Price = decimal.Round(price, 2, MidpointRounding.AwayFromZero);
        item.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
    }
}
