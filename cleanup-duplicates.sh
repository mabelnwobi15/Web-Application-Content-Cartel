#!/usr/bin/env bash
# Content Cartel - safe duplicate cleanup.
# Run from the REPO ROOT (the folder containing ContentCartel.slnx), on a new branch.
#   ./cleanup-duplicates.sh            -> dry run, only prints what it would do
#   ./cleanup-duplicates.sh --apply    -> actually does it
set -euo pipefail

[ -f ContentCartel.slnx ] || { echo "Run this from the repo root (where ContentCartel.slnx is)."; exit 1; }
git rev-parse --is-inside-work-tree >/dev/null 2>&1 || { echo "Not a git repo."; exit 1; }

APPLY=0; [ "${1:-}" = "--apply" ] && APPLY=1
say() { if [ $APPLY = 1 ]; then echo "$*"; else echo "[dry-run] $*"; fi; }

NESTED="Web-Application-Content-Cartel-main"

# 1. Nested full copy of the whole project -------------------------------
if [ -d "$NESTED" ]; then
  echo "== 1. Nested copy: ./$NESTED"
  echo "   Files that differ from the real project (root version is the live one):"
  diff -rq . "$NESTED" --exclude="$NESTED" --exclude=.git 2>/dev/null | grep -v -e "^Only in \./" -e "\.gradle" -e "/build/" | sed 's/^/     /' | head -20 || true
  say "git rm -r $NESTED"
  if [ $APPLY = 1 ]; then git rm -r -q --ignore-unmatch "$NESTED"; rm -rf "$NESTED"; fi
fi

# 2. Generated files that should never be in git -------------------------
echo "== 2. Untrack generated/IDE files (kept on your disk)"
for p in .gradle build; do
  if git ls-files --error-unmatch "$p" >/dev/null 2>&1; then
    say "git rm -r --cached $p"
    if [ $APPLY = 1 ]; then git rm -r -q --cached "$p"; fi
  fi
done
while read -r f; do
  [ -z "$f" ] && continue
  case "$f" in "$NESTED"/*) continue ;; esac   # goes away with the nested copy
  say "git rm --cached $f"
  if [ $APPLY = 1 ]; then git rm -q --cached "$f"; fi
done < <(git ls-files '*.csproj.user')

# 3. .gitignore (the old one had literal "\n" text on line 1, so it ignored nothing)
echo "== 3. Replace .gitignore"
if [ $APPLY = 1 ]; then
cat > .gitignore <<'IGN'
# Gradle / IDE leftovers
*.iml
.gradle/
/build
/captures
.externalNativeBuild
.cxx
local.properties
/.idea/caches
/.idea/libraries
/.idea/modules.xml
/.idea/workspace.xml
/.idea/navEditor.xml
/.idea/assetWizardSettings.xml
.DS_Store

# Visual Studio / .NET
.vs/
bin/
obj/
*.csproj.user

# Secrets
firebase-service-account.json*
google-services.json
appsettings.Local.json
IGN
else
  echo "[dry-run] write new .gitignore"
fi

echo
if [ $APPLY = 1 ]; then
  git add -A
  echo "Done. Review with: git status   then commit:  git commit -m \"Remove duplicate copy and generated files\""
else
  echo "Dry run finished. Re-run with --apply to make the changes."
fi
