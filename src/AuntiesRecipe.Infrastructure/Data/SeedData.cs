using AuntiesRecipe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuntiesRecipe.Infrastructure.Data;

public static class SeedData
{
    private const string FeaturedCategoryName = "Featured Elixirs & Juices";

    public static async Task EnsureSeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        // We only manage the featured drinks for this demo.
        var desiredMenu = GetDesiredMenu();

        var featuredCategory = await db.Categories
            .FirstOrDefaultAsync(c => c.Name == FeaturedCategoryName, cancellationToken);

        var featuredItemCount = featuredCategory is null
            ? 0
            : await db.MenuItems.CountAsync(m => m.CategoryId == featuredCategory.Id, cancellationToken);

        // Only seed once for new databases; keep admin edits/additions intact on future startups.
        var needsReseed = featuredCategory is null || featuredItemCount == 0;

        if (!needsReseed)
        {
            return;
        }

        // Clear carts so no one ends up with references to old menu item IDs.
        db.CartItems.RemoveRange(db.CartItems);
        db.Carts.RemoveRange(db.Carts);
        await db.SaveChangesAsync(cancellationToken);

        // Create category if needed.
        if (featuredCategory is null)
        {
            featuredCategory = new Category { Name = FeaturedCategoryName, SortOrder = 1 };
            db.Categories.Add(featuredCategory);
            await db.SaveChangesAsync(cancellationToken);
        }

        db.MenuItems.AddRange(desiredMenu.Select(x => new MenuItem
        {
            CategoryId = featuredCategory.Id,
            Name = x.Name,
            Description = x.Description,
            Price = x.Price,
        }));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<MenuSeed> GetDesiredMenu() => new()
    {
        new MenuSeed(
            "Desert Detox Elixir",
            "A potent blend for cellular rejuvenation, featuring earthy prickly pear cactus and zesty Key lime for a refreshing, natural cleanse. (The bottles on the left with nopal paddles).",
            15.99m),
        new MenuSeed(
            "Immunity Boost Juice",
            "A citrus powerhouse crafted with sun-ripened oranges to fortify your natural defenses and brighten your morning. (The bottles in the center with oranges).",
            15.99m),
        new MenuSeed(
            "Vitality Hibiscus Elixir",
            "A vibrant source of antioxidants and natural energy, based on traditional Mexican hibiscus (jamaica) and sweetened with exotic pomegranate. (The bottle with hibiscus flowers and cinnamon).",
            15.99m),
        new MenuSeed(
            "Tropical Hydration Juice",
            "Essential electrolytes for pure hydration, using the hydrating power of mature pineapple and crisp watermelon. (The bottle on the right with pineapple chunks).",
            15.99m),

        new MenuSeed(
            "Sunrise Papaya-Mango Elixir",
            "A rich, velvety blend designed to reduce inflammation and boost vitamin C. It uses whole, velvety mangoes and creamy papaya to create a nutritious and satisfying drink that looks like the morning sun. (Reference to the large mango and cut papaya in the foreground).",
            15.99m),
        new MenuSeed(
            "Watermelon Mint Cooler",
            "Ultra-refreshing and perfect for cooling down, combining crisp, hydration-packed watermelon with aromatic garden mint sprigs. (Reference to the mint bundles and watermelon halves).",
            15.99m),
        new MenuSeed(
            "Hacienda Citrus Medley",
            "The ultimate refreshing \"agua fresca\", blending fresh-squeezed lime, orange, and a hint of prickly pear cactus to perfectly capture the essence of a Mexican hacienda courtyard. (Reference to the limes, oranges, and nopales in the courtyard setting).",
            15.99m),
        new MenuSeed(
            "Golden Pineapple Ginger Shots",
            "Small, powerful boosts designed to improve digestion, featuring concentrated mature pineapple juice and spicy, warming ginger root. (Reference to the smaller, concentrated bottle shapes and prominent pineapple).",
            15.99m),
    };

    private sealed record MenuSeed(string Name, string Description, decimal Price);
}
