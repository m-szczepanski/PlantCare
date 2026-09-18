using Microsoft.EntityFrameworkCore;
using PlantCare.Api.Data;
using PlantCare.Api.Dtos;
using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

public enum PlantProfileWriteStatus
{
    Success,
    NotFound,
    DuplicateName,
}

public sealed record PlantProfileWriteResult(PlantProfileWriteStatus Status, PlantProfileResponseDto? Profile = null);

public interface IPlantProfileService
{
    Task<IReadOnlyList<PlantProfileResponseDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<PlantProfileWriteResult> CreateAsync(PlantProfileRequestDto dto, CancellationToken cancellationToken = default);

    Task<PlantProfileWriteResult> UpdateAsync(int id, PlantProfileRequestDto dto, CancellationToken cancellationToken = default);
}

public sealed class PlantProfileService(AppDbContext db) : IPlantProfileService
{
    public async Task<IReadOnlyList<PlantProfileResponseDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await db.PlantProfiles
            .OrderBy(p => p.CommonName)
            .ToListAsync(cancellationToken);

        return profiles
            .Select(ToResponse)
            .ToList();
    }

    public async Task<PlantProfileWriteResult> CreateAsync(PlantProfileRequestDto dto, CancellationToken cancellationToken = default)
    {
        var commonName = dto.CommonName.Trim();
        if (await NameTakenAsync(commonName, null, cancellationToken))
        {
            return new PlantProfileWriteResult(PlantProfileWriteStatus.DuplicateName);
        }

        var profile = new PlantProfile
        {
            CommonName = commonName,
            ScientificName = dto.ScientificName?.Trim(),
            DefaultWateringIntervalDays = dto.DefaultWateringIntervalDays,
            LightRequirement = dto.LightRequirement,
            HumidityNotes = dto.HumidityNotes.Trim(),
            CareTips = dto.CareTips.Trim(),
        };

        db.PlantProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        return new PlantProfileWriteResult(PlantProfileWriteStatus.Success, ToResponse(profile));
    }

    public async Task<PlantProfileWriteResult> UpdateAsync(int id, PlantProfileRequestDto dto, CancellationToken cancellationToken = default)
    {
        var profile = await db.PlantProfiles.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (profile is null)
        {
            return new PlantProfileWriteResult(PlantProfileWriteStatus.NotFound);
        }

        var commonName = dto.CommonName.Trim();
        if (await NameTakenAsync(commonName, id, cancellationToken))
        {
            return new PlantProfileWriteResult(PlantProfileWriteStatus.DuplicateName);
        }

        profile.CommonName = commonName;
        profile.ScientificName = dto.ScientificName?.Trim();
        profile.DefaultWateringIntervalDays = dto.DefaultWateringIntervalDays;
        profile.LightRequirement = dto.LightRequirement;
        profile.HumidityNotes = dto.HumidityNotes.Trim();
        profile.CareTips = dto.CareTips.Trim();

        await db.SaveChangesAsync(cancellationToken);

        return new PlantProfileWriteResult(PlantProfileWriteStatus.Success, ToResponse(profile));
    }

    private Task<bool> NameTakenAsync(string commonName, int? excludeId, CancellationToken cancellationToken)
        => excludeId is null
            ? db.PlantProfiles.AnyAsync(p => p.CommonName == commonName, cancellationToken)
            : db.PlantProfiles.AnyAsync(p => p.CommonName == commonName && p.Id != excludeId, cancellationToken);

    private static PlantProfileResponseDto ToResponse(PlantProfile profile) => new()
    {
        Id = profile.Id,
        CommonName = profile.CommonName,
        ScientificName = profile.ScientificName,
        DefaultWateringIntervalDays = profile.DefaultWateringIntervalDays,
    };
}
