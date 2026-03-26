using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Repositories;
using AuntiesRecipe.Application.Services;
using AuntiesRecipe.Infrastructure.Data;
using AuntiesRecipe.Infrastructure.Repositories;
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

        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IBusinessProfileRepository, BusinessProfileRepository>();

        services.AddScoped<ICartService, CartAppService>();
        services.AddScoped<IMenuService, MenuAppService>();
        services.AddScoped<IOrderService, OrderAppService>();
        services.AddScoped<IBusinessProfileService, BusinessProfileAppService>();

        return services;
    }
}
