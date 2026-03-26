using AuntiesRecipe.Application.Abstractions;
using AuntiesRecipe.Application.Business;
using AuntiesRecipe.Application.Repositories;
using AuntiesRecipe.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace AuntiesRecipe.Application.Services;

public sealed class BusinessProfileAppService(
    IBusinessProfileRepository profileRepo,
    IConfiguration configuration) : IBusinessProfileService
{
    public async Task<BusinessProfileDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var existing = await profileRepo.GetAsync(cancellationToken);
        if (existing is not null)
        {
            return new BusinessProfileDto(
                existing.BusinessName, existing.Tagline, existing.HeroImagePath, existing.MapEmbedUrl,
                existing.AboutText, existing.AddressLine1, existing.AddressLine2,
                existing.Phone, existing.Email);
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
        var entity = new BusinessProfile
        {
            BusinessName = profile.BusinessName.Trim(),
            Tagline = profile.Tagline.Trim(),
            HeroImagePath = profile.HeroImagePath.Trim(),
            MapEmbedUrl = string.IsNullOrWhiteSpace(profile.MapEmbedUrl) ? null : profile.MapEmbedUrl.Trim(),
            AboutText = profile.AboutText.Trim(),
            AddressLine1 = profile.AddressLine1.Trim(),
            AddressLine2 = profile.AddressLine2.Trim(),
            Phone = profile.Phone.Trim(),
            Email = profile.Email.Trim()
        };
        await profileRepo.UpsertAsync(entity, cancellationToken);
    }
}
