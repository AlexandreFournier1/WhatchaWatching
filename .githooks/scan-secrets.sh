#!/bin/sh
# Shared by the git hooks and the Claude Code hook.
# Usage: scan-secrets.sh staged | scan-secrets.sh range <git rev range> | scan-secrets.sh unpushed

if ! command -v gitleaks >/dev/null 2>&1; then
  echo "gitleaks is not installed: install it (winget install Gitleaks.Gitleaks) and reopen the terminal." >&2
  exit 1
fi

root=$(git rev-parse --show-toplevel) || exit 1
config="$root/.gitleaks.toml"

case "$1" in
  staged)
    # `gitleaks git --staged` runs `git diff --staged .`, which fails on this OneDrive folder
    # ("failed to stat '.'"), so scan the staged content of the changed files instead.
    tmp=$(mktemp -d) || exit 1
    trap 'rm -rf "$tmp"' EXIT
    git diff --cached --name-only --diff-filter=ACMR -z | xargs -0 -r git checkout-index --prefix="$tmp/" -- || exit 1
    gitleaks dir "$tmp" --redact --no-banner --config "$config"
    ;;
  range)
    gitleaks git --log-opts="$2" --redact --no-banner --config "$config" "$root"
    ;;
  unpushed)
    gitleaks git --log-opts="HEAD --not --remotes" --redact --no-banner --config "$config" "$root"
    ;;
  *)
    echo "usage: scan-secrets.sh staged | range <rev-range> | unpushed" >&2
    exit 2
    ;;
esac
