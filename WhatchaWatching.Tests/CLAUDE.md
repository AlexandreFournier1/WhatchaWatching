# Tests (`WhatchaWatching.Tests`)

Repo-wide rules are in the root [CLAUDE.md](../CLAUDE.md).

- xUnit; `Xunit` is a global using. Follow the style of `DependencyInjectionTests.cs`: one behavior per `[Fact]`, names like `Method_Condition_Expected`.
- Test Application services and mappers. Mock HTTP with a fake `HttpMessageHandler`; do not call real Jikan/TMDB endpoints.
- `LocalizationTests` fails if a text the UI writes has no French translation, or if a translation drops a `{0}` placeholder: use `/add-translation`.
- Run `dotnet test` after any code change and before every commit. Report failures with their output; never claim success without running them.
