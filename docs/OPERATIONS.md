# Operations

How the live site (AlwaysData) is looked after: settings to make in the AlwaysData panel, the email setup, the backups, and what to check after a deploy. Nothing here is automated; the steps that touch the database or the site are the owner's.

## Settings in the AlwaysData panel

- **Redirect http to https.** The site answers on both. In the settings of the site, turn on the redirection to HTTPS. The app does not redirect by itself: it only knows it is served over https thanks to the headers of the proxy (`X-Forwarded-Proto`), and a redirect in the app would loop if they did not arrive.
- **Environment variables of the site** (Web > Sites > the site > environment, one `NAME=value` per line):
  - `ConnectionStrings__DefaultConnection`: the database. Add `SslMode=Required` to the string. MySqlConnector (before 2.6.1) could send the password before checking the certificate when the mode was left at its default; the patched version is pinned, and `Required` makes sure the connection is encrypted.
  - `Tmdb__AccessToken`: the TMDB read access token. (The country of the streaming platforms is each user's own setting.)
  - `Email__Host`, `Email__Port` (587), `Email__UserName`, `Email__Password`, `Email__From` (and optionally `Email__FromName`): the mailbox the site sends from, see below.

## Email (confirmation and password reset)

The site sends two kinds of message: the link that confirms an address and the link that resets a password. It needs a mailbox to send from:

1. Create a mailbox in AlwaysData (Email > Mailboxes), for example `no-reply@<your domain>`, or use an SMTP service.
2. Set the `Email__*` variables above. The host is the SMTP server of the mailbox (for AlwaysData, `smtp-<account>.alwaysdata.net`), the port 587 with STARTTLS.
3. Restart the site and try **Forgot your password** with your own address.

Without these settings the site works as before, but no message leaves: the password reset page answers as if it had sent one (it does not tell who has an account) and the warning "The password reset email could not be sent" is written to the log. The links work for two hours.

## Backups

A migration or a bug can damage the data, and `/apply-migrations` cannot undo what it did.

- **Before each migration**: export the database. In the panel, Databases > MySQL > the database > export, or phpMyAdmin > Export. Keep the file somewhere other than the site.
- **From a terminal**, with the connection details of the panel (the password is asked, not typed in the command):

  ```bash
  mysqldump --single-transaction --no-tablespaces -h mysql-<account>.alwaysdata.net -u <user> -p <database> > backup-$(date +%F).sql
  ```

- **Try a restore once** into a local database, and keep it as the check that backups are worth something:

  ```bash
  mysql -u root -p -e "CREATE DATABASE restore_test CHARACTER SET utf8mb4"
  mysql -u root -p restore_test < backup-YYYY-MM-DD.sql
  mysql -u root -p restore_test -e "SELECT COUNT(*) FROM Users; SELECT COUNT(*) FROM WatchListItems; SELECT COUNT(*) FROM Media; SELECT MigrationId FROM __EFMigrationsHistory ORDER BY 1 DESC LIMIT 1;"
  ```

  The counts must be those of the live database and the last migration the latest applied one. Delete the test database afterwards. AlwaysData also keeps its own backups: check in the panel how long and how often, and do not rely on them alone.

## Secret scan before commit and push

A secret pushed to GitHub must be treated as leaked, even if the commit is deleted afterwards. Three checks run before it can happen, all through [gitleaks](https://github.com/gitleaks/gitleaks) with the rules of `.gitleaks.toml` (the default rules, plus connection string passwords, the TMDB token and the mailbox password):

- `.githooks/pre-commit` scans the staged files, and refuses `appsettings.Production*.json`, `.env*` and `secrets.json` outright.
- `.githooks/pre-push` scans the commits about to be pushed (a new branch is scanned against everything already on a remote).
- The Claude Code hook `.claude/hooks/check-secrets.sh` (declared in `.claude/settings.json`) runs the same scan before Claude issues a `git commit` or `git push`. It also reacts to a command that only mentions these words.

On a new clone:

1. `winget install Gitleaks.Gitleaks`, then reopen the terminal (and the Claude app) so `gitleaks` is on the `PATH`. Without it, every commit and push is refused with a message saying so.
2. `git config core.hooksPath .githooks` (git does not version this setting).

If a hook blocks:

- A real secret: remove it from the files. If it is already in a commit that was not pushed, rewrite that commit; if it was pushed, change the secret first (new password, new token), then clean the history.
- A false positive: tighten the rule or add the exact path or pattern to the `[[allowlists]]` in `.gitleaks.toml`, and say why in its description. `--no-verify` is not an answer.
- To scan the whole history: `gitleaks git --redact --config .gitleaks.toml .` (`--redact` keeps the secret out of the output).

`git diff --staged .` fails in the OneDrive folder of this project ("failed to stat '.'"), and `gitleaks git --staged` runs exactly that, so `.githooks/scan-secrets.sh` scans the staged content through a temporary copy instead.

## Continuous integration

`.github/workflows/ci.yml` runs on every push to `main` and every pull request: restore, build and test in Release, a check that no package has a known vulnerability (`dotnet list package --vulnerable --include-transitive`), and a gitleaks scan of the whole history with `.gitleaks.toml`. It needs no secret and no database (the tests mock HTTP). It does not deploy: deploying and applying migrations stay manual. In the GitHub settings, protect `main` and require the `build-test` and `secrets` checks to make them binding.

## After a deploy

- `curl -sI https://<site>/login` shows `strict-transport-security`, `content-security-policy`, `x-content-type-options`, `x-frame-options`, `referrer-policy` and `permissions-policy`, and the antiforgery `set-cookie` has `secure`.
- `curl -sI http://<site>/login` answers with a redirect to https once the panel setting above is on.
- Open the site with the console of the browser open: no line "Refused to ..." (the content security policy is enforced).
- In the log of the site, no warning "Requests come from the private address ...": it would mean `X-Forwarded-For` does not arrive, and the rate limits then count every visitor together.

## What is limited, and how

- A visitor from one address can try the login, sign-up, reset-password and account forms 10 times in 5 minutes, and make 600 other requests a minute (files such as scripts and images are not counted). Over that, the answer is `429` with a `Retry-After`.
- A wrong password locks an account for 15 minutes after 5 attempts.
- The searches made from the page itself (through the Blazor connection) are not requests the limiter sees: they are bounded by what the providers allow. Caching them is on the roadmap.
