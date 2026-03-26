using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AuntiesRecipe.Web.IntegrationTests;

public sealed class AuthEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToReturnUrl()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["usernameOrEmail"] = "admin.net",
            ["password"] = "admin.password",
            ["returnUrl"] = "/checkout"
        });

        var response = await client.PostAsync("/api/auth/login", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Be("/checkout");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_RedirectsToError()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["usernameOrEmail"] = "admin.net",
            ["password"] = "wrong-password"
        });

        var response = await client.PostAsync("/api/auth/login", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Be("/login?error=1");
    }

    [Fact]
    public async Task Login_WithUnsafeReturnUrl_RedirectsToHome()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["usernameOrEmail"] = "admin.net",
            ["password"] = "admin.password",
            ["returnUrl"] = "https://evil.example.com"
        });

        var response = await client.PostAsync("/api/auth/login", content);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Be("/");
    }

    [Fact]
    public async Task Logout_AlwaysRedirectsHome()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/auth/logout");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.OriginalString.Should().Be("/");
    }
}
