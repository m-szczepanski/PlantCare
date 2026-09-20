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
    InvalidChecklist,
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
            .Include(p => p.Plants)
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

        if (!DiagnosisChecklist.TryValidate(dto.DiagnosisChecklist, out _, out _))
        {
            return new PlantProfileWriteResult(PlantProfileWriteStatus.InvalidChecklist);
        }

        var profile = new PlantProfile
        {
            CommonName = commonName,
            ScientificName = dto.ScientificName?.Trim(),
            DefaultWateringIntervalDays = dto.DefaultWateringIntervalDays,
            LightRequirement = dto.LightRequirement,
            HumidityNotes = dto.HumidityNotes.Trim(),
            CareTips = dto.CareTips.Trim(),
            ToxicToPets = dto.ToxicToPets,
            ToxicToChildren = dto.ToxicToChildren,
            DiagnosisChecklist = dto.DiagnosisChecklist?.Trim(),
        };

        db.PlantProfiles.Add(profile);
        await db.SaveChangesAsync(cancellationToken);

        return new PlantProfileWriteResult(PlantProfileWriteStatus.Success, await ReloadAsync(profile.Id, cancellationToken));
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

        if (!DiagnosisChecklist.TryValidate(dto.DiagnosisChecklist, out _, out _))
        {
            return new PlantProfileWriteResult(PlantProfileWriteStatus.InvalidChecklist);
        }

        profile.CommonName = commonName;
        profile.ScientificName = dto.ScientificName?.Trim();
        profile.DefaultWateringIntervalDays = dto.DefaultWateringIntervalDays;
        profile.LightRequirement = dto.LightRequirement;
        profile.HumidityNotes = dto.HumidityNotes.Trim();
        profile.CareTips = dto.CareTips.Trim();

        profile.LightRequirement = dto.LightRequirement;
            profile.HumidityNotes = dto.HumidityNotes.Trim();
            profile.CareTips = dto.CareTips.Trim();
            profile.ToxicToPets = dto.ToxicToPets;
            profile.ToxicToChildren = dto.ToxicToChildren;
            profile.DiagnosisChecklist = dto.DiagnosisChecklist?.Trim();
            profile.DefaultWateringIntervalDays = dto.DefaultWateringIntervalDays;
            profile.ScientificName = dto.ScientificName?.Trim();

            await db.SaveChangesAsync(cancellationToken);

        return new PlantProfileWriteResult(PlantProfileWriteStatus.Success, await ReloadAsync(profile.Id, cancellationToken));
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
        LightRequirement = profile.LightRequirement,
        HumidityNotes = profile.HumidityNotes,
        CareTips = profile.CareTips,
        ToxicToPets = profile.ToxicToPets,
        ToxicToChildren = profile.ToxicToChildren,
        DiagnosisChecklist = profile.DiagnosisChecklist,
        PlantCount = profile.Plants?.Count ?? 0,
    };

    private async Task<PlantProfileResponseDto> ReloadAsync(int id, CancellationToken cancellationToken)
    {
        var profile = await db.PlantProfiles
            .Include(p => p.Plants)
            .AsNoTracking()
            .FirstAsync(p => p.Id == id, cancellationToken);

        return ToResponse(profile);
    }
}
