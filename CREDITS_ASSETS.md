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
