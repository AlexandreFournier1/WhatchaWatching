namespace WhatchaWatching.Services;

/// <summary>
/// The position of the "adult content" switch next to the search and the random pick, kept for the visit (one per
/// browser session). It can only narrow what the profile allows: a user who did not turn adult content on in the account
/// page never sees the switch, and the services ignore it for them.
/// </summary>
public sealed class AdultFilterState
{
    public bool Enabled { get; set; } = true;
}
