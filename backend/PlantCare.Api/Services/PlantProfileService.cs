using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;

namespace PlantCare.Api.Services;

public interface IPlantProfileService
{
    Task<IReadOnlyList<PlantProfileResponseDto>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed class PlantProfileService(AppDbContext db) : IPlantProfileService
{
    public async Task<IReadOnlyList<PlantProfileResponseDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await db.PlantProfiles
            .OrderBy(p => p.CommonName)
            .ToListAsync(cancellationToken);

        return profiles
            .Select(p => new PlantProfileResponseDto
            {
                Id = p.Id,
                CommonName = p.CommonName,
                ScientificName = p.ScientificName,
                DefaultWateringIntervalDays = p.DefaultWateringIntervalDays,
            })
            .ToList();
    }
}
