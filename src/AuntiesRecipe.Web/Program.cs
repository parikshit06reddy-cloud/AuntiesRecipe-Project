using AuntiesRecipe.Infrastructure;
using AuntiesRecipe.Infrastructure.Data;
using AuntiesRecipe.Infrastructure.Identity;
using AuntiesRecipe.Web.Components;
using AuntiesRecipe.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

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

builder.Services.AddControllers();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();
var imageStorageRoot = ImageStoragePathResolver.Resolve(app.Environment, app.Configuration);
Directory.CreateDirectory(imageStorageRoot);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imageStorageRoot),
    RequestPath = "/images"
});
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

    // Keep demo credentials deterministic; tolerate concurrency conflicts in test hosts.
    try
    {
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(adminUser);
        await userManager.ResetPasswordAsync(adminUser, resetToken, adminPassword);
    }
    catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
    {
        // Another host instance already reset the password — safe to continue.
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }
}

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
