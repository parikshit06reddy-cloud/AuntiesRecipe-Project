namespace AuntiesRecipe.Domain.Entities;

public sealed class Cart
{
    public string Id { get; set; } = string.Empty; // guid string

    // Optional: set when the cart is associated with a logged-in customer.
    public string? CustomerUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<CartItem> Items { get; init; } = new List<CartItem>();
}
