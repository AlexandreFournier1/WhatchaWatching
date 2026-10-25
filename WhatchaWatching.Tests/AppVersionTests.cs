using WhatchaWatching.Services;

namespace WhatchaWatching.Tests;

public class AppVersionTests
{
    [Theory]
    [InlineData("0.1.0+318545d1f0a2b3c4d5e6f708192a3b4c5d6e7f80", "v0.1.0 (318545d)")]
    [InlineData("1.2.3-beta.1+abc", "v1.2.3-beta.1 (abc)")]
    [InlineData("0.1.0", "v0.1.0")]
    [InlineData("0.1.0+", "v0.1.0")]
    public void Format_ShowsTheVersionAndTheShortCommit(string informationalVersion, string expected)
    {
        Assert.Equal(expected, AppVersion.Format(informationalVersion));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Format_WithoutAVersion_ShowsNothing(string? informationalVersion)
    {
        Assert.Equal(string.Empty, AppVersion.Format(informationalVersion));
    }

    [Fact]
    public void Display_ForTheRunningSite_StartsWithTheProjectVersion()
    {
        Assert.StartsWith("v0.1.0", AppVersion.Display);
    }
}
