using System.Security.Cryptography;
using System.Text;

namespace PlantCare.Api.Services;

/// <summary>In-process sliding-window limiter for quick-action hits (single-user app, one API instance).</summary>
public sealed class QuickActionRateLimiter
{
    private const int MaxAttemptsPerWindow = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    private readonly object _gate = new();
    private readonly Dictionary<string, List<DateTime>> _attempts = [];

    public bool TryAllow(string key)
    {
        var now = DateTime.UtcNow;
        lock (_gate)
        {
            if (!_attempts.TryGetValue(key, out var hits))
            {
                hits = [];
                _attempts[key] = hits;
            }

            hits.RemoveAll(h => now - h > Window);
            if (hits.Count >= MaxAttemptsPerWindow)
            {
                return false;
            }

            hits.Add(now);
            return true;
        }
    }

    public static bool SecretMatches(string? provided, string expected)
    {
        if (provided is null)
        {
            return false;
        }

        var a = Encoding.UTF8.GetBytes(provided);
        var b = Encoding.UTF8.GetBytes(expected);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}
