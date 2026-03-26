using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Infrastructure.Data;
using AuntiesRecipe.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuntiesRecipe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=auntiesrecipe.db";

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, CartService>();
        services.AddScoped<IBusinessProfileService, BusinessProfileService>();

        return services;
    }
}
