using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuntiesRecipe.Web.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderSummaryDto>> GetById(int id, CancellationToken ct)
    {
        var order = await orderService.GetOrderByIdAsync(id, ct);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IReadOnlyList<OrderSummaryDto>> GetAll(CancellationToken ct) =>
        await orderService.GetOrdersForAdminAsync(ct);

    [HttpGet("admin/status/{bucket}")]
    [Authorize(Roles = "Admin")]
    public async Task<IReadOnlyList<OrderSummaryDto>> GetByStatus(string bucket, CancellationToken ct) =>
        await orderService.GetOrdersByStatusForAdminAsync(bucket, ct);

    [HttpPost("admin/history")]
    [Authorize(Roles = "Admin")]
    public async Task<PagedOrderHistoryDto> GetHistory([FromBody] AdminOrderHistoryFilterDto filter, CancellationToken ct) =>
        await orderService.GetOrderHistoryForAdminAsync(filter, ct);

    [HttpPut("admin/{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status, CancellationToken ct)
    {
        await orderService.UpdateOrderStatusAsync(id, status, ct);
        return Ok();
    }
}
