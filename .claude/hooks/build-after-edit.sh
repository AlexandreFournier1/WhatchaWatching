#!/bin/sh
# Claude Code PostToolUse hook (Edit|Write|MultiEdit): build after a .cs or .razor file changes.
# Exit 2 hands the compiler errors back to Claude; tests stay in /commit-push.
input=$(cat)

path=$(printf '%s' "$input" | sed -n 's/.*"file_path" *: *"\([^"]*\)".*/\1/p' | head -n 1)
case "$path" in
  *.cs|*.razor) ;;
  *) exit 0 ;;
esac

cd "${CLAUDE_PROJECT_DIR:-.}" || exit 0

out=$(dotnet build WhatchaWatching.slnx --nologo -v q -clp:NoSummary 2>&1)
if [ $? -ne 0 ]; then
  printf '%s\n' "$out" | grep -E 'error [A-Za-z]+[0-9]+' | sort -u | head -n 30 >&2
  echo "dotnet build failed after editing $path." >&2
  exit 2
fi
exit 0
