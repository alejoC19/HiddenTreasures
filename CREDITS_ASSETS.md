# Créditos de assets de terceros

Todas las texturas de esta lista son **CC0 (dominio público)**, de [Poly Haven](https://polyhaven.com). No exigen atribución; se acredita igualmente. Descarga reproducible: `tools/fetch_cc0_assets.py`.

| Uso en el juego | ID en Poly Haven | Autores |
|---|---|---|
| `forest_floor` | [forest_ground_04](https://polyhaven.com/a/forest_ground_04) | Rob Tuytel, Rico Cilliers |
| `grass` | [leafy_grass](https://polyhaven.com/a/leafy_grass) | Charlotte Baglioni |
| `road` | [stony_dirt_path](https://polyhaven.com/a/stony_dirt_path) | eye-candy.xyz |
| `red_earth` | [red_laterite_soil_stones](https://polyhaven.com/a/red_laterite_soil_stones) | Amal Kumar |
| `rock` | [mossy_rock](https://polyhaven.com/a/mossy_rock) | Rob Tuytel |
| `bark_pine` | [pine_bark](https://polyhaven.com/a/pine_bark) | Dimitrios Savva |
| `bark_broadleaf` | [bark_brown_02](https://polyhaven.com/a/bark_brown_02) | Rob Tuytel |
| `ruin_stone` | [mossy_stone_wall](https://polyhaven.com/a/mossy_stone_wall) | Amal Kumar |
| `ruin_blocks` | [medieval_blocks_02](https://polyhaven.com/a/medieval_blocks_02) | Rob Tuytel |
| `wood_planks` | [weathered_planks](https://polyhaven.com/a/weathered_planks) | Dario Barresi, Dimitrios Savva |

## Modelos (Poly Haven, CC0)

Descarga reproducible: `tools/fetch_cc0_props.py`.

| ID | Autores | Polígonos |
|---|---|---|
| [stone_fire_pit](https://polyhaven.com/a/stone_fire_pit) | Sebastian Platen | 3887 |
| [wooden_crate_01](https://polyhaven.com/a/wooden_crate_01) | James Ray Cock | 6576 |
| [wooden_crate_02](https://polyhaven.com/a/wooden_crate_02) | James Ray Cock, Jurita Burger | 5176 |
| [barrel_03](https://polyhaven.com/a/barrel_03) | Serhii Khromov | 1473 |
| [wooden_bucket_01](https://polyhaven.com/a/wooden_bucket_01) | James Ray Cock | 5116 |
| [vintage_oil_lamp](https://polyhaven.com/a/vintage_oil_lamp) | Monsta3D | 7208 |
| [wooden_lantern_01](https://polyhaven.com/a/wooden_lantern_01) | James Ray Cock | 8321 |
| [wooden_ladder_02](https://polyhaven.com/a/wooden_ladder_02) | JN_3DPRINTINGNERD | 6126 |
| [antique_ceramic_vase_01](https://polyhaven.com/a/antique_ceramic_vase_01) | James Ray Cock | 9408 |
| [ceramic_vase_02](https://polyhaven.com/a/ceramic_vase_02) | James Ray Cock | 2756 |
| [brass_vase_03](https://polyhaven.com/a/brass_vase_03) | Rico Cilliers | 2616 |

## Vehículo y personaje (modelos 3D reales)

Ambos se cargan por código desde `Resources/Art/Models/`; si faltan, el juego usa el respaldo procedural (camioneta y explorador de primitivas con materiales PBR pintados por código).

| Archivo | Origen | Licencia | Notas |
|---|---|---|---|
| `WillysJeep.fbx` + `willys_jeep_{diff,nor,ao_arm}.png` | [Willy's Jeep Asset](https://opengameart.org/content/willys-jeep-asset) (OpenGameArt) | **CC-BY-SA 3.0 / GPL 3.0** | Jeep clásico, 20k triángulos, texturas PBR 2K. Las 4 ruedas se separan de la malla al cargar (`MeshSplitter`). **Atención:** la licencia es *share-alike*; si se publica el juego hay que revisarla (o reemplazar el modelo, ver abajo). |
| `Soldier.glb` | Personaje "Soldier" de Mixamo, distribuido en los ejemplos de [three.js](https://github.com/mrdoob/three.js/tree/dev/examples/models/gltf) (MIT para el repo) | Términos de uso de Mixamo (uso libre en proyectos; no redistribuir el archivo suelto) | Rig + clips Idle / Walk / Run. Importado con el paquete oficial glTFast (`com.unity.cloud.gltfast` 6.9.1). |

Reemplazar un modelo: dejar el nuevo archivo con el mismo nombre en `Resources/Art/Models/` (o cambiar `PlaceholderVehicle.ModelPath` / `PlaceholderCharacter.ModelPath`). Alternativas libres ya localizadas: [UAZ pickup](https://opengameart.org/content/uaz-truck) (CC-BY 4.0, sin texturas, ruedas fusionadas) y [GAZ-67](https://opengameart.org/content/lowpoly-soviet-jeep) (CC0, low-poly).

Todo lo demás del fuego, la camioneta de respaldo, el detector y el explorador de respaldo se genera por código (sin archivos externos).

## Camioneta principal (pickup moderna)

| Archivo | Origen | Licencia | Atribución obligatoria |
|---|---|---|---|
| `Resources/Art/Models/Silverado.glb` | "2019 Chevrolet Silverado Trail Boss Z71" de **Ddiaz Design** — https://sketchfab.com/3d-models/2019-chevrolet-silverado-trail-boss-z71-652324cc8a974d3a9869ce2b0f3beaaa | **CC-BY-4.0** (uso comercial permitido) | *This work is based on "2019 Chevrolet Silverado Trail Boss Z71" (https://sketchfab.com/3d-models/2019-chevrolet-silverado-trail-boss-z71-652324cc8a974d3a9869ce2b0f3beaaa) by Ddiaz Design (https://sketchfab.com/ddiaz-design) licensed under CC-BY-4.0 (http://creativecommons.org/licenses/by/4.0/)* |

Modificaciones (CC-BY exige indicarlas): reducido de 478 874 a ~234 000 triángulos (meshoptimizer), partes fusionadas por material (192 → 51 mallas), llantas y rines conservados por separado para girar, materiales convertidos a URP/Lit con valores de metal/brillo ajustados, y orientación/escala/anclajes de ruedas calculados al cargar. El texto de licencia original está en `Resources/Art/Models/Silverado_LICENSE.txt`. **Pendiente:** mostrar esta atribución dentro del juego (pantalla de créditos) antes de publicarlo.
El Willys Jeep sigue en el proyecto solo como respaldo si falta la Silverado.

## Modelos de Sketchfab (CC BY 4.0: atribución obligatoria)

Modificados para el juego: decimados, divididos en LOD, escalados a medidas reales y con texturas reorganizadas (`tools/art/`).

| Uso | Título | Autor | Licencia |
|---|---|---|---|
| pine_wenedi | [Pine Tree [Game-ready]](https://sketchfab.com/3d-models/pine-tree-game-ready-dc3fbd9205cf4027a4455d1f415e0478) | Wenedi (^-^)/ | CC BY 4.0 |
| beech | [Beech tree](https://sketchfab.com/3d-models/beech-tree-0983d8933531491f9be71c669e8a907b) | evolveduk | CC BY 4.0 |
| deadtree | [Dead Tree](https://sketchfab.com/3d-models/dead-tree-33ccce6111604656a4b5709483670ec3) | MaX3Dd | CC BY 4.0 |
| deadpine | [Mid Poly Dead Pine Tree](https://sketchfab.com/3d-models/mid-poly-dead-pine-tree-84b73e8b6efb4ca7b47409c89689927a) | sujirour | CC BY 4.0 |
| ruin_arch / ruin_pillar_a / ruin_pillar_b (derivados) | [Stone Arch Pillars - Ruins](https://sketchfab.com/3d-models/stone-arch-pillars-ruins-a1a8e633c9aa40c08b4d2a0631ac61d2) | Unreal Designer | CC BY 4.0 |
| temple | [Old Ruined Temple](https://sketchfab.com/3d-models/old-ruined-temple-bb2b2cc8db1f4fb49917b0eb8cb83977) | Muhammet Gündel | CC BY 4.0 |
| modarch | [Modular Ancient Stone Archway - PBR Game Ready](https://sketchfab.com/3d-models/modular-ancient-stone-archway-pbr-game-ready-052a392328b0451db4eaa257aeea969c) | Hardframe | CC BY 4.0 |
| bigrock | [Big Rock 1B](https://sketchfab.com/3d-models/big-rock-1b-44c96d506d55496598b6b92dec8b1049) | Andrei.D | CC BY 4.0 |
| rockcliff | [Rockcliff](https://sketchfab.com/3d-models/rockcliff-3e3b2d1cfda0493da4e1ec150f046c41) | DJMaesen | CC BY 4.0 |
| cliff | [Cliff](https://sketchfab.com/3d-models/cliff-082da1166a814c6e9c9e6c1b38159e4e) | DJMaesen | CC BY 4.0 |
| desertcliff | [Desert Cliff 6](https://sketchfab.com/3d-models/desert-cliff-6-ed2e9c7e976e4768868481e4a5c63f6f) | WireframeArt | CC BY 4.0 |
| fallentrunk | [Pine Tree Fallen Trunk Scan 02](https://sketchfab.com/3d-models/pine-tree-fallen-trunk-scan-02-f5ad948275cd4d13ba3ce2040b72ed47) | Pers Scans | CC BY 4.0 |
| logpile | [Log Pile (Game Ready / 2K PBR)](https://sketchfab.com/3d-models/log-pile-game-ready-2k-pbr-3f4205a30a7d4adda50c775fb8cce06c) | Meerschaum Digital | CC BY 4.0 |
| sandarch | [Ancient Ruined Sandstone Archway & Gate](https://sketchfab.com/3d-models/ancient-ruined-sandstone-archway-gate-76fe696447c54e9ca4fa97c591e75ccc) | mana5677 | CC BY 4.0 |
| abbey | [Abbey Wall Ruins](https://sketchfab.com/3d-models/abbey-wall-ruins-37936ba6a1674f15be60075315469a43) | tonyflanagan | CC BY 4.0 |

## Modelos y materiales CC0 añadidos (arte del mundo)

Poly Haven (modelos, ya en metros): rock_07, rock_09, stone_01, rock_moss_set_01, rock_moss_set_02, tree_stump_01, tree_stump_02, dead_tree_trunk, shrub_03, shrub_04, fern_02, nettle_plant, weed_plant_02, dry_branches_medium_01. Descarga: `tools/fetch_polyhaven_models.py`.

ambientCG (texturas 2K, CC0, aún sin integrar): Moss002, Ground068, Ground037, Ground071, Gravel041, Gravel043, Rock064, Ground011, ScatteredLeaves009, Bark014. Descarga: `tools/fetch_ambientcg.py`.

### Añadidos: cactus y detector (Sketchfab, CC BY 4.0)

| Uso | Título | Autor |
|---|---|---|
| saguaro1..4 | [Realistic HD Saguaro cactus (27/30)](https://sketchfab.com/3d-models/realistic-hd-saguaro-cactus-2730-124202937a8c45e091f3af365446a087) | PlantCatalog |
| saguaro1..4 | [Realistic HD Saguaro cactus (7/30)](https://sketchfab.com/3d-models/realistic-hd-saguaro-cactus-730-24416333c66b4c34ab042cd93ac44a32) | PlantCatalog |
| saguaro1..4 | [Realistic HD Saguaro cactus (26/30)](https://sketchfab.com/3d-models/realistic-hd-saguaro-cactus-2630-f59d9c31832246819f718088ca654664) | PlantCatalog |
| saguaro1..4 | [Realistic HD Saguaro cactus (17/30)](https://sketchfab.com/3d-models/realistic-hd-saguaro-cactus-1730-c9310cae729f471e82c355a15efebed9) | PlantCatalog |
| detector | [Metal Detector](https://sketchfab.com/3d-models/metal-detector-2527ebf908e94ee88d00aad6fa062377) | Tr0n |

Variante nevada del pino (`pine_snow`): derivada de Pine Tree [Game-ready] (Wenedi), con color mezclado hacia blanco (`tools/art/make_snow_variant.py`).

### Campamento (Sketchfab, CC BY 4.0)

| Uso | Título | Autor |
|---|---|---|
| surtidor | [Vintage Gas Pump](https://sketchfab.com/3d-models/vintage-gas-pump-0096f206ce1a4381bea58f9f94fc6b1c) | Rescue3D Assets |
| carpa | [Military Tent](https://sketchfab.com/3d-models/military-tent-d0d6d9a9f5ab4939932dd27bbaa5ca7d) | Khata |
| mostrador | [Medieval Market Stall](https://sketchfab.com/3d-models/medieval-market-stall-b5d87398b87a4e7ab8959da2c6a47056) | AspectStudios |
| bidones | [Game Ready Jerry Cans](https://sketchfab.com/3d-models/game-ready-jerry-cans-01fea903b3464194a4d0eec7d9a1a5d1) | Liam3D |
