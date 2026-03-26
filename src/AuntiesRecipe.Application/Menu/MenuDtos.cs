namespace AuntiesRecipe.Application.Menu;

public sealed record MenuItemDto(int Id, string Name, string? Description, decimal Price);

public sealed record CategoryMenuDto(int Id, string Name, int SortOrder, IReadOnlyList<MenuItemDto> Items);
