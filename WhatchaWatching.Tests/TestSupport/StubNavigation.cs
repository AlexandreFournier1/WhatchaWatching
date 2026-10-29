using Microsoft.AspNetCore.Components;

namespace WhatchaWatching.Tests.TestSupport;

/// <summary>A navigation manager that stands at an address, for the components that read where they are.</summary>
internal sealed class StubNavigation : NavigationManager
{
    public StubNavigation(string address = "http://localhost/") => Initialize("http://localhost/", address);

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
    }
}
