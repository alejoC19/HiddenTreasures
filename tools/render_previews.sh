#!/usr/bin/env bash
# Genera imagenes del mundo real del juego con un Editor de Unity real, SIN Play Mode (no es gameplay).
# Linux sin GPU: usa Xvfb + Vulkan por software (lavapipe) si existen. Windows: menu "Hidden Treasures/Generar vistas previas".
# Uso: tools/render_previews.sh <carpeta-del-Editor> [carpeta-de-salida]
set -euo pipefail
EDITOR_DIR=$1; OUT=${2:-${TMPDIR:-/tmp}/ht-previews}
ROOT=$(cd "$(dirname "$0")/.." && pwd)
W=$(mktemp -d); mkdir -p "$W/project" "$OUT"
cp -r "$ROOT/Assets" "$ROOT/Packages" "$ROOT/ProjectSettings" "$W/project/"
export HT_PREVIEW_DIR=$OUT
RUN=("$EDITOR_DIR/Editor/Unity" -batchmode -force-vulkan -projectPath "$W/project" \
     -executeMethod HuesoOxido.Core.EditorTools.PreviewRenderer.RunBatch -logFile "$W/editor.log")
if command -v xvfb-run >/dev/null; then
  ICD=${VK_ICD_FILENAMES:-/opt/lavapipe/lvp_icd.json}
  xvfb-run -a -s "-screen 0 1600x900x24" env VK_ICD_FILENAMES="$ICD" "${RUN[@]}" || true
else
  "${RUN[@]}" || true
fi
grep -E "^\[PREVIEW\]" "$W/editor.log" || { echo "sin salida (ver $W/editor.log)"; exit 2; }
echo "imagenes en $OUT"
