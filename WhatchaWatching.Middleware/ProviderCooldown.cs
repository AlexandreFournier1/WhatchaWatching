using System.Collections.Concurrent;

namespace WhatchaWatching.Middleware;

/// <summary>
/// Remembers which providers failed recently, so the next requests go straight to the next provider
/// instead of waiting again for one that is down. Shared by every request.
/// </summary>
public sealed class ProviderCooldown(TimeProvider timeProvider)
{
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(60);

    private readonly ConcurrentDictionary<string, DateTimeOffset> _until = new();

    public bool IsCoolingDown(string provider) =>
        _until.TryGetValue(provider, out var until) && timeProvider.GetUtcNow() < until;

    public void Trip(string provider) => _until[provider] = timeProvider.GetUtcNow() + Duration;

    public void Clear(string provider) => _until.TryRemove(provider, out _);
}
