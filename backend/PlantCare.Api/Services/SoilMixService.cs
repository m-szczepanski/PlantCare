using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;

namespace PlantCare.Api.Services;

public interface ISoilMixService
{
    Task<IReadOnlyList<SoilMixOptionDto>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed class SoilMixService(AppDbContext db) : ISoilMixService
{
    public async Task<IReadOnlyList<SoilMixOptionDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var mixes = await db.SoilMixes
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return mixes
            .Select(m => new SoilMixOptionDto { Id = m.Id, Name = m.Name })
            .ToList();
    }
}
