#!/usr/bin/env bash
# Procesa todos los assets elegidos (ver ARTE_PLAN.md). Requiere /home/user/art_raw (descargas) y node con @gltf-transform + meshoptimizer.
# Uso: tools/art/build_all.sh <entrada_raw> <salida_work>
set -e
R=${1:-/home/user/art_raw}; W=${2:-/home/user/art_work}
cd "$(dirname "$0")"
run(){ node prep.mjs "$@" | tail -1; }
# --- Sketchfab (escala a altura real en metros) ---
run $R/beech.glb $W/beech beech 11 1,0.4,0.15
run $R/pine_wenedi.glb $W/pine_wenedi pine_wenedi 15 1,0.5,0.2
run $R/deadtree.glb $W/deadtree deadtree 6 1,0.5,0.2
run $R/deadpine.glb $W/deadpine deadpine 9 1,0.5,0.2
run $R/abbey.glb $W/abbey abbey 5.5 0.3,0.12
run $R/archpillars.glb $W/ruin_arch ruin_arch 0 0.2,0.08 'Object_(5|6)$'
run $R/archpillars.glb $W/ruin_pillar_a ruin_pillar_a 0 0.2,0.08 'Object_(9|10)$'
run $R/archpillars.glb $W/ruin_pillar_b ruin_pillar_b 0 0.2,0.08 'Object_(13|14)$'
run $R/temple.glb $W/temple temple 6 0.6,0.25
run $R/modarch.glb $W/modarch modarch 4.5 1,0.5
run $R/bigrock.glb $W/bigrock bigrock 3.2 0.3,0.1
run $R/rockcliff.glb $W/rockcliff rockcliff 5 1,0.4
run $R/cliff.glb $W/cliff cliff 3 1,0.4
run $R/desertcliff.glb $W/desertcliff desertcliff 6 0.6,0.25
run $R/fallentrunk.glb $W/fallentrunk fallentrunk 1.3 0.2,0.08
run $R/logpile.glb $W/logpile logpile 0.7 1,0.5
run $R/sandarch.glb $W/sandarch sandarch 5 0.4,0.15
run $R/saguaro1.glb $W/saguaro1 saguaro1 6.5 0.35,0.12
run $R/saguaro2.glb $W/saguaro2 saguaro2 5.5 0.4,0.15
run $R/saguaro3.glb $W/saguaro3 saguaro3 3.5 0.7,0.3
run $R/saguaro4.glb $W/saguaro4 saguaro4 5 0.5,0.2
run $R/detector.glb $W/detector detector 1.3 0.5,0.2
python3 make_snow_variant.py $W pine_wenedi pine_snow 0.75
run $R/grass_tuft.glb $W/grass_tuft grass_tuft 0.9 1,0.5
# --- Poly Haven (ya vienen en metros) ---
P=$R/ph
run $P/rock_07/rock_07_2k.gltf $W/rock_07 rock_07 0 0.25,0.1
run $P/rock_09/rock_09_2k.gltf $W/rock_09 rock_09 0 0.3,0.12
run $P/stone_01/stone_01_2k.gltf $W/stone_01 stone_01 0 0.1,0.04
run $P/rock_moss_set_01/rock_moss_set_01_2k.gltf $W/rock_moss_set_01 rock_moss_set_01 0 0.12,0.05
run $P/rock_moss_set_02/rock_moss_set_02_2k.gltf $W/rock_moss_set_02 rock_moss_set_02 0 0.12,0.05
run $P/tree_stump_01/tree_stump_01_2k.gltf $W/tree_stump_01 tree_stump_01 0 0.15,0.06
run $P/tree_stump_02/tree_stump_02_2k.gltf $W/tree_stump_02 tree_stump_02 0 0.1,0.04
run $P/dead_tree_trunk/dead_tree_trunk_2k.gltf $W/dead_tree_trunk dead_tree_trunk 0 0.1,0.04
run $P/shrub_03/shrub_03_2k.gltf $W/shrub_03 shrub_03 0 0.4,0.15
run $P/shrub_04/shrub_04_2k.gltf $W/shrub_04 shrub_04 0 0.2,0.07
run $P/fern_02/fern_02_2k.gltf $W/fern_02 fern_02 0 1,0.4
run $P/nettle_plant/nettle_plant_2k.gltf $W/nettle_plant nettle_plant 0 0.15,0.05
run $P/weed_plant_02/weed_plant_02_2k.gltf $W/weed_plant_02 weed_plant_02 0 0.35,0.12
run $P/dry_branches_medium_01/dry_branches_medium_01_2k.gltf $W/dry_branches dry_branches 0 0.3,0.1
for d in $W/*/; do n=$(basename $d); mx=1024; case $n in ruin_*|temple|bigrock|rock_moss_set_0*) mx=2048;; esac; python3 post_textures.py $d $mx; done
