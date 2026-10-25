#!/usr/bin/env bash
# Raises the <Version> of the web project by one level and prints "old -> new".
#
# Usage: bump-version.sh <major|minor|patch> [path to the .csproj] [--dry-run]
#
# With --dry-run it only prints what it would do and leaves the file alone.
# It only touches the <Version>x.y.z</Version> line, and stops with an error (changing nothing) when that line is
# missing or is not a plain x.y.z number.
set -euo pipefail

dry_run=false
args=()
for arg in "$@"; do
  if [ "$arg" = "--dry-run" ]; then
    dry_run=true
  else
    args+=("$arg")
  fi
done

level="${args[0]:-}"
project="${args[1]:-WhatchaWatching/WhatchaWatching.csproj}"

case "$level" in
  major | minor | patch) ;;
  *)
    echo "Usage: bump-version.sh <major|minor|patch> [csproj] [--dry-run]" >&2
    exit 2
    ;;
esac

[ -f "$project" ] || { echo "Project file not found: $project" >&2; exit 1; }

# "|| true": grep finding nothing must lead to the clear message below, not to a silent exit.
current=$(grep -o '<Version>[^<]*</Version>' "$project" | head -n 1 | sed -e 's/<Version>//' -e 's/<\/Version>//' || true)
[ -n "$current" ] || { echo "No <Version> found in $project" >&2; exit 1; }

if ! [[ "$current" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
  echo "The version '$current' is not a plain x.y.z number: bump it by hand." >&2
  exit 1
fi

major="${BASH_REMATCH[1]}"
minor="${BASH_REMATCH[2]}"
patch="${BASH_REMATCH[3]}"

case "$level" in
  major) major=$((major + 1)); minor=0; patch=0 ;;
  minor) minor=$((minor + 1)); patch=0 ;;
  patch) patch=$((patch + 1)) ;;
esac

next="$major.$minor.$patch"

if [ "$dry_run" = true ]; then
  echo "$current -> $next"
  exit 0
fi

sed -i "s|<Version>$current</Version>|<Version>$next</Version>|" "$project"

# Make sure the line really changed before reporting success.
grep -q "<Version>$next</Version>" "$project" || { echo "The version line could not be updated." >&2; exit 1; }

echo "$current -> $next"
