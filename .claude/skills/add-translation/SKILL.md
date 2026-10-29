---
name: add-translation
description: Add the French translation of every interface text that has none. Use after writing or changing UI text (L["..."], Loc.T, validation messages, account emails), or when LocalizationTests fails because a text has no translation.
allowed-tools: Bash(dotnet test*) Bash(git diff*)
---

# Add missing French translations

The English text is the key; `WhatchaWatching/Services/Localization/FrenchTranslations.cs` holds the French.
A text without a translation shows in English, and `LocalizationTests` fails if a text the UI writes has none.

## Steps

1. Run `dotnet test --filter FullyQualifiedName~LocalizationTests` and read which texts are reported missing.
   If the test passes but the user named new texts, use those.
2. Open `FrenchTranslations.cs` and put each entry in the commented section it belongs to (create a section comment only if none fits).
3. Translate as a native French UI would, in the tone of the neighbouring entries:
   - Keep every `{0}`, `{1}` placeholder, in the same count (a test checks it).
   - Use "tu" or "vous" like the surrounding entries; do not mix.
   - Keep proper names (TMDB, Jikan, AniList, MyAnimeList) untouched.
   - Do not translate the TMDB attribution sentence: it stays in English on purpose.
   - Titles, synopses and genre names come from the providers: they are not translated here.
4. Run the same test again, then `dotnet test`. Report any failure with its output.

## Rules
- Never change an existing English key to fix a typo without changing it at every call site too.
- Do not add translations for texts the UI does not write.
