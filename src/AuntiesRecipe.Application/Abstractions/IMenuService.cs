using AuntiesRecipe.Application.Menu;

namespace AuntiesRecipe.Application.Abstractions;

public interface IMenuService
{
    Task<IReadOnlyList<CategoryMenuDto>> GetMenuAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuItemDto>> GetFeaturedItemsForAdminAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuCategoryAdminDto>> GetCategoriesForAdminAsync(CancellationToken cancellationToken = default);
    Task AddCategoryAsync(string categoryName, CancellationToken cancellationToken = default);
    Task RenameCategoryAsync(int categoryId, string categoryName, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task AddFeaturedItemAsync(string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default);
    Task AddMenuItemAsync(int categoryId, string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default);
    Task UpdateFeaturedItemAsync(int menuItemId, string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default);
    Task UpdateMenuItemAsync(int menuItemId, int categoryId, string name, string? description, decimal price, string? imageUrl, CancellationToken cancellationToken = default);
    Task DeleteMenuItemAsync(int menuItemId, CancellationToken cancellationToken = default);
}
