using WhatchaWatching.Services;

namespace WhatchaWatching.Tests;

public class LocalUrlTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/my-list")]
    [InlineData("/search?q=frieren&page=2")]
    public void Normalize_KeepsAPathOfThisSite(string url)
    {
        Assert.Equal(url, LocalUrl.Normalize(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("my-list")]
    [InlineData("https://evil.example/")]
    [InlineData("//evil.example/")]
    [InlineData("/\\evil.example/")]
    [InlineData("javascript:alert(1)")]
    public void Normalize_SendsAnythingElseHome(string? url)
    {
        Assert.Equal("/", LocalUrl.Normalize(url));
    }
}
