using PlantCare.Api.Models;

namespace PlantCare.Api.Services;

/// <summary>
/// Field-by-field resolution of profile content: the translation matching the
/// requested language wins; missing rows or fields fall back to the canonical
/// (English) values on the profile row.
/// </summary>
public static class ProfileTranslations
{
    public static PlantProfileTranslation? Pick(PlantProfile? profile, string language)
        => profile?.Translations.FirstOrDefault(t =>
            string.Equals(t.Language, language, StringComparison.OrdinalIgnoreCase));

    public static string? LocalizedCommonName(PlantProfile? profile, PlantProfileTranslation? translation)
        => translation?.CommonName ?? profile?.CommonName;

    public static string LocalizedHumidityNotes(PlantProfile profile, PlantProfileTranslation? translation)
        => translation?.HumidityNotes ?? profile.HumidityNotes;

    public static string LocalizedCareTips(PlantProfile profile, PlantProfileTranslation? translation)
        => translation?.CareTips ?? profile.CareTips;

    public static string? LocalizedDiagnosisChecklist(PlantProfile profile, PlantProfileTranslation? translation)
        => translation?.DiagnosisChecklist ?? profile.DiagnosisChecklist;
}
