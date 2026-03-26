namespace AuntiesRecipe.Domain.Entities;

public sealed class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int MenuItemId { get; set; }

    public string MenuItemName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public Order Order { get; set; } = null!;

    public decimal LineTotal => UnitPrice * Quantity;
}
