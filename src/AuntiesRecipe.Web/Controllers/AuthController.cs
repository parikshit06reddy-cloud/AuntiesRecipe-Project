using AuntiesRecipe.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AuntiesRecipe.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login([FromForm] string usernameOrEmail, [FromForm] string password, [FromForm] string? returnUrl)
    {
        var input = (usernameOrEmail ?? string.Empty).Trim();
        var user = input.Contains('@')
            ? await userManager.FindByEmailAsync(input)
            : await userManager.FindByNameAsync(input);

        if (user is null)
        {
            logger.LogWarning("Login failed: user not found for input {Input}", input);
            return Redirect("/login?error=1");
        }

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            logger.LogWarning("Login failed: invalid credentials for user {UserName}", user.UserName);
            return Redirect("/login?error=1");
        }

        if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.TryCreate(returnUrl, UriKind.Relative, out _))
            returnUrl = "/";

        logger.LogInformation("Login succeeded for user {UserName}; redirecting to {ReturnUrl}", user.UserName, returnUrl);
        return Redirect(returnUrl);
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        logger.LogInformation("User logout completed.");
        return Redirect("/");
    }
}
