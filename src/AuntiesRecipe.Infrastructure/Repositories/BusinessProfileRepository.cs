using AuntiesRecipe.Application.Repositories;
using AuntiesRecipe.Domain.Entities;
using AuntiesRecipe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuntiesRecipe.Infrastructure.Repositories;

public sealed class BusinessProfileRepository(IDbContextFactory<AppDbContext> dbFactory) : IBusinessProfileRepository
{
    public async Task<BusinessProfile?> GetAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        return await db.BusinessProfiles.AsNoTracking().FirstOrDefaultAsync(ct);
    }

    public async Task UpsertAsync(BusinessProfile profile, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existing = await db.BusinessProfiles.FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            db.BusinessProfiles.Add(profile);
        }
        else
        {
            existing.BusinessName = profile.BusinessName;
            existing.Tagline = profile.Tagline;
            existing.HeroImagePath = profile.HeroImagePath;
            existing.MapEmbedUrl = profile.MapEmbedUrl;
            existing.AboutText = profile.AboutText;
            existing.AddressLine1 = profile.AddressLine1;
            existing.AddressLine2 = profile.AddressLine2;
            existing.Phone = profile.Phone;
            existing.Email = profile.Email;
        }
        await db.SaveChangesAsync(ct);
    }
}
