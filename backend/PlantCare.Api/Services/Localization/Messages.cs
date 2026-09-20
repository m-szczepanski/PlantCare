namespace PlantCare.Api.Services.Localization;

/// <summary>
/// Server-side message catalog for the languages the API itself speaks
/// (due messages, notification digests, hints, ICS text, error payloads).
/// Frontend UI strings live in the SPA's i18n catalogs instead. Values use
/// {0}-style placeholders; plural keys carry .one/.few/.many/.other variants
/// selected with each language's cardinal plural rules.
/// </summary>
public static class Messages
{
    public const string DefaultLanguage = "en";

    private static readonly Dictionary<string, Dictionary<string, string>> Catalog = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = new(StringComparer.Ordinal)
        {
            ["due.none"] = "No watering schedule",
            ["due.today"] = "Due today",
            ["due.tomorrow"] = "Due tomorrow",
            ["due.overdue.one"] = "{0} day overdue",
            ["due.overdue.other"] = "{0} days overdue",
            ["due.until.one"] = "{0} day until due",
            ["due.until.other"] = "{0} days until due",

            ["digest.title.one"] = "1 plant needs water",
            ["digest.title.other"] = "{0} plants need water",
            ["digest.overdue.one"] = " ({0} overdue)",
            ["digest.overdue.other"] = " ({0} overdue)",
            ["digest.toxicPets"] = " [toxic to pets]",
            ["digest.toxicChildren"] = " [toxic to children]",
            ["digest.button"] = "Water now",

            ["hint.fertilizingWinter"] = "Winter rest: hold off feeding until spring.",
            ["hint.flush"] = "Flushing the soil with plain water helps clear fertilizer salts.",
            ["hint.wateringWinter"] = "Winter: watering interval is doubled.",

            ["calendar.name"] = "PlantCare watering",
            ["calendar.summary"] = "Water {0}",

            ["quick.rateLimited"] = "Too many quick-action attempts.",
            ["quick.invalidKey"] = "Invalid quick-action key.",
            ["quick.notFound"] = "Plant not found.",
            ["quick.done"] = "{0} watered just now.",
            ["quick.note"] = "Watered via notification",

            ["error.unknownProfile.title"] = "Unknown plant profile.",
            ["error.unknownProfile.detail"] = "No plant profile with id {0} exists.",
            ["error.unknownRoom.title"] = "Unknown room.",
            ["error.unknownRoom.detail"] = "No room with id {0} exists.",
            ["error.noPhoto.title"] = "No photo supplied.",
            ["error.noPhoto.detail"] = "Send the image in the multipart form field \"file\".",
            ["error.unsupportedPhoto.title"] = "Unsupported photo.",
            ["error.photo.unsupportedType"] = "Unsupported photo type '{0}'.",
            ["error.photo.tooLarge"] = "Photo exceeds the 5 MB limit.",
            ["error.daysRange.title"] = "Days must be between 1 and 365.",
            ["error.duplicateCareTask.title"] = "This plant already has that care task.",
            ["error.unknownCareTaskType.title"] = "Unknown care task type.",
            ["error.unknownCareTaskType.detail"] = "'{0}' is not a known care task type.",
            ["error.duplicateRoom.title"] = "A room with that name already exists.",
            ["error.duplicateProfile.title"] = "Profile name already in use.",
            ["error.duplicateProfile.detail"] = "A plant profile named '{0}' already exists.",
            ["error.invalidChecklist.title"] = "Invalid diagnosis checklist.",
            ["error.invalidChecklist.detail"] = "Provide a JSON array of { symptom, causes[] } entries with non-empty values.",
            ["error.journalEmpty.title"] = "A journal entry needs a photo, a note, or both.",
            ["error.importFailed.title"] = "Import failed.",
        },
        ["pl"] = new(StringComparer.Ordinal)
        {
            ["due.none"] = "Brak harmonogramu podlewania",
            ["due.today"] = "Dziś potrzebne podlanie",
            ["due.tomorrow"] = "Podlanie jutro",
            ["due.overdue.one"] = "Zaległe o {0} dzień",
            ["due.overdue.few"] = "Zaległe o {0} dni",
            ["due.overdue.many"] = "Zaległe o {0} dni",
            ["due.until.one"] = "Podlanie za {0} dzień",
            ["due.until.few"] = "Podlanie za {0} dni",
            ["due.until.many"] = "Podlanie za {0} dni",

            ["digest.title.one"] = "1 roślina potrzebuje podlania",
            ["digest.title.few"] = "{0} rośliny potrzebują podlania",
            ["digest.title.many"] = "{0} roślin potrzebuje podlania",
            ["digest.overdue.one"] = " (zalega {0})",
            ["digest.overdue.few"] = " (zalegają {0})",
            ["digest.overdue.many"] = " (zalega {0})",
            ["digest.toxicPets"] = " [trująca dla zwierząt]",
            ["digest.toxicChildren"] = " [trująca dla dzieci]",
            ["digest.button"] = "Podlej teraz",

            ["hint.fertilizingWinter"] = "Zimowy spoczynek: wstrzymaj nawożenie do wiosny.",
            ["hint.flush"] = "Przepłukanie podłoża czystą wodą pomaga usunąć sole z nawozów.",
            ["hint.wateringWinter"] = "Zima: przedział podlewania jest wydłużony dwukrotnie.",

            ["calendar.name"] = "Podlewanie PlantCare",
            ["calendar.summary"] = "Podlej: {0}",

            ["quick.rateLimited"] = "Zbyt wiele prób szybkiego podlania.",
            ["quick.invalidKey"] = "Nieprawidłowy klucz szybkiego podlania.",
            ["quick.notFound"] = "Nie znaleziono rośliny.",
            ["quick.done"] = "{0} podlana przed chwilą.",
            ["quick.note"] = "Podlano przez powiadomienie",

            ["error.unknownProfile.title"] = "Nieznany profil rośliny.",
            ["error.unknownProfile.detail"] = "Nie istnieje profil rośliny o id {0}.",
            ["error.unknownRoom.title"] = "Nieznany pokój.",
            ["error.unknownRoom.detail"] = "Nie istnieje pokój o id {0}.",
            ["error.noPhoto.title"] = "Nie przesłano zdjęcia.",
            ["error.noPhoto.detail"] = "Wyślij obraz w polu formularza multipart \"file\".",
            ["error.unsupportedPhoto.title"] = "Nieobsługiwane zdjęcie.",
            ["error.photo.unsupportedType"] = "Nieobsługiwany typ zdjęcia „{0}”.",
            ["error.photo.tooLarge"] = "Zdjęcie przekracza limit 5 MB.",
            ["error.daysRange.title"] = "Liczba dni musi mieścić się w zakresie 1–365.",
            ["error.duplicateCareTask.title"] = "Ta roślina ma już takie zadanie pielęgnacyjne.",
            ["error.unknownCareTaskType.title"] = "Nieznany typ zadania.",
            ["error.unknownCareTaskType.detail"] = "„{0}” to nieznany typ zadania pielęgnacyjnego.",
            ["error.duplicateRoom.title"] = "Pokój o tej nazwie już istnieje.",
            ["error.duplicateProfile.title"] = "Nazwa profilu jest już zajęta.",
            ["error.duplicateProfile.detail"] = "Profil rośliny o nazwie „{0}” już istnieje.",
            ["error.invalidChecklist.title"] = "Nieprawidłowa lista diagnostyczna.",
            ["error.invalidChecklist.detail"] = "Podaj tablicę JSON z wpisami { symptom, causes[] } o niepustych wartościach.",
            ["error.journalEmpty.title"] = "Wpis dziennika wymaga zdjęcia, notatki lub obu.",
            ["error.importFailed.title"] = "Import nie powiódł się.",
        },
    };

    public static bool IsSupported(string language) => Catalog.ContainsKey(language);

    public static string Get(string language, string key)
        => Catalog.TryGetValue(language, out var messages) && messages.TryGetValue(key, out var value)
            ? value
            : Catalog[DefaultLanguage].TryGetValue(key, out var fallback)
                ? fallback
                : key;

    public static string Get(string language, string key, params object[] args)
        => string.Format(Get(language, key), args);

    /// <summary>Resolves a plural key (base + .one/.few/.many/.other) for the language's cardinal rules.</summary>
    public static string GetPlural(string language, string key, int count)
    {
        var category = PluralCategory(language, count);
        var resolved = Get(language, $"{key}.{category}");
        if (resolved == $"{key}.{category}")
        {
            resolved = Get(language, $"{key}.other");
        }
        return string.Format(resolved, count);
    }

    private static string PluralCategory(string language, int count)
    {
        if (language.Equals("pl", StringComparison.OrdinalIgnoreCase))
        {
            var abs = Math.Abs(count);
            if (abs == 1)
            {
                return "one";
            }
            var mod10 = abs % 10;
            var mod100 = abs % 100;
            if (mod10 is >= 2 and <= 4 && mod100 is < 12 or > 14)
            {
                return "few";
            }
            return "many";
        }

        return Math.Abs(count) == 1 ? "one" : "other";
    }
}
