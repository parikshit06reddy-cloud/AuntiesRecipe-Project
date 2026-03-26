namespace AuntiesRecipe.Domain.Entities;

public sealed class CartItem
{
    public int Id { get; set; }

    public string CartId { get; set; } = string.Empty;

    public int MenuItemId { get; set; }

    public int Quantity { get; set; }

    // Unit price snapshot at the time the item is added to the cart.
    public decimal UnitPrice { get; set; }

    public Cart Cart { get; set; } = null!;
}
