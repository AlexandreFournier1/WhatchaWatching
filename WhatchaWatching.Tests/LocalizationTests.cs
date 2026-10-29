using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using WhatchaWatching.Services.Localization;

namespace WhatchaWatching.Tests;

public partial class LocalizationTests
{
    private static T InCulture<T>(string culture, Func<T> action)
    {
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo(culture);
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void Localizer_InFrench_ReturnsTheTranslation()
    {
        Assert.Equal("Ma liste", InCulture("fr", () => new Localizer()["My List"]));
    }

    [Fact]
    public void Localizer_InEnglish_ReturnsTheKey()
    {
        Assert.Equal("My List", InCulture("en", () => new Localizer()["My List"]));
    }

    [Fact]
    public void Localizer_WithoutTranslation_FallsBackToTheEnglishText()
    {
        Assert.Equal("Not translated anywhere", InCulture("fr", () => new Localizer()["Not translated anywhere"]));
    }

    [Fact]
    public void Localizer_FillsThePlaceholders()
    {
        Assert.Equal("Bonjour alex,", InCulture("fr", () => new Localizer().F("Hello {0},", "alex")));
    }

    [Fact]
    public void Localizer_ForAnotherLanguage_IsEnglish()
    {
        Assert.Equal("en", InCulture("de", () => new Localizer().Language));
    }

    [Fact]
    public void Translations_KeepThePlaceholdersOfTheirKey()
    {
        var placeholders = PlaceholderRegex();

        foreach (var (key, translation) in FrenchTranslations.Texts)
        {
            Assert.Equal(
                placeholders.Matches(key).Select(m => m.Value).Order(),
                placeholders.Matches(translation).Select(m => m.Value).Order());
        }
    }

    [Fact]
    public void Translations_AreNeverEmpty()
    {
        Assert.All(FrenchTranslations.Texts, pair => Assert.False(string.IsNullOrWhiteSpace(pair.Value), pair.Key));
    }

    [Fact]
    public void EveryTextTheUiWrites_HasAFrenchTranslation()
    {
        var root = FindSolutionFolder();
        var missing = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "WhatchaWatching"), "*.*", SearchOption.AllDirectories)
                     .Where(f => (f.EndsWith(".razor") || f.EndsWith(".cs"))
                                 && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                                 && !f.EndsWith("FrenchTranslations.cs")))
        {
            foreach (Match match in UsedTextRegex().Matches(File.ReadAllText(file)))
            {
                var text = match.Groups["text"].Value.Replace("\\\"", "\"");
                if (!FrenchTranslations.Texts.ContainsKey(text))
                {
                    missing.Add(text);
                }
            }
        }

        Assert.True(missing.Count == 0, "Without a French translation:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void LanguageEndpoint_RemembersTheChoiceInACookieAndGoesBack()
    {
        var context = new DefaultHttpContext();

        var result = LanguageEndpoint.Choose(context, "fr", "/my-list?type=movie");

        Assert.Equal("/my-list?type=movie", Assert.IsType<RedirectHttpResult>(result).Url);
        Assert.Contains("c=fr|uic=fr", Uri.UnescapeDataString(context.Response.Headers.SetCookie.ToString()));
    }

    [Fact]
    public void LanguageEndpoint_IgnoresALanguageItDoesNotKnow()
    {
        var context = new DefaultHttpContext();

        LanguageEndpoint.Choose(context, "xx", "/");

        Assert.False(context.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Fact]
    public void LanguageEndpoint_NeverLeavesTheSite()
    {
        var result = LanguageEndpoint.Choose(new DefaultHttpContext(), "fr", "https://evil.example/");

        Assert.StartsWith("/", Assert.IsType<RedirectHttpResult>(result).Url);
    }

    private static string FindSolutionFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !folder.EnumerateFiles("*.slnx").Any())
        {
            folder = folder.Parent;
        }

        return folder?.FullName ?? throw new InvalidOperationException("The solution folder was not found.");
    }

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderRegex();

    // L["text"], L.F("text", ...) and Loc.T(l, "text") with a plain text; texts built at run time are not found.
    [GeneratedRegex(@"(?:\bL|localizer|Localizer)(?:\.F)?[\[(]""(?<text>(?:[^""\\]|\\.)*)""|Loc\.T\(\w+, *""(?<text>(?:[^""\\]|\\.)*)""")]
    private static partial Regex UsedTextRegex();
}
