using Microsoft.AspNetCore.Http;

namespace AuntiesRecipe.Web.Services;

/// <summary>
/// Resolves the anonymous cart id from the <c>cart_id</c> cookie when an HTTP request is in scope,
/// and falls back to an in-memory id for the current Blazor circuit when <see cref="IHttpContextAccessor.HttpContext"/> is null
/// (common for interactive Blazor Server renders after the initial request).
/// </summary>
public sealed class CartSessionService
{
    public const string CookieName = "cart_id";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private string? _circuitFallbackCartId;

    public CartSessionService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetOrCreateCartId()
    {
        var http = _httpContextAccessor.HttpContext;
        if (http is not null)
        {
            var existing = http.Request.Cookies[CookieName];
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }

            var newId = Guid.NewGuid().ToString("N");
            http.Response.Cookies.Append(
                CookieName,
                newId,
                new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromDays(30)
                });

            return newId;
        }

        return _circuitFallbackCartId ??= Guid.NewGuid().ToString("N");
    }
}
