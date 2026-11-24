#!/usr/bin/env bash
# Valida compilacion y tests puros con .NET. Uso: tools/validate.sh [work_dir]
set -e
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
WORK=${1:-/home/user/tools/val}
ROOT=$(cd "$(dirname "$0")/.." && pwd)
python3 "$ROOT/tools/gen_validation.py" "$ROOT" "$WORK"
fail=0
while read -r p; do
  case "$p" in *Tests*) continue;; esac
  out=$(dotnet build "$p" -c Release -v q 2>&1 | grep -E "error|warning CS" | grep -v CS2008 | sed 's#.*/Assets/#Assets/#' | sort -u || true)
  if [ -n "$out" ]; then echo "== $(basename "$p")"; echo "$out"; fi
  echo "$out" | grep -q "error" && fail=1
done < "$WORK/proj/projects.txt"
for p in $(grep Tests "$WORK/proj/projects.txt"); do dotnet test "$p" -v q 2>&1 | tail -15; done
exit $fail
