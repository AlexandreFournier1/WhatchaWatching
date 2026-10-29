using System.Globalization;
using System.Runtime.CompilerServices;

namespace WhatchaWatching.Tests.TestSupport;

/// <summary>The tests read English texts and invariant-looking numbers, whatever the language of the machine running them.</summary>
internal static class TestCulture
{
    [ModuleInitializer]
    internal static void UseEnglish()
    {
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
    }
}
