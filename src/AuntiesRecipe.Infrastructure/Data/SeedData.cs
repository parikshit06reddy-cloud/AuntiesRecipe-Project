using AuntiesRecipe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuntiesRecipe.Infrastructure.Data;

public static class SeedData
{
    private const string CookiesCategoryName = "Cookies";
    private const string JuicesCategoryName = "Featured Elixirs & Juices";
    private const string FruitSaladsCategoryName = "Fruit Salads";
    private const string CombosCategoryName = "Combos";
    private const string SpecialOffersCategoryName = "Special Offers";

    public static async Task EnsureSeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureCategorySeedAsync(db, CookiesCategoryName, 1, GetCookieMenu(), cancellationToken);
        await EnsureCategorySeedAsync(db, JuicesCategoryName, 2, GetJuiceMenu(), cancellationToken);
        await EnsureCategorySeedAsync(db, FruitSaladsCategoryName, 3, GetFruitSaladMenu(), cancellationToken);
        await EnsureCategorySeedAsync(db, CombosCategoryName, 4, GetComboMenu(), cancellationToken);
        await EnsureCategorySeedAsync(db, SpecialOffersCategoryName, 5, GetSpecialOffersMenu(), cancellationToken);
    }

    private static async Task EnsureCategorySeedAsync(
        AppDbContext db,
        string categoryName,
        int sortOrder,
        IReadOnlyList<MenuSeed> desiredItems,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == categoryName, cancellationToken);
        if (category is null)
        {
            category = new Category { Name = categoryName, SortOrder = sortOrder };
            db.Categories.Add(category);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (category.SortOrder != sortOrder)
        {
            category.SortOrder = sortOrder;
            await db.SaveChangesAsync(cancellationToken);
        }

        var existingItems = await db.MenuItems
            .Where(m => m.CategoryId == category.Id)
            .ToListAsync(cancellationToken);

        var existingNames = existingItems.Select(x => x.Name).ToList();

        var updatedAny = false;
        foreach (var existing in existingItems)
        {
            var desired = desiredItems.FirstOrDefault(i => string.Equals(i.Name, existing.Name, StringComparison.OrdinalIgnoreCase));
            if (desired is not null && string.IsNullOrWhiteSpace(existing.ImageUrl) && !string.IsNullOrWhiteSpace(desired.ImageUrl))
            {
                existing.ImageUrl = desired.ImageUrl;
                updatedAny = true;
            }
        }

        if (updatedAny)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        var missing = desiredItems
            .Where(i => !existingNames.Contains(i.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        db.MenuItems.AddRange(missing.Select(x => new MenuItem
        {
            CategoryId = category.Id,
            Name = x.Name,
            Description = x.Description,
            Price = x.Price,
            ImageUrl = x.ImageUrl,
        }));

        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<MenuSeed> GetCookieMenu() => new()
    {
        new MenuSeed(
            "Golden Hour Gingersnaps",
            "Warm spiced gingersnaps with a classic crackled top and deep molasses notes.",
            15.99m,
            "https://source.unsplash.com/1200x900/?gingersnap,cookie"),
        new MenuSeed(
            "Midnight Velvet Buttons",
            "Dark cocoa cookie buttons with a soft, fudgy center.",
            15.99m,
            "https://source.unsplash.com/1200x900/?chocolate,cookie"),
        new MenuSeed(
            "Sea Salt Sunsets",
            "Sweet-salty salted caramel cookie with a premium artisanal finish.",
            15.99m,
            "https://source.unsplash.com/1200x900/?salted,caramel,cookie"),
        new MenuSeed(
            "Whispering Willow Shortbread",
            "Delicate buttery shortbread with a light, melt-in-your-mouth texture.",
            15.99m,
            "https://source.unsplash.com/1200x900/?shortbread,biscuit"),
        new MenuSeed(
            "Honeyed Hearthstone Rounds",
            "Rustic hearty cookies inspired by oatmeal-raisin and nutty hearth-baked flavors.",
            15.99m,
            "https://source.unsplash.com/1200x900/?oatmeal,cookie"),
    };

    private static List<MenuSeed> GetFruitSaladMenu() => new()
    {
        new MenuSeed(
            "Ambrosia Aura",
            "Creamy dreamy fruit salad with mandarin, pineapple, coconut, and cloud-like texture.",
            15.99m,
            "https://source.unsplash.com/1200x900/?fruit,salad,creamy"),
        new MenuSeed(
            "Ruby Riviera Medley",
            "Vacation-style medley of strawberries, raspberries, and pomegranate with a fresh finish.",
            15.99m,
            "https://source.unsplash.com/1200x900/?berry,fruit,salad"),
        new MenuSeed(
            "Golden Hour Harvest",
            "Bright tropical blend of mango, pineapple, and papaya at peak ripeness.",
            15.99m,
            "https://source.unsplash.com/1200x900/?tropical,fruit,salad"),
        new MenuSeed(
            "Twilight Orchard Toss",
            "Deeper orchard profile with grapes, plums, and rich evening-fruit notes.",
            15.99m,
            "https://source.unsplash.com/1200x900/?grapes,plum,fruit"),
        new MenuSeed(
            "Morning Dew Sparkler",
            "Crisp refreshing fruit salad with zesty citrus and a wake-up palate lift.",
            15.99m,
            "https://source.unsplash.com/1200x900/?citrus,fruit,salad"),
    };

    private static List<MenuSeed> GetJuiceMenu() => new()
    {
        new MenuSeed(
            "Desert Detox Elixir",
            "A potent blend for cellular rejuvenation, featuring earthy prickly pear cactus and zesty Key lime for a refreshing, natural cleanse. (The bottles on the left with nopal paddles).",
            15.99m,
            "https://source.unsplash.com/1200x900/?green,juice,bottle"),
        new MenuSeed(
            "Immunity Boost Juice",
            "A citrus powerhouse crafted with sun-ripened oranges to fortify your natural defenses and brighten your morning. (The bottles in the center with oranges).",
            15.99m,
            "https://source.unsplash.com/1200x900/?orange,juice"),
        new MenuSeed(
            "Vitality Hibiscus Elixir",
            "A vibrant source of antioxidants and natural energy, based on traditional Mexican hibiscus (jamaica) and sweetened with exotic pomegranate. (The bottle with hibiscus flowers and cinnamon).",
            15.99m,
            "https://source.unsplash.com/1200x900/?hibiscus,drink"),
        new MenuSeed(
            "Tropical Hydration Juice",
            "Essential electrolytes for pure hydration, using the hydrating power of mature pineapple and crisp watermelon. (The bottle on the right with pineapple chunks).",
            15.99m,
            "https://source.unsplash.com/1200x900/?pineapple,juice"),

        new MenuSeed(
            "Sunrise Papaya-Mango Elixir",
            "A rich, velvety blend designed to reduce inflammation and boost vitamin C. It uses whole, velvety mangoes and creamy papaya to create a nutritious and satisfying drink that looks like the morning sun. (Reference to the large mango and cut papaya in the foreground).",
            15.99m,
            "https://source.unsplash.com/1200x900/?mango,papaya,juice"),
        new MenuSeed(
            "Watermelon Mint Cooler",
            "Ultra-refreshing and perfect for cooling down, combining crisp, hydration-packed watermelon with aromatic garden mint sprigs. (Reference to the mint bundles and watermelon halves).",
            15.99m,
            "https://source.unsplash.com/1200x900/?watermelon,mint,drink"),
        new MenuSeed(
            "Hacienda Citrus Medley",
            "The ultimate refreshing \"agua fresca\", blending fresh-squeezed lime, orange, and a hint of prickly pear cactus to perfectly capture the essence of a Mexican hacienda courtyard. (Reference to the limes, oranges, and nopales in the courtyard setting).",
            15.99m,
            "https://source.unsplash.com/1200x900/?citrus,agua,fresca"),
        new MenuSeed(
            "Golden Pineapple Ginger Shots",
            "Small, powerful boosts designed to improve digestion, featuring concentrated mature pineapple juice and spicy, warming ginger root. (Reference to the smaller, concentrated bottle shapes and prominent pineapple).",
            15.99m,
            "https://source.unsplash.com/1200x900/?ginger,shot,juice"),
    };

    private static List<MenuSeed> GetComboMenu() => new()
    {
        new MenuSeed(
            "Sunrise Combo",
            "Any juice + one cookie + mini fruit salad at a bundle price.",
            22.99m,
            "https://source.unsplash.com/1200x900/?juice,cookie,combo"),
        new MenuSeed(
            "Family Table Combo",
            "Two juices, two fruit salads, and four cookies for sharing.",
            44.99m,
            "https://source.unsplash.com/1200x900/?family,meal,table"),
    };

    private static List<MenuSeed> GetSpecialOffersMenu() => new()
    {
        new MenuSeed(
            "Today Saver Pack",
            "Chef-picked seasonal drink and snack combo at a limited-time discount.",
            18.99m,
            "https://source.unsplash.com/1200x900/?special,offer,food"),
        new MenuSeed(
            "Weekend Fiesta Deal",
            "A vibrant weekend bundle with premium juice and sweet pairings.",
            24.99m,
            "https://source.unsplash.com/1200x900/?weekend,food,deal"),
    };

    private sealed record MenuSeed(string Name, string Description, decimal Price, string? ImageUrl);
}
