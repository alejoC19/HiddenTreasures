# Plan de arte: integrar assets reales (sector de referencia)

Estado: **propuesta, sin integrar nada todavia.** Todo gratis. Evaluado por poligonos, licencia y apariencia (miniaturas revisadas una por una).

## Licencias
- Poly Haven y ambientCG: CC0 (sin atribucion obligatoria).
- Sketchfab: CC-BY (atribucion obligatoria: va en `CREDITS_ASSETS.md` y en la pantalla de creditos del menu). Descargar requiere un token de cuenta (formulario seguro; el token viejo hay que regenerarlo).

## Sector de referencia
Unos 60 x 60 m alrededor del camino que lleva a las ruinas (camino + ruinas + claro de bosque + camioneta). Se termina y se aprueba antes de tocar el resto del mundo.

## Seleccion por fase
| Fase | Elemento | Assets elegidos | Tris (origen -> objetivo) | Notas |
|---|---|---|---|---|
| 1 | Suelo PBR | ambientCG: Moss002, Ground068, Ground037, Ground071, Gravel041/043, Rock064; ya tenemos Poly Haven (forest_floor, grass, road, red_earth) | 2K, color+normal+rugosidad+AO | El shader de suelo hoy usa suavidad fija: se le agrega rugosidad por mapa y mezcla por altura |
| 2 | Coniferas | Sketchfab "Low Poly Forest Tree Pack" (99.Miles, CC-BY, 3.7k total, 9 arboles realistas), "Pine Tree Game-ready" (Wenedi, 1.6k) | <= 4k + LOD | Mejor conjunto de coniferas gratis que encontre |
| 2 | Latifoliados | Sketchfab "Beech tree" (evolveduk, 6.5k, hojas realistas) | <= 6.5k + LOD | |
| 2 | Arboles muertos | "Dead Tree" (MaX3Dd, 3k), "Mid Poly Dead Pine" (sujirour, 6k) | 3-6k | Tambien sirven al pantano |
| 2 | Sotobosque | Poly Haven: shrub_02/03/04, fern_02, nettle_plant, weed_plant_02, dry_branches_medium_01 (varios con LOD) | 3k-50k -> 1-8k | Reemplazan las tarjetas de pasto/helecho actuales |
| 3 | Rocas | Poly Haven: rock_07, rock_09, rock_moss_set_01/02, stone_01 (LOD), boulders; Sketchfab: Big Rock 1B (24k), Rockcliff (7k), Cliff (5.7k); desierto: Desert Cliff (14k) | 20-70k -> 3-8k + LOD | Reemplazan las rocas procedurales |
| 3 | Troncos y tocones | Poly Haven dead_tree_trunk, tree_stump_01/02; Sketchfab Fallen Trunk Scan (49k), Log Pile (3.8k) | 40-100k -> 5-10k | |
| 4 | Ruinas | Sketchfab: Abbey Wall Ruins (58k, musgo), Stone Arch Pillars (131k), Old Ruined Temple (20k), Modular Arch (3k); desierto: Ancient Ruined Sandstone Archway (17k) | 20-130k -> 5-15k | Piedras individuales y musgo reales; se modifica solo el visual, no el gameplay |
| 5 | Camioneta | Ya integrada: Silverado (CC-BY). Se re-evalua dentro del sector (pintura, barro, reflejos) | 234k -> LOD | |
| 6 | Herramientas | Detector candidatos: "Game Ready Old Detector" (11.7k), "Professional Metal Detector" (glowbox3d, 7.4k), "Metal Detector" (tronlives, 16.5k) | <= 8k | Se compara en pantalla antes de elegir |

## Pipeline tecnico
1. Descarga (API de Poly Haven / ambientCG sin token; Sketchfab con token).
2. Optimizacion con gltf-transform / meshoptimizer: decimado a objetivo, LOD0/1/2, atlas de texturas a 2K, compresion de texturas.
3. Import en Unity (glTFast), materiales URP/Lit con alpha clip para hojas, doble cara solo donde haga falta.
4. Instanciado por GPU con el sistema existente (`InstancedVegetation`: LOD, distancias, sombras); no se crean sistemas nuevos.
5. Verificacion: render en Unity a ras de suelo con calidad Alta + medicion F9 en la RX 580 (solo tu PC puede dar FPS reales).

## Presupuesto de referencia (RX 580 8 GB, 1080p, 60 FPS: objetivo, no promesa)
- Arbol cercano <= 7k tris, LOD1 ~35 %, LOD2 billboard.
- Texturas 2K comprimidas, total < 2,5 GB de VRAM.

## Limites honestos
- Ningun arbol gratis que encontre iguala a las capturas de referencia (que usan escaneos tipo Megascans). Lo gratis llega a "bien trabajado, convincente"; el salto final seria con plata.
- Poly Haven no tiene arboles, solo rocas, arbustos, tocones y troncos.
- Asset Store de Unity: se puede mirar, pero bajar assets requiere tu cuenta.
