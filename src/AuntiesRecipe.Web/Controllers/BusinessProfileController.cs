using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Business;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuntiesRecipe.Web.Controllers;

[ApiController]
[Route("api/profile")]
public sealed class BusinessProfileController(IBusinessProfileService profileService) : ControllerBase
{
    [HttpGet]
    public async Task<BusinessProfileDto> Get(CancellationToken ct) =>
        await profileService.GetAsync(ct);

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update([FromBody] BusinessProfileDto dto, CancellationToken ct)
    {
        await profileService.UpdateAsync(dto, ct);
        return Ok();
    }
}
