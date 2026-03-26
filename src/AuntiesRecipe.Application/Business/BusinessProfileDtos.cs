namespace AuntiesRecipe.Application.Business;

public sealed record BusinessProfileDto(
    string BusinessName,
    string Tagline,
    string HeroImagePath,
    string? MapEmbedUrl,
    string AboutText,
    string AddressLine1,
    string AddressLine2,
    string Phone,
    string Email);

