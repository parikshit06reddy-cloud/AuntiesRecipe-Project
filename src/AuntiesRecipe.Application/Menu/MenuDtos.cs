namespace AuntiesRecipe.Application.Menu;

public sealed record MenuItemDto(int Id, string Name, string? Description, decimal Price, string? ImageUrl);

public sealed record CategoryMenuDto(int Id, string Name, int SortOrder, IReadOnlyList<MenuItemDto> Items);

public sealed record MenuCategoryAdminDto(int Id, string Name, int SortOrder, int ItemCount);
