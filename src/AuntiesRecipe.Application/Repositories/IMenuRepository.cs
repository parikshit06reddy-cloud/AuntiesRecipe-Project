using AuntiesRecipe.Domain.Entities;

namespace AuntiesRecipe.Application.Repositories;

public interface IMenuRepository
{
    Task<List<Category>> GetCategoriesWithItemsAsync(CancellationToken ct = default);
    Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default);
    Task<Category?> GetCategoryByIdAsync(int categoryId, CancellationToken ct = default);
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken ct = default);
    Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null, CancellationToken ct = default);
    Task<int> GetMaxCategorySortOrderAsync(CancellationToken ct = default);
    Task AddCategoryAsync(Category category, CancellationToken ct = default);
    Task RenameCategoryAsync(int categoryId, string newName, CancellationToken ct = default);
    Task RemoveCategoryAsync(Category category, CancellationToken ct = default);
    Task<MenuItem?> GetMenuItemByIdAsync(int menuItemId, CancellationToken ct = default);
    Task<decimal> GetMenuItemPriceAsync(int menuItemId, CancellationToken ct = default);
    Task<Dictionary<int, string>> GetMenuItemNamesByIdsAsync(int[] ids, CancellationToken ct = default);
    Task AddMenuItemAsync(MenuItem item, CancellationToken ct = default);
    Task UpdateMenuItemAsync(int menuItemId, int? categoryId, string name, string? description, decimal price, string? imageUrl, CancellationToken ct = default);
    Task RemoveMenuItemAsync(MenuItem item, CancellationToken ct = default);
    Task<int> GetOrCreateFeaturedCategoryIdAsync(CancellationToken ct = default);
    Task<int> GetTodaySpecialMenuItemIdAsync(CancellationToken ct = default);
    Task<List<int>> GetMenuItemIdsByCategoryAsync(int categoryId, CancellationToken ct = default);
    Task RemoveCartItemsForMenuItemsAsync(List<int> menuItemIds, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
