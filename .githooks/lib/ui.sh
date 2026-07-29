#!/usr/bin/env bash

if [[ -t 1 && "${NO_COLOR:-}" == "" ]]; then
  UI_GREEN='\033[0;32m'; UI_RED='\033[0;31m'; UI_YELLOW='\033[1;33m'
  UI_CYAN='\033[0;36m'; UI_DIM='\033[2m'; UI_RESET='\033[0m'
else
  UI_GREEN=''; UI_RED=''; UI_YELLOW=''; UI_CYAN=''; UI_DIM=''; UI_RESET=''
fi

HOOK_STARTED_AT="$(date +%s)"
HOOK_PASSED=0

hook_header() {
  printf "\n${UI_CYAN}╭─ ClaudyGod Quality Gate ─────────────────────────────╮${UI_RESET}\n"
  printf "${UI_CYAN}│${UI_RESET}  %s\n" "$1"
  printf "${UI_CYAN}╰───────────────────────────────────────────────────────╯${UI_RESET}\n\n"
}

info() { printf "${UI_CYAN}→${UI_RESET} %s\n" "$*"; }
warn() { printf "${UI_YELLOW}⚠${UI_RESET}  %s\n" "$*"; }

die() {
  printf "\n${UI_RED}✗ Quality gate failed${UI_RESET}\n  %s\n" "$*" >&2
  printf "  ${UI_DIM}Nothing was pushed. Fix the issue and run the command again.${UI_RESET}\n\n" >&2
  exit 1
}

run_step() {
  local label="$1" started elapsed
  shift
  started="$(date +%s)"
  printf "${UI_CYAN}●${UI_RESET} %s\n" "$label"
  if "$@"; then
    elapsed=$(( $(date +%s) - started ))
    HOOK_PASSED=$((HOOK_PASSED + 1))
    printf "${UI_GREEN}✓${UI_RESET} %s ${UI_DIM}(%ss)${UI_RESET}\n\n" "$label" "$elapsed"
  else
    elapsed=$(( $(date +%s) - started ))
    printf "${UI_RED}✗${UI_RESET} %s ${UI_DIM}(%ss)${UI_RESET}\n" "$label" "$elapsed" >&2
    die "${HOOK_FAILURE_HINT:-Review the command output above.}"
  fi
}

hook_success() {
  local elapsed=$(( $(date +%s) - HOOK_STARTED_AT ))
  printf "${UI_GREEN}╭─ Passed ──────────────────────────────────────────────╮${UI_RESET}\n"
  printf "${UI_GREEN}│${UI_RESET}  %s check(s) passed in %ss\n" "$HOOK_PASSED" "$elapsed"
  printf "${UI_GREEN}│${UI_RESET}  %s\n" "$1"
  printf "${UI_GREEN}╰───────────────────────────────────────────────────────╯${UI_RESET}\n\n"
}
