using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Domain.Entities;
using AuntiesRecipe.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuntiesRecipe.Web.IntegrationTests;

public sealed class AdminOrderOperationsIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task OrderService_StatusBucketsAndUpdates_WorkInHostedApp()
    {
        using var scope = factory.Services.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            db.Orders.RemoveRange(db.Orders);
            await db.SaveChangesAsync();

            db.Orders.AddRange(
                new Order
                {
                    PickupName = "One",
                    PickupPhone = "111",
                    Status = OrderStatus.Placed,
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
                    TokenDateUtc = DateTime.UtcNow.Date,
                    DailyTokenNumber = 1,
                    Items = [new OrderItem { MenuItemId = 1, MenuItemName = "A", UnitPrice = 5m, Quantity = 1 }]
                },
                new Order
                {
                    PickupName = "Two",
                    PickupPhone = "222",
                    Status = OrderStatus.Preparing,
                    CreatedAtUtc = DateTime.UtcNow.AddMinutes(-20),
                    TokenDateUtc = DateTime.UtcNow.Date,
                    DailyTokenNumber = 2,
                    Items = [new OrderItem { MenuItemId = 2, MenuItemName = "B", UnitPrice = 6m, Quantity = 1 }]
                });
            await db.SaveChangesAsync();
        }

        var todo = await orderService.GetOrdersByStatusForAdminAsync("todo");
        var inprogress = await orderService.GetOrdersByStatusForAdminAsync("inprogress");
        todo.Should().HaveCount(1);
        inprogress.Should().HaveCount(1);

        await orderService.UpdateOrderStatusAsync(todo[0].OrderId, "Completed");
        var completed = await orderService.GetOrdersByStatusForAdminAsync("completed");
        completed.Should().ContainSingle(x => x.OrderId == todo[0].OrderId);
    }
}
