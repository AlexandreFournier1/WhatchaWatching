---
name: deploy-alwaysdata
description: Build, test and publish the web app to the AlwaysData hosting over SFTP, restart the site and check that it responds, after asking for confirmation. Use when the user wants to deploy, publish, release or update the live site.
disable-model-invocation: true
allowed-tools: Bash(git status *) Bash(git log *) Bash(git branch *) Bash(dotnet build *) Bash(dotnet test *) Bash(dotnet publish *) Bash(dotnet ef migrations list *)
---

# Deploy to AlwaysData

Publish `WhatchaWatching` (the web project) in Release mode, upload it over SFTP to the AlwaysData site, restart the site and check it answers.
This replaces the live site, so the user must see what will be sent and where before anything is uploaded.

## Settings

Non-secret settings are in `.claude/skills/deploy-alwaysdata/config.json`:

| Key | Meaning |
|---|---|
| `account` | AlwaysData account name |
| `sshHost` | SFTP/SSH host, usually `ssh-<account>.alwaysdata.net` |
| `sshUser` | SFTP user (the account name, or `<account>_<user>`) |
| `remoteDir` | Directory of the site on the server, relative to the home directory (for example `www/whatcha-watching`) |
| `siteId` | Numeric id of the site (visible in the URL of the site's page in the AlwaysData admin panel) |
| `siteUrl` | Public URL, used for the final check |

Secret, only from the environment of the user's terminal (never from files or the chat):
- `ALWAYSDATA_API_TOKEN`: API token, used to restart the site.
- SFTP login uses an SSH key (no password prompt is possible in a non-interactive run).

## Rules
- Never print, log, write or commit a token, password or connection string. Never ask the user to paste one in the chat.
- Never publish files that look like secrets (`secrets.json`, `.env`, `appsettings.*.json` holding credentials). The production connection string lives in the site's environment on AlwaysData, not in the package.
- Never delete remote files outside `remoteDir`, and never use a remote path from anywhere but `config.json`.
- The confirmation below is required every time, even if the user's arguments say "yes" or "just do it".

## Current state
- Branch and changes: !`git status --short --branch 2>&1 || true`
- Latest commits: !`git log --oneline -3 2>&1 || true`

## Steps

### 1. Check the settings
Read `config.json`. If `account`, `sshHost`, `sshUser`, `remoteDir` or `siteId` is empty, ask the user for the missing values (they are not secret), write them to `config.json`, and continue.
Check that `ALWAYSDATA_API_TOKEN` is set (test with `[ -n "$ALWAYSDATA_API_TOKEN" ]`, never print it). If it is not, tell the user to create a token in the AlwaysData admin panel (Account > API tokens) and export it in their terminal, then stop.
Check that an `sftp` client exists (`sftp -V`). If not, stop and tell the user to install the OpenSSH client.

### 2. Check the code
- Uncommitted changes: warn the user. The deployed version should be a committed one, so suggest `/commit-push` first and let them decide.
- Being on a branch other than `main`: mention it.
- Run `dotnet build -c Release` and `dotnet test -c Release`. If either fails, show the errors and stop.
- Run `dotnet ef migrations list --project WhatchaWatching.Infrastructure`. If migrations are `(Pending)` and the new code depends on them, tell the user to run `/apply-migrations` first and let them decide. If the command cannot connect, skip this check and say so.

### 3. Publish
Create a fresh empty directory in the OS temp area (for example with `mktemp -d`) and run
`dotnet publish WhatchaWatching -c Release --no-self-contained -o <that directory>`.
- Look at the output: no `secrets.json`, `.env`, `*.pdb` is fine, but no `appsettings.Development.json` with credentials.
- Report the number of files and the total size.

### 4. Confirmation
Use AskUserQuestion (or plain text if unavailable) and show:
- The commit being deployed (hash and subject), and whether the working tree is clean.
- Where it goes: `sshUser@sshHost:remoteDir`, and the site that will be restarted (`siteUrl`).
- The files and size from step 3.
- That the site is replaced in place, so a visitor may see a short interruption, and that files deleted since the last release are not removed from the server.

Options: **Deploy**, **Cancel**. Do not upload anything until the user picks Deploy.

### 5. Upload
Use key authentication only, never a password: `sftp -oBatchMode=yes -b <batch file> <sshUser>@<sshHost>`.
Write the batch file in the temp area (not in the repository) with:
```
lcd <published directory>
cd <remoteDir>
put -r *
```
- If the connection fails or asks for a password, stop and explain how to set up an SSH key for AlwaysData. Do not retry in a loop.
- If the upload stops halfway, say so: the site may be in a mixed state. Offer to run the same upload again, and do not restart the site until an upload has fully succeeded.

### 6. Restart
Restart the site through the AlwaysData API, without showing the token or the response body:
```
curl -s -o /dev/null -w "%{http_code}" -X POST --basic --user "$ALWAYSDATA_API_TOKEN account=<account>:" https://api.alwaysdata.com/v1/site/<siteId>/restart/
```
- A `2xx` code means the restart was requested. Any other code: report it and tell the user to restart the site from the admin panel (Web > Sites > restart).

### 7. Check
Wait a few seconds, then request `siteUrl` with `curl -s -o /dev/null -w "%{http_code}"`; retry up to 5 times, 5 seconds apart.
- `200`: success.
- Anything else: report the code and point to the site logs in the AlwaysData admin panel (Web > Sites > logs). Do not roll back on your own.

### 8. Clean up and report
Delete the temporary publish directory and batch file. End with a short summary: commit deployed, number of files sent, restart result and the HTTP status of the site.
