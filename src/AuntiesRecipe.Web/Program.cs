using AuntiesRecipe.Infrastructure;
using AuntiesRecipe.Infrastructure.Data;
using AuntiesRecipe.Infrastructure.Identity;
using AuntiesRecipe.Web.Components;
using AuntiesRecipe.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=auntiesrecipe.db";

// Menu, cart, orders, etc.
builder.Services.AddInfrastructure(builder.Configuration);

// Identity (customer + admin/owner logins)
builder.Services.AddDbContext<AppIdentityDbContext>(options => options.UseSqlite(connectionString));

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.AddAuthorization();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // Keep demo friction low.
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireDigit = false;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppIdentityDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CartSessionService>();
builder.Services.AddScoped<AuthenticationStateProvider, HttpContextAuthenticationStateProvider>();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

await using (var scope = app.Services.CreateAsyncScope())
{
    // Migrate menu + cart + orders.
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
    await SeedData.EnsureSeedAsync(db);

    // Migrate Identity.
    var idDb = scope.ServiceProvider.GetRequiredService<AppIdentityDbContext>();
    await idDb.Database.MigrateAsync();

    // Ensure a default admin user exists for demos.
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    var adminUserName = builder.Configuration["AdminDefaults:UserName"] ?? "admin.net";
    var adminEmail = builder.Configuration["AdminDefaults:Email"] ?? "admin.net@aunties.local";
    var adminPassword = builder.Configuration["AdminDefaults:Password"] ?? "admin.password";

    var adminUser = await userManager.FindByNameAsync(adminUserName);
    if (adminUser is null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail
        };

        var createResult = await userManager.CreateAsync(adminUser, adminPassword);
        if (!createResult.Succeeded)
        {
            var duplicate = createResult.Errors.Any(e => e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase));
            if (!duplicate)
            {
                throw new InvalidOperationException($"Default admin creation failed: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
            }

            adminUser = await userManager.FindByNameAsync(adminUserName)
                ?? throw new InvalidOperationException($"Default admin creation failed: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
        }
    }

    // Keep demo credentials deterministic.
    var resetToken = await userManager.GeneratePasswordResetTokenAsync(adminUser);
    var resetResult = await userManager.ResetPasswordAsync(adminUser, resetToken, adminPassword);
    if (!resetResult.Succeeded)
    {
        throw new InvalidOperationException($"Default admin password reset failed: {string.Join("; ", resetResult.Errors.Select(e => e.Description))}");
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/auth/login", async (
    [FromForm] string usernameOrEmail,
    [FromForm] string password,
    [FromForm] string? returnUrl,
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager) =>
{
    var input = (usernameOrEmail ?? string.Empty).Trim();
    var user = input.Contains('@')
        ? await userManager.FindByEmailAsync(input)
        : await userManager.FindByNameAsync(input);

    if (user is null)
    {
        return Results.Redirect("/login?error=1");
    }

    var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: false, lockoutOnFailure: false);
    if (!result.Succeeded)
    {
        return Results.Redirect("/login?error=1");
    }

    if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.TryCreate(returnUrl, UriKind.Relative, out _))
    {
        returnUrl = "/";
    }

    return Results.Redirect(returnUrl);
})
.DisableAntiforgery();

app.MapGet("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
});

app.Run();

public partial class Program;
