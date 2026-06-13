using System.Collections.Concurrent;
using Jellyfin.Plugin.VisualHome.Models;

namespace Jellyfin.Plugin.VisualHome.Services;

/// <summary>
/// Small in-memory cache for section payloads.
/// </summary>
public sealed class SectionCacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets an existing entry or creates one.
    /// </summary>
    /// <param name="key">Cache key.</param>
    /// <param name="cacheMinutes">Cache lifetime in minutes.</param>
    /// <param name="factory">Factory for uncached values.</param>
    /// <returns>Cached or fresh section.</returns>
    public VisualSectionResult GetOrCreate(string key, int cacheMinutes, Func<VisualSectionResult> factory)
    {
        if (cacheMinutes > 0
            && _cache.TryGetValue(key, out var entry)
            && entry.ExpiresAtUtc > DateTimeOffset.UtcNow)
        {
            return entry.Result;
        }

        var result = factory();
        if (cacheMinutes > 0)
        {
            _cache[key] = new CacheEntry(result, DateTimeOffset.UtcNow.AddMinutes(cacheMinutes));
        }

        return result;
    }

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    private sealed record CacheEntry(VisualSectionResult Result, DateTimeOffset ExpiresAtUtc);
}
