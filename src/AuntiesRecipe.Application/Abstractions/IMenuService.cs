using AuntiesRecipe.Application.Menu;

namespace AuntiesRecipe.Application.Abstractions;

public interface IMenuService
{
    Task<IReadOnlyList<CategoryMenuDto>> GetMenuAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuItemDto>> GetFeaturedItemsForAdminAsync(CancellationToken cancellationToken = default);
    Task AddFeaturedItemAsync(string name, string? description, decimal price, CancellationToken cancellationToken = default);
    Task UpdateFeaturedItemAsync(int menuItemId, string name, string? description, decimal price, CancellationToken cancellationToken = default);
}
