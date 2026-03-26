using AuntiesRecipe.Application.Cart;
using AuntiesRecipe.Application.Services;
using AuntiesRecipe.Domain.Entities;
using AuntiesRecipe.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuntiesRecipe.Infrastructure.Tests;

public sealed class CartServiceTests
{
    [Fact]
    public async Task CheckoutAsync_CreatesOrderAndClearsCart()
    {
        using var factory = new SqliteDbFactory();
        await using (var seedDb = await factory.CreateDbContextAsync())
        {
            var category = new Category { Name = "Featured Elixirs & Juices", SortOrder = 1 };
            seedDb.Categories.Add(category);
            await seedDb.SaveChangesAsync();

            seedDb.MenuItems.Add(new MenuItem
            {
                CategoryId = category.Id,
                Name = "Horchata",
                Description = "Rice and cinnamon",
                Price = 5.50m
            });
            await seedDb.SaveChangesAsync();
        }

        var cartRepo = new CartRepository(factory);
        var menuRepo = new MenuRepository(factory);
        var orderRepo = new OrderRepository(factory);
        var service = new CartAppService(cartRepo, menuRepo, orderRepo, NullLogger<CartAppService>.Instance);

        await service.AddToCartAsync("cart-1", menuItemId: 1, quantity: 2);

        var orderId = await service.CheckoutAsync(
            "cart-1",
            new CheckoutRequestDto("Parikshit", "8067022014", "No ice"),
            customerUserId: "user-1");

        orderId.Should().BeGreaterThan(0);

        await using var verifyDb = await factory.CreateDbContextAsync();
        verifyDb.Orders.Should().ContainSingle();
        verifyDb.OrderItems.Should().HaveCountGreaterThan(0);
        verifyDb.CartItems.Should().BeEmpty();

        var order = verifyDb.Orders.Single();
        order.PickupName.Should().Be("Parikshit");
        order.DailyTokenNumber.Should().BeGreaterThan(0);
    }
}
