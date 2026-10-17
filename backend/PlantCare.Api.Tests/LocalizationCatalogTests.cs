using PlantCare.Api.Services.Localization;
using Xunit;

namespace PlantCare.Api.Tests;

/// <summary>
/// The server catalogs are hand-maintained dictionaries; nothing else stops a message
/// from being added to one language and forgotten in another. Plural variants differ per
/// language (en: one/other, pl: one/few/many), so parity is checked on the base key.
/// </summary>
public class LocalizationCatalogTests
{
    private static readonly string[] PluralSuffixes = [".one", ".few", ".many", ".other"];

    [Fact]
    public void Every_language_defines_the_same_message_keys()
    {
        var reference = BaseKeys(Messages.DefaultLanguage);

        foreach (var language in Messages.Languages)
        {
            Assert.Equal(reference, BaseKeys(language));
        }
    }

    private static string[] BaseKeys(string language) =>
        Messages.Keys(language).Select(StripPlural).Distinct().Order().ToArray();

    private static string StripPlural(string key) =>
        PluralSuffixes.FirstOrDefault(suffix => key.EndsWith(suffix, StringComparison.Ordinal)) is { } match
            ? key[..^match.Length]
            : key;
}
