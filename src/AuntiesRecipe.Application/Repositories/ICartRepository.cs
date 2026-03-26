namespace AuntiesRecipe.Application.Repositories;

public interface ICartRepository
{
    Task<Domain.Entities.Cart> GetOrCreateAsync(string cartId, CancellationToken ct = default);
    Task<List<Domain.Entities.CartItem>> GetItemsAsync(string cartId, CancellationToken ct = default);
    Task<Domain.Entities.CartItem?> GetItemAsync(string cartId, int menuItemId, CancellationToken ct = default);
    Task AddItemAsync(Domain.Entities.CartItem item, CancellationToken ct = default);
    Task UpdateItemQuantityAsync(string cartId, int menuItemId, int quantity, decimal unitPrice, CancellationToken ct = default);
    Task RemoveItemAsync(string cartId, int menuItemId, CancellationToken ct = default);
    Task RemoveAllItemsAsync(string cartId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
