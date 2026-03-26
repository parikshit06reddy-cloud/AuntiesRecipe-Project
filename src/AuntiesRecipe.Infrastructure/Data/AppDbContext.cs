using AuntiesRecipe.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuntiesRecipe.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<BusinessProfile> BusinessProfiles => Set<BusinessProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(120);
            entity.HasMany(e => e.MenuItems)
                .WithOne(e => e.Category)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(160);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.Price).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(64);
            entity.Property(e => e.CreatedAtUtc);

            entity.HasMany(e => e.Items)
                .WithOne(e => e.Cart)
                .HasForeignKey(e => e.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPrice).HasPrecision(12, 2);

            entity.HasIndex(e => new { e.CartId, e.MenuItemId }).IsUnique();
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<int>();
            entity.Property(e => e.PickupName).HasMaxLength(120);
            entity.Property(e => e.PickupPhone).HasMaxLength(30);
            entity.Property(e => e.CreatedAtUtc);
            entity.Property(e => e.TokenDateUtc);
            entity.Property(e => e.DailyTokenNumber);

            entity.HasIndex(e => new { e.TokenDateUtc, e.DailyTokenNumber }).IsUnique();

            entity.HasMany(e => e.Items)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.MenuItemName).HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(12, 2);
        });

        modelBuilder.Entity<BusinessProfile>(entity =>
        {
            entity.Property(e => e.AboutText).HasMaxLength(1200);
            entity.Property(e => e.AddressLine1).HasMaxLength(200);
            entity.Property(e => e.AddressLine2).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(200);
        });
    }
}
