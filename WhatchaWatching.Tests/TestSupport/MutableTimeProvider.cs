namespace WhatchaWatching.Tests.TestSupport;

public sealed class MutableTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public void Advance(TimeSpan duration) => _now += duration;

    public override DateTimeOffset GetUtcNow() => _now;
}
