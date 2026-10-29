#!/bin/sh
# Claude Code PreToolUse hook (Bash): scan for secrets before `git commit` or `git push`.
# The tool call arrives as JSON on stdin; exit 2 blocks it and shows stderr to Claude.
input=$(cat)
cd "${CLAUDE_PROJECT_DIR:-.}" || exit 0

case "$input" in
  *'git commit'*|*'git push'*) ;;
  *) exit 0 ;;
esac

dir=.githooks

case "$input" in
  *'git push'*)
    sh "$dir/scan-secrets.sh" unpushed >&2 || { echo "Push blocked: gitleaks found a possible secret in the commits to push." >&2; exit 2; }
    ;;
esac

case "$input" in
  *'git commit'*)
    sh "$dir/scan-secrets.sh" staged >&2 || { echo "Commit blocked: gitleaks found a possible secret in the staged changes." >&2; exit 2; }
    ;;
esac
exit 0
