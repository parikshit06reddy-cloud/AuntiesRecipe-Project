using AuntiesRecipe.Application.Cart;

namespace AuntiesRecipe.Application.Abstractions;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string cartId, CancellationToken cancellationToken = default);
    Task AddToCartAsync(string cartId, int menuItemId, int quantity, CancellationToken cancellationToken = default);
    Task SetQuantityAsync(string cartId, int menuItemId, int quantity, CancellationToken cancellationToken = default);
    Task RemoveFromCartAsync(string cartId, int menuItemId, CancellationToken cancellationToken = default);
    Task<int> CheckoutAsync(string cartId, CheckoutRequestDto request, string? customerUserId, CancellationToken cancellationToken = default);
}
