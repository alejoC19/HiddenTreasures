#!/usr/bin/env bash
# Ejecuta los tests en un Editor REAL de Unity en modo batch (sin grafica: -nographics).
# Uso: tools/run_unity_tests.sh <carpeta-del-Editor> <EditMode|PlayMode> [salida]
# Trabaja sobre una COPIA del proyecto para no ensuciar el repo con Library/.
set -euo pipefail
EDITOR_DIR=$1; PLATFORM=${2:-PlayMode}; OUT=${3:-/tmp/unity-tests}
ROOT=$(cd "$(dirname "$0")/.." && pwd)
mkdir -p "$OUT"; rm -rf "$OUT/project"; mkdir -p "$OUT/project"
cp -r "$ROOT/Assets" "$ROOT/Packages" "$ROOT/ProjectSettings" "$OUT/project/"
"$EDITOR_DIR/Editor/Unity" -batchmode -nographics -projectPath "$OUT/project" \
  -runTests -testPlatform "$PLATFORM" -testResults "$OUT/results-$PLATFORM.xml" -logFile "$OUT/editor-$PLATFORM.log" || true
python3 - "$OUT/results-$PLATFORM.xml" <<'PY'
import sys, xml.etree.ElementTree as ET
try: r = ET.parse(sys.argv[1]).getroot()
except Exception as e: print("SIN RESULTADOS (¿error de compilacion?):", e); sys.exit(2)
print("total=%s passed=%s failed=%s skipped=%s" % (r.get("total"), r.get("passed"), r.get("failed"), r.get("skipped")))
for t in r.iter("test-case"):
    print(("  OK   " if t.get("result") == "Passed" else "  FAIL ") + t.get("name") + "  (%ss)" % t.get("duration"))
    if t.get("result") != "Passed":
        m = t.find(".//message")
        print("       " + (m.text or "").strip().replace("\n", "\n       ")[:900] if m is not None else "")
sys.exit(0 if r.get("failed") == "0" else 1)
PY
