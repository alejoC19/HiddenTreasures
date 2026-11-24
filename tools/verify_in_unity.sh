#!/usr/bin/env bash
# Verifica con un Editor REAL (sin Play Mode, no requiere licencia en batch): abre Main.unity, resuelve el script
# del bootstrap por GUID y comprueba URP, Input System y paquetes. Compila ademas todo con el compilador de Unity.
# Uso: tools/verify_in_unity.sh <carpeta-del-Editor>   (p. ej. ~/Unity/Hub/Editor/6000.0.58f2 en Linux)
set -euo pipefail
EDITOR_DIR=$1
ROOT=$(cd "$(dirname "$0")/.." && pwd)
W=$(mktemp -d); mkdir -p "$W/project"
cp -r "$ROOT/Assets" "$ROOT/Packages" "$ROOT/ProjectSettings" "$W/project/"
"$EDITOR_DIR/Editor/Unity" -batchmode -nographics -projectPath "$W/project" \
  -executeMethod HuesoOxido.Core.EditorTools.ProjectVerifier.RunBatch -logFile "$W/editor.log" || true
echo "errores de compilacion: $(grep -c 'error CS' "$W/editor.log")   advertencias: $(grep -c 'warning CS' "$W/editor.log")"
grep -E "^\[VERIFY\]" "$W/editor.log" | cut -c1-200 || { echo "sin salida del verificador (ver $W/editor.log)"; exit 2; }
grep -q "RESULTADO: OK" "$W/editor.log"
