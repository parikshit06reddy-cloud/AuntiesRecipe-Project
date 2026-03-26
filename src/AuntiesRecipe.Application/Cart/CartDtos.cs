namespace AuntiesRecipe.Application.Cart;

public sealed record CartItemDto(int MenuItemId, string Name, string? Description, decimal UnitPrice, int Quantity);

public sealed record CartDto(string CartId, IReadOnlyList<CartItemDto> Items, decimal Subtotal);

public sealed record CheckoutRequestDto(string PickupName, string PickupPhone, string? Notes);
