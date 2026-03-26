using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Business;
using AuntiesRecipe.Domain.Entities;
using AuntiesRecipe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AuntiesRecipe.Infrastructure.Services;

public sealed class BusinessProfileService(
    IDbContextFactory<AppDbContext> dbFactory,
    IConfiguration configuration) : IBusinessProfileService
{
    public async Task<BusinessProfileDto> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.BusinessProfiles.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return new BusinessProfileDto(
                existing.BusinessName,
                existing.Tagline,
                existing.HeroImagePath,
                existing.MapEmbedUrl,
                existing.AboutText,
                existing.AddressLine1,
                existing.AddressLine2,
                existing.Phone,
                existing.Email);
        }

        return new BusinessProfileDto(
            configuration["Business:Name"] ?? "Aunties Recipe",
            configuration["Business:Tagline"] ?? "Mexican-style juices & daily specials",
            configuration["Business:HeroImagePath"] ?? "/images/hero.png",
            configuration["Business:MapEmbedUrl"],
            "This demo portfolio site shows a real-world style layout: landing content, a menu, cart, checkout, and admin order viewing.",
            configuration["Business:AddressLine1"] ?? "Street address here",
            configuration["Business:AddressLine2"] ?? "Aubrey, TX 76227",
            configuration["Business:Phone"] ?? "(000) 000-0000",
            configuration["Business:Email"] ?? "hello@example.com");
    }

    public async Task UpdateAsync(BusinessProfileDto profile, CancellationToken cancellationToken = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var existing = await db.BusinessProfiles.FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            existing = new BusinessProfile();
            db.BusinessProfiles.Add(existing);
        }

        existing.BusinessName = profile.BusinessName.Trim();
        existing.Tagline = profile.Tagline.Trim();
        existing.HeroImagePath = profile.HeroImagePath.Trim();
        existing.MapEmbedUrl = string.IsNullOrWhiteSpace(profile.MapEmbedUrl) ? null : profile.MapEmbedUrl.Trim();
        existing.AboutText = profile.AboutText.Trim();
        existing.AddressLine1 = profile.AddressLine1.Trim();
        existing.AddressLine2 = profile.AddressLine2.Trim();
        existing.Phone = profile.Phone.Trim();
        existing.Email = profile.Email.Trim();

        await db.SaveChangesAsync(cancellationToken);
    }
}

