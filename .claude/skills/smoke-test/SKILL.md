---
name: smoke-test
description: Start WhatchaWatching locally and walk through its main user journeys in the built-in browser (home, search, title page, sign-up and sign-in, watchlist, random pick, language switch) to confirm a UI or service change works in the real app. Use after changing pages, components, services or accounts, or when the user asks to smoke-test the site.
disable-model-invocation: true
---

# Smoke test

Drives the real app in the built-in browser. Tests only on the local development site (`http://localhost:5209`, or the port Aspire gives); never on production.

## Rules
- Create the test account on the local site only, with a made-up address like `smoke-<timestamp>@example.test` and a generated password. Do not repeat the password in your replies.
- Without TMDB or email settings the site still starts: movie/TV calls say TMDB is unavailable and no email is sent. Report that as a limit, not a failure.
- Do not use real provider keys, and do not touch a production database.
- Read pages with `get_page_text` / `read_page` first; use screenshots only to judge layout.

## Steps

### 1. Start the app
- Preferred: Aspire (`dotnet run --project WhatchaWatching.AppHost --launch-profile http`, needs Docker for the MySQL container), started with `preview_start` or in the background. Dashboard at http://localhost:15209.
- Alone: `dotnet run --project WhatchaWatching --launch-profile http` on http://localhost:5209, which needs a connection string.
- If it does not start (Docker off, no connection string, port taken), stop and tell the user what is missing.
- Wait until `/` answers, and check the console and server logs for errors.

### 2. Journeys
Run each one and note pass or fail with what you saw. Stop a journey at its first failure and move on.

1. **Home** (`/`): loads without console errors; the type switch (Anime / Movies / TV series) changes `?type=`; the random pick area is shown.
2. **Search** (`/search`): search for a common title (for example "naruto"); results show; paging works; the adult switch is off by default. If the providers are unreachable, the page says results come from the local database.
3. **Title page** (`/anime/{id}` from a result): synopsis, genres and streaming platforms show; no raw errors.
4. **Language**: the footer link to French switches the interface (`/language?culture=fr`), the same page is shown in French, and English comes back.
5. **Accounts**: `/register` with the test account, then `/login`; the header shows the account; `/my-list` works only when signed in (signed out it redirects to `/login?returnUrl=...`).
6. **Watchlist**: add a title from its page, see it in `/my-list`, remove it.
7. **Random pick**: with at least two titles in the list, pick one with and without a filter; a filter that matches nothing shows a clear message.
8. **Account page** (`/account`): shows the country and adult-content settings; saving a change works. Do not delete the account unless the database is a throwaway local one.
9. **Not found**: `/does-not-exist` shows the NotFound page, not a stack trace.

### 3. Narrow screen
Resize to the mobile preset, check Home, Search and the navbar menu (`<details>` toggle) are usable, then restore with the desktop preset.

### 4. Report
A table of journeys: pass, fail or not run (with the reason). For each failure give the page, what happened, the console or server log line if any, and the likely file. List what could not be checked (TMDB, emails). Stop the app you started.
