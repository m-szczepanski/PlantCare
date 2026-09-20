using PlantCare.Api.Services.Localization;

namespace PlantCare.Api.Services;

/// <summary>
/// Language-aware access to the server-side message catalog
/// (<see cref="Messages"/>). Scoped services resolve the current language
/// from the request's Accept-Language header; with no request (background
/// jobs) the configured default language is used.
/// </summary>
public interface IAppLocalizer
{
    string Language { get; }

    string T(string key);

    string Tf(string key, params object[] args);

    string Tp(string key, int count);
}

public sealed class AppLocalizer(string language) : IAppLocalizer
{
    public string Language { get; } = Messages.IsSupported(language) ? language : Messages.DefaultLanguage;

    public string T(string key) => Messages.Get(Language, key);

    public string Tf(string key, params object[] args) => Messages.Get(Language, key, args);

    public string Tp(string key, int count) => Messages.GetPlural(Language, key, count);
}

public sealed record AppLanguageOptions(string DefaultLanguage);

public sealed class RequestAppLocalizer(IHttpContextAccessor httpContext, AppLanguageOptions options) : IAppLocalizer
{
    private readonly AppLocalizer inner = new(ResolveLanguage(httpContext.HttpContext, options));

    public string Language => inner.Language;

    public string T(string key) => inner.T(key);

    public string Tf(string key, params object[] args) => inner.Tf(key, args);

    public string Tp(string key, int count) => inner.Tp(key, count);

    private static string ResolveLanguage(HttpContext? http, AppLanguageOptions options)
    {
        if (http is null)
        {
            return options.DefaultLanguage;
        }

        var header = http.Request.Headers["Accept-Language"].ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return options.DefaultLanguage;
        }

        // Honor the first (highest-preference) tag; the SPA sends an explicit
        // single-value header, browsers send e.g. "pl,en-US;q=0.9".
        var primary = header.Split(',')[0].Split(';')[0].Trim();
        return primary.StartsWith("pl", StringComparison.OrdinalIgnoreCase) ? "pl" : Messages.DefaultLanguage;
    }
}
