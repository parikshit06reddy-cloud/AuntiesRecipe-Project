using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Cart;
using Microsoft.AspNetCore.Mvc;

namespace AuntiesRecipe.Web.Controllers;

[ApiController]
[Route("api/cart")]
public sealed class CartController(ICartService cartService) : ControllerBase
{
    [HttpGet("{cartId}")]
    public async Task<CartDto> GetCart(string cartId, CancellationToken ct) =>
        await cartService.GetCartAsync(cartId, ct);

    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request, CancellationToken ct)
    {
        await cartService.AddToCartAsync(request.CartId, request.MenuItemId, request.Quantity, ct);
        return Ok();
    }

    [HttpPost("quantity")]
    public async Task<IActionResult> SetQuantity([FromBody] SetQuantityRequest request, CancellationToken ct)
    {
        await cartService.SetQuantityAsync(request.CartId, request.MenuItemId, request.Quantity, ct);
        return Ok();
    }

    [HttpDelete("{cartId}/items/{menuItemId:int}")]
    public async Task<IActionResult> RemoveItem(string cartId, int menuItemId, CancellationToken ct)
    {
        await cartService.RemoveFromCartAsync(cartId, menuItemId, ct);
        return NoContent();
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<int>> Checkout([FromBody] CheckoutApiRequest request, CancellationToken ct)
    {
        var orderId = await cartService.CheckoutAsync(
            request.CartId,
            new CheckoutRequestDto(request.PickupName, request.PickupPhone, request.Notes),
            request.CustomerUserId, ct);
        return Ok(orderId);
    }
}

public sealed record AddToCartRequest(string CartId, int MenuItemId, int Quantity);
public sealed record SetQuantityRequest(string CartId, int MenuItemId, int Quantity);
public sealed record CheckoutApiRequest(string CartId, string PickupName, string PickupPhone, string? Notes, string? CustomerUserId);
