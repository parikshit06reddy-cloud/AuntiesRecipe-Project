using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuntiesRecipe.Web.Controllers;

[ApiController]
[Route("api/menu")]
public sealed class MenuController(IMenuService menuService) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<CategoryMenuDto>> GetMenu(CancellationToken ct) =>
        await menuService.GetMenuAsync(ct);

    [HttpGet("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<IReadOnlyList<MenuCategoryAdminDto>> GetCategories(CancellationToken ct) =>
        await menuService.GetCategoriesForAdminAsync(ct);

    [HttpPost("categories")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddCategory([FromBody] string name, CancellationToken ct)
    {
        await menuService.AddCategoryAsync(name, ct);
        return Ok();
    }

    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RenameCategory(int id, [FromBody] string name, CancellationToken ct)
    {
        await menuService.RenameCategoryAsync(id, name, ct);
        return Ok();
    }

    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
    {
        await menuService.DeleteCategoryAsync(id, ct);
        return NoContent();
    }

    [HttpPost("items")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddItem([FromBody] AddMenuItemRequest request, CancellationToken ct)
    {
        await menuService.AddMenuItemAsync(request.CategoryId, request.Name, request.Description, request.Price, request.ImageUrl, ct);
        return Ok();
    }

    [HttpPut("items/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateMenuItemRequest request, CancellationToken ct)
    {
        await menuService.UpdateMenuItemAsync(id, request.CategoryId, request.Name, request.Description, request.Price, request.ImageUrl, ct);
        return Ok();
    }

    [HttpDelete("items/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteItem(int id, CancellationToken ct)
    {
        await menuService.DeleteMenuItemAsync(id, ct);
        return NoContent();
    }
}

public sealed record AddMenuItemRequest(int CategoryId, string Name, string? Description, decimal Price, string? ImageUrl);
public sealed record UpdateMenuItemRequest(int CategoryId, string Name, string? Description, decimal Price, string? ImageUrl);
