using AuntiesRecipe.Domain.Entities;

namespace AuntiesRecipe.Application.Repositories;

public interface IBusinessProfileRepository
{
    Task<BusinessProfile?> GetAsync(CancellationToken ct = default);
    Task UpsertAsync(BusinessProfile profile, CancellationToken ct = default);
}
