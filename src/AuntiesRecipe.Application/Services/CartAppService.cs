using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Cart;
using AuntiesRecipe.Application.Repositories;
using AuntiesRecipe.Domain.Entities;
using Microsoft.Extensions.Logging;

using CartEntity = AuntiesRecipe.Domain.Entities.Cart;
using CartItemEntity = AuntiesRecipe.Domain.Entities.CartItem;

namespace AuntiesRecipe.Application.Services;

public sealed class CartAppService(
    ICartRepository cartRepo,
    IMenuRepository menuRepo,
    IOrderRepository orderRepo,
    ILogger<CartAppService> logger) : ICartService
{
    private const decimal DailySpecialPrice = 7.99m;

    public async Task<CartDto> GetCartAsync(string cartId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
            throw new ArgumentException("cartId is required", nameof(cartId));

        await cartRepo.GetOrCreateAsync(cartId, cancellationToken);

        var cartItems = await cartRepo.GetItemsAsync(cartId, cancellationToken);
        if (cartItems.Count == 0)
            return new CartDto(cartId, Array.Empty<CartItemDto>(), 0m);

        var menuItemIds = cartItems.Select(i => i.MenuItemId).Distinct().ToArray();
        var menuNames = await menuRepo.GetMenuItemNamesByIdsAsync(menuItemIds, cancellationToken);

        var items = new List<CartItemDto>(cartItems.Count);
        foreach (var ci in cartItems)
        {
            if (!menuNames.TryGetValue(ci.MenuItemId, out var name)) continue;
            items.Add(new CartItemDto(ci.MenuItemId, name, null, ci.UnitPrice, ci.Quantity));
        }

        return new CartDto(cartId, items, items.Sum(i => i.UnitPrice * i.Quantity));
    }

    public async Task AddToCartAsync(string cartId, int menuItemId, int quantity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
            throw new ArgumentException("cartId is required", nameof(cartId));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "quantity must be > 0");

        await cartRepo.GetOrCreateAsync(cartId, cancellationToken);
        var unitPrice = await ResolveUnitPriceAsync(menuItemId, cancellationToken);
        var existing = await cartRepo.GetItemAsync(cartId, menuItemId, cancellationToken);

        if (existing is null)
        {
            await cartRepo.AddItemAsync(new CartItemEntity
            {
                CartId = cartId,
                MenuItemId = menuItemId,
                Quantity = quantity,
                UnitPrice = unitPrice
            }, cancellationToken);
        }
        else
        {
            await cartRepo.UpdateItemQuantityAsync(cartId, menuItemId, existing.Quantity + quantity, unitPrice, cancellationToken);
        }
    }

    public async Task SetQuantityAsync(string cartId, int menuItemId, int quantity, CancellationToken cancellationToken = default)
    {
        var existing = await cartRepo.GetItemAsync(cartId, menuItemId, cancellationToken);
        if (existing is null)
        {
            if (quantity <= 0) return;
            await cartRepo.GetOrCreateAsync(cartId, cancellationToken);
            var unitPrice = await ResolveUnitPriceAsync(menuItemId, cancellationToken);
            await cartRepo.AddItemAsync(new CartItemEntity
            {
                CartId = cartId,
                MenuItemId = menuItemId,
                Quantity = quantity,
                UnitPrice = unitPrice
            }, cancellationToken);
        }
        else
        {
            if (quantity <= 0)
                await cartRepo.RemoveItemAsync(cartId, menuItemId, cancellationToken);
            else
                await cartRepo.UpdateItemQuantityAsync(cartId, menuItemId, quantity, existing.UnitPrice, cancellationToken);
        }
    }

    public async Task RemoveFromCartAsync(string cartId, int menuItemId, CancellationToken cancellationToken = default)
    {
        await cartRepo.RemoveItemAsync(cartId, menuItemId, cancellationToken);
    }

    public async Task<int> CheckoutAsync(string cartId, CheckoutRequestDto request, string? customerUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cartId))
            throw new ArgumentException("cartId is required", nameof(cartId));
        if (string.IsNullOrWhiteSpace(request.PickupName))
            throw new ArgumentException("PickupName is required", nameof(request));
        if (string.IsNullOrWhiteSpace(request.PickupPhone))
            throw new ArgumentException("PickupPhone is required", nameof(request));

        logger.LogInformation("Checkout started for cart {CartId} by user {UserId}", cartId, customerUserId ?? "anonymous");

        var cartItems = await cartRepo.GetItemsAsync(cartId, cancellationToken);
        if (cartItems.Count == 0)
            throw new InvalidOperationException("Cart is empty");

        var menuItemIds = cartItems.Select(i => i.MenuItemId).Distinct().ToArray();
        var menuNames = await menuRepo.GetMenuItemNamesByIdsAsync(menuItemIds, cancellationToken);

        var tokenDateUtc = DateTime.UtcNow.Date;
        var nextToken = await orderRepo.GetNextDailyTokenAsync(tokenDateUtc, cancellationToken);

        var order = new Order
        {
            CustomerUserId = customerUserId,
            TokenDateUtc = tokenDateUtc,
            DailyTokenNumber = nextToken,
            PickupName = request.PickupName,
            PickupPhone = request.PickupPhone,
            Notes = request.Notes,
            Status = OrderStatus.Placed,
            CreatedAtUtc = DateTime.UtcNow,
            Items = cartItems.Select(ci => new OrderItem
            {
                MenuItemId = ci.MenuItemId,
                MenuItemName = menuNames.TryGetValue(ci.MenuItemId, out var n) ? n : "Item",
                UnitPrice = ci.UnitPrice,
                Quantity = ci.Quantity
            }).ToList()
        };

        await orderRepo.AddAsync(order, cancellationToken);
        await cartRepo.RemoveAllItemsAsync(cartId, cancellationToken);

        logger.LogInformation(
            "Checkout completed for cart {CartId}. Created order {OrderId} token {TokenDate}/{TokenNumber} with {ItemCount} items",
            cartId, order.Id, order.TokenDateUtc.ToString("yyyy-MM-dd"), order.DailyTokenNumber, cartItems.Count);

        return order.Id;
    }

    private async Task<decimal> ResolveUnitPriceAsync(int menuItemId, CancellationToken ct)
    {
        var todaySpecialId = await menuRepo.GetTodaySpecialMenuItemIdAsync(ct);
        var price = await menuRepo.GetMenuItemPriceAsync(menuItemId, ct);
        if (price <= 0m) price = 15.99m;
        return menuItemId == todaySpecialId ? DailySpecialPrice : price;
    }
}
