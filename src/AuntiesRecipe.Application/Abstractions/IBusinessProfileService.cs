using AuntiesRecipe.Application.Business;

namespace AuntiesRecipe.Application.Abstractions;

public interface IBusinessProfileService
{
    Task<BusinessProfileDto> GetAsync(CancellationToken cancellationToken = default);
    Task UpdateAsync(BusinessProfileDto profile, CancellationToken cancellationToken = default);
}

