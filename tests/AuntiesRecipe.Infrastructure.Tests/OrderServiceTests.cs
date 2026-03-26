using AuntiesRecipe.Application.Orders;
using AuntiesRecipe.Application.Services;
using AuntiesRecipe.Domain.Entities;
using AuntiesRecipe.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuntiesRecipe.Infrastructure.Tests;

public sealed class OrderServiceTests
{
    [Fact]
    public async Task GetOrderHistoryForAdminAsync_AppliesFiltersAndPaging()
    {
        using var factory = new SqliteDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Orders.AddRange(
                new Order
                {
                    PickupName = "Alice",
                    PickupPhone = "111",
                    CreatedAtUtc = new DateTime(2026, 03, 20, 10, 0, 0, DateTimeKind.Utc),
                    TokenDateUtc = new DateTime(2026, 03, 20, 0, 0, 0, DateTimeKind.Utc),
                    DailyTokenNumber = 1,
                    Status = OrderStatus.Completed,
                    Items = [new OrderItem { MenuItemId = 1, MenuItemName = "A", Quantity = 1, UnitPrice = 4m }]
                },
                new Order
                {
                    PickupName = "Bob",
                    PickupPhone = "222",
                    CreatedAtUtc = new DateTime(2026, 03, 21, 10, 0, 0, DateTimeKind.Utc),
                    TokenDateUtc = new DateTime(2026, 03, 21, 0, 0, 0, DateTimeKind.Utc),
                    DailyTokenNumber = 2,
                    Status = OrderStatus.Preparing,
                    Items = [new OrderItem { MenuItemId = 2, MenuItemName = "B", Quantity = 1, UnitPrice = 5m }]
                },
                new Order
                {
                    PickupName = "Alice",
                    PickupPhone = "333",
                    CreatedAtUtc = new DateTime(2026, 03, 22, 10, 0, 0, DateTimeKind.Utc),
                    TokenDateUtc = new DateTime(2026, 03, 22, 0, 0, 0, DateTimeKind.Utc),
                    DailyTokenNumber = 3,
                    Status = OrderStatus.Completed,
                    Items = [new OrderItem { MenuItemId = 3, MenuItemName = "C", Quantity = 2, UnitPrice = 3m }]
                });
            await db.SaveChangesAsync();
        }

        var orderRepo = new OrderRepository(factory);
        var service = new OrderAppService(orderRepo, NullLogger<OrderAppService>.Instance);
        var filtered = await service.GetOrderHistoryForAdminAsync(new AdminOrderHistoryFilterDto(
            PickupName: "Alice", PickupPhone: null, TokenNumber: null,
            FromDateUtc: new DateTime(2026, 03, 20, 0, 0, 0, DateTimeKind.Utc),
            ToDateUtc: new DateTime(2026, 03, 22, 0, 0, 0, DateTimeKind.Utc),
            Status: "Completed", Page: 1, PageSize: 1));

        filtered.Items.Should().HaveCount(1);
        filtered.Items[0].PickupName.Should().Be("Alice");
        filtered.Items[0].Status.Should().Be("Completed");
        filtered.TotalCount.Should().Be(2);
        filtered.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_UpdatesPersistedStatus()
    {
        using var factory = new SqliteDbFactory();
        await using (var db = await factory.CreateDbContextAsync())
        {
            db.Orders.Add(new Order
            {
                PickupName = "Test",
                PickupPhone = "123",
                CreatedAtUtc = DateTime.UtcNow,
                TokenDateUtc = DateTime.UtcNow.Date,
                DailyTokenNumber = 1,
                Status = OrderStatus.Placed,
                Items = [new OrderItem { MenuItemId = 1, MenuItemName = "X", Quantity = 1, UnitPrice = 1m }]
            });
            await db.SaveChangesAsync();
        }

        var orderRepo = new OrderRepository(factory);
        var service = new OrderAppService(orderRepo, NullLogger<OrderAppService>.Instance);
        await service.UpdateOrderStatusAsync(1, "Completed");

        await using var verifyDb = await factory.CreateDbContextAsync();
        verifyDb.Orders.Single().Status.Should().Be(OrderStatus.Completed);
    }
}
