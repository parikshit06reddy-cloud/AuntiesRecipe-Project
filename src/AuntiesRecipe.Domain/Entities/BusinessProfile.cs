namespace AuntiesRecipe.Domain.Entities;

public sealed class BusinessProfile
{
    public int Id { get; set; }

    public string AboutText { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

