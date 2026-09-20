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

public sealed class PlantProfileService(AppDbContext db, IAppLocalizer localizer) : IPlantProfileService
{
    public async Task<IReadOnlyList<PlantProfileResponseDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await db.PlantProfiles
            .Include(p => p.Plants)
            .Include(p => p.Translations)
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
        var profile = await db.PlantProfiles
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (profile is null)
        {
            return new PlantProfileWriteResult(PlantProfileWriteStatus.NotFound);
        }

        if (!DiagnosisChecklist.TryValidate(dto.DiagnosisChecklist, out _, out _))
        {
            return new PlantProfileWriteResult(PlantProfileWriteStatus.InvalidChecklist);
        }

        if (IsCanonicalLanguage)
        {
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
            profile.ToxicToPets = dto.ToxicToPets;
            profile.ToxicToChildren = dto.ToxicToChildren;
            profile.DiagnosisChecklist = dto.DiagnosisChecklist?.Trim();
        }
        else
        {
            // Non-default language edits update (or lazily create) that language's
            // translation row; structural fields stay owned by the canonical row.
            var translation = profile.Translations.FirstOrDefault(t =>
                string.Equals(t.Language, localizer.Language, StringComparison.OrdinalIgnoreCase));
            if (translation is null)
            {
                translation = new PlantProfileTranslation
                {
                    PlantProfileId = profile.Id,
                    Language = localizer.Language,
                    CommonName = profile.CommonName,
                    HumidityNotes = profile.HumidityNotes,
                    CareTips = profile.CareTips,
                    DiagnosisChecklist = profile.DiagnosisChecklist,
                };
                db.PlantProfileTranslations.Add(translation);
            }

            translation.CommonName = dto.CommonName.Trim();
            translation.HumidityNotes = dto.HumidityNotes.Trim();
            translation.CareTips = dto.CareTips.Trim();
            translation.DiagnosisChecklist = dto.DiagnosisChecklist?.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);

        return new PlantProfileWriteResult(PlantProfileWriteStatus.Success, await ReloadAsync(profile.Id, cancellationToken));
    }

    private bool IsCanonicalLanguage =>
        string.Equals(localizer.Language, Localization.Messages.DefaultLanguage, StringComparison.OrdinalIgnoreCase);

    private Task<bool> NameTakenAsync(string commonName, int? excludeId, CancellationToken cancellationToken)
        => excludeId is null
            ? db.PlantProfiles.AnyAsync(p => p.CommonName == commonName, cancellationToken)
            : db.PlantProfiles.AnyAsync(p => p.CommonName == commonName && p.Id != excludeId, cancellationToken);

    private PlantProfileResponseDto ToResponse(PlantProfile profile)
    {
        var translation = ProfileTranslations.Pick(profile, localizer.Language);
        return new PlantProfileResponseDto
        {
            Id = profile.Id,
            CommonName = ProfileTranslations.LocalizedCommonName(profile, translation) ?? profile.CommonName,
            ScientificName = profile.ScientificName,
            DefaultWateringIntervalDays = profile.DefaultWateringIntervalDays,
            LightRequirement = profile.LightRequirement,
            HumidityNotes = ProfileTranslations.LocalizedHumidityNotes(profile, translation),
            CareTips = ProfileTranslations.LocalizedCareTips(profile, translation),
            ToxicToPets = profile.ToxicToPets,
            ToxicToChildren = profile.ToxicToChildren,
            DiagnosisChecklist = ProfileTranslations.LocalizedDiagnosisChecklist(profile, translation),
            PlantCount = profile.Plants?.Count ?? 0,
        };
    }

    private async Task<PlantProfileResponseDto> ReloadAsync(int id, CancellationToken cancellationToken)
    {
        var profile = await db.PlantProfiles
            .Include(p => p.Plants)
            .Include(p => p.Translations)
            .AsNoTracking()
            .FirstAsync(p => p.Id == id, cancellationToken);

        return ToResponse(profile);
    }
}
