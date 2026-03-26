namespace AuntiesRecipe.Domain.Entities;

public enum OrderStatus
{
    Placed = 0,
    Preparing = 1,
    ReadyForPickup = 2,
    Completed = 3,
    Cancelled = 9
}

public sealed class Order
{
    public int Id { get; set; }

    public string? CustomerUserId { get; set; }

    // Pickup token: starts at 1 each day (based on TokenDateUtc).
    public DateTime TokenDateUtc { get; set; }
    public int DailyTokenNumber { get; set; }

    // Denormalized checkout details (optional but good for a demo).
    public string PickupName { get; set; } = string.Empty;
    public string PickupPhone { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public OrderStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ICollection<OrderItem> Items { get; init; } = new List<OrderItem>();
}
