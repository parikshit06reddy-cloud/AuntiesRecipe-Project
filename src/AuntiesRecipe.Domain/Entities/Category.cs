namespace AuntiesRecipe.Domain.Entities;

public sealed class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<MenuItem> MenuItems { get; init; } = new List<MenuItem>();
}
