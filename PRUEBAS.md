# Cómo probar todo (guía para tu PC con Windows)

Esta guía complementa a `PRIMERA_EJECUCION_WINDOWS.md` (instalación y apertura del proyecto). Lo que cambió desde entonces:
hay **62 tests EditMode**, el juego usa **Git LFS** (texturas, modelos y audio de referencia) y se agregó el paquete **glTFast**
(Unity lo descarga solo la primera vez: necesitás internet al abrir el proyecto).

> **Importante:** nada de esto se ejecutó nunca en Play Mode. Lo que se verificó fuera de tu PC: compila en Unity 6000.0.58f2,
> 62 tests de lógica pura pasan, y se generaron imágenes estáticas del mundo. **El juego corriendo es tu primera prueba real.**

## 0. Antes de abrir Unity (2 minutos)

```powershell
git lfs install
git clone -b stable https://github.com/alejoC19/HiddenTreasures.git C:\Dev\HiddenTreasures
cd C:\Dev\HiddenTreasures
git lfs pull
git log --oneline -1
```

- `git lfs pull` es **obligatorio**: sin él las texturas y modelos son archivos de texto de 130 bytes y todo se ve rosa o vacío.
- Clonalo fuera de OneDrive/Escritorio/Documentos y en una ruta corta.
- Abrilo con **Unity 6000.0.58f2** (no otra serie). La primera importación tarda varios minutos.

## 1. Comprobaciones automáticas (5 minutos)

1. **Console** (Window → General → Console) sin errores rojos. Los amarillos de paquetes son normales.
2. Menú **Hidden Treasures → Verificar proyecto** → debe terminar en `=> OK`.
3. **Window → General → Test Runner**
   - **EditMode → Run All** → esperados **62 verdes**.
   - **PlayMode → Run All** → esperados **11 verdes**. *Ojo:* esos 11 tests se escribieron antes de los últimos cambios (misión, menú
     cinemático, bloqueo de movimiento al recoger la pieza) y **nunca se ejecutaron**. Si alguno falla, no asumas que el juego está roto:
     copiame el primero que falle (los demás dependen de él).

## 2. Recorrido de 15 minutos (el orden importa)

Abrí `Assets/_Project/Scenes/Main.unity` y presioná **Play**. Hacé **clic en la vista Game** para que capture el mouse.

### Teclas de depuración (solo en el Editor)
Con **F1** se muestra un panel con FPS, estado, mision y más. Con el panel abierto:
**F2** te lleva junto a la próxima reliquia · **F3** +$1000 · **F4** avanza 3 horas · **F6** cambia el clima · **F7** vuelve al campamento · **F8** llena la energía.

| # | Qué hacer | Qué deberías ver / oír |
|---|---|---|
| 1 | Mirar el **menú principal** sin tocar nada | La cámara orbita lentamente la fogata; el fuego, la bruma y (de noche) las luciérnagas se mueven. Título a la izquierda, sin HUD. Si hay partida guardada, "Continuar" aparece seleccionado. Se oye viento. |
| 2 | **Nueva partida** | Aparece el HUD: arriba a la izquierda el panel de estado y **debajo** el cuadro "OBJETIVO — Encontrar 3 reliquias prehispánicas — Reliquias: 0/3". No se pisan. |
| 3 | Caminar (WASD, Shift correr, C agacharse, **V** cambia 1ª/3ª persona) | El personaje camina y corre con animación; en 3ª persona lleva el detector en la mano. Las pisadas **suenan** distinto en el camino de tierra, en el pasto y sobre la piedra de las ruinas. |
| 4 | Caminar por el **camino de tierra** y la explanada del campamento | Quedan **huellas** de bota oscuras que se desvanecen en ~30 s (máximo 48 a la vez). |
| 5 | **F1** y mirar el bosque | Bosque denso, niebla profunda, no se ve el borde del mapa. En el panel: **FPS ≥ 60** (anotá tu GPU y el número) y "Plantas dibujadas" cambia al girar. |
| 6 | En el campamento, **mostrador** (apuntar + **E**) → comprar el **permiso** | El dinero baja ($120 → $80). Sin permiso no se puede excavar. |
| 7 | **F1 → F2** (te lleva junto a una reliquia) y mover el detector | La barra **SEÑAL** sube al acercarte y el **bip se vuelve más agudo y rápido**. |
| 8 | **Mantener clic izquierdo** sobre la señal fuerte | Barra de progreso, golpes de pala con **sonido de raspado**. Al terminar: el personaje **recoge** la pieza (barra verde "Recogiendo…") y luego la **limpia con el cepillo** ("Limpiando…", 3 pasadas con sonido). **No podés moverte** ~3 s. Sale un **cubo dorado** que sube y gira, con **"clink"**, destello y chispas. |
| 9 | Después de la excavación | Aviso de hallazgo, la misión pasa a **Reliquias: 1/3** y, en el **mapa (M)**, aparece un marcador dorado "Reliquia hallada". |
| 10 | **J** (libreta) | Lista de reliquias y sitios: lo no desenterrado dice **???**; la que acabás de sacar muestra nombre, rareza y descripción. (Tab es el inventario, J la libreta.) |
| 11 | Repetí 7–9 con las otras dos reliquias (F2 otra vez) | Al tercer hallazgo: "¡Misión cumplida!" y +$400. |
| 12 | Volver al campamento → **mostrador → Vender** | Total de lo que valen tus hallazgos, ordenados por valor; al vender suena la **moneda**, aparece "Vendiste N objetos por $X" y se guarda la partida sola. |
| 13 | **Esc → Menú principal → Continuar** | Dinero, mochila, misión, sitios excavados (libreta) y posiciones vuelven como estaban. |
| 14 | **Cerrar Play y volver a abrirlo → Continuar** | Lo mismo: el guardado sobrevive entre sesiones. (El archivo es `hidden_treasures_save.json`; buscalo con el Explorador en `%USERPROFILE%\AppData\LocalLow`.) |
| 15 | **Jeep**: apuntar + **E**, conducir (W/A/S/D), bajar con **E** | Las 4 ruedas giran y las delanteras doblan. Probá el surtidor y el **baúl** (E atrás). |
| 16 | **F1 → F4** hasta ~21 h | De noche: luciérnagas, fuego más visible, grillos (se acaban los pájaros). |
| 17 | **F6** varias veces | Nublado → Lluvia (efecto de lluvia, más viento, sin pájaros) → Niebla (muy densa). |
| 18 | **Esc** (pausa) | Sensibilidad, controles y reanudar funcionan; el fondo no es cinemático (solo en el menú principal). |

### Calidad gráfica y rendimiento
- Hay **tres niveles** (Bajo / Medio / Alto) en el menú principal y en la pausa ("Calidad gráfica: …"). El juego arranca en **Medio** en una GPU de 8 GB (como la RX 580) y, si los FPS promedio quedan por debajo de 40 durante ~6 s de juego, **baja solo un escalón** y te avisa.
- El proyecto ahora usa **DirectX 11** en Windows (antes Unity elegía DirectX 12). Si en tu PC se ve mal o va peor, cambialo en *Edit → Project Settings → Player → Other Settings → Auto Graphics API*.
- Con **F1** mirá FPS y "Plantas dibujadas". Mandame esos números por nivel (Bajo/Medio/Alto).

### Medir el rendimiento de verdad (RX 580)
El juego trae un **perfilado reproducible**: recorre 4 lugares (campamento, camino, bosque denso, ruinas) en los 3 niveles de calidad (≈ 3 min) y escribe un informe con FPS promedio, "1 % low", tiempos de CPU y GPU, draw calls, triángulos y memoria.

1. En Unity: menú **Hidden Treasures → Compilar build de rendimiento (Windows)**. Genera `Builds\Perf\HiddenTreasuresPerf.exe` (build de desarrollo; es mucho más fiel que el Play Mode del Editor).
2. Cerrá otros programas y ejecutá, en PowerShell, desde la carpeta del proyecto:
   `Builds\Perf\HiddenTreasuresPerf.exe -htperf -screen-width 1920 -screen-height 1080 -screen-fullscreen 0`
3. El juego mide solo y se cierra. El informe queda en `%USERPROFILE%\AppData\LocalLow\<empresa>\<producto>\perf\<fecha>\perf_summary.md` (más `perf_frames.csv` con cada cuadro). Mandame el `perf_summary.md`.
- *Alternativa rápida (menos fiable):* en el Editor, en Play Mode, tecla **F9**.
- Lectura: si **GPU ms > CPU ms** el límite es la tarjeta; si **CPU ms > GPU ms**, el procesador (draw calls, scripts). 60 FPS = 16,7 ms.

## 3. Lo que más probablemente falle (y qué hacer)

| Síntoma | Causa probable | Acción |
|---|---|---|
| Todo **rosa/magenta** o sin texturas | Falta `git lfs pull` | Ejecutalo y reabrí Unity. |
| **Sin plantas/árboles** | El instancing (`Graphics.RenderMeshInstanced`) no dibuja en tu GPU | F1: si "Plantas dibujadas" es 0, avisame con tu GPU. |
| Vehículo sin ruedas separadas o personaje invisible | Falla al cargar los modelos (glTFast / FBX legible) | Mirá la Console y mandame el mensaje. |
| **Sin sonido** | Audio de Windows / Unity en silencio | Vista Game → botón de altavoz no silenciado. |
| Personaje en T-pose | La animación (Playables) no arrancó | Console: buscá `[Personaje]`. |
| Mouse no responde | Input System | Ver tabla de `PRIMERA_EJECUCION_WINDOWS.md` (sección G). |
| FPS bajos (< 40) | Sombras/instancing | F1, anotá FPS y GPU; probá bajar la calidad en Edit → Project Settings → Quality. |

## 4. Qué enviarme

Pegame esto completo (o una captura del panel F1):

```
Commit (git log --oneline -1):
GPU / CPU / RAM:
Tests: EditMode __/62   PlayMode __/11   (primer test que falló: ...)
FPS en el campamento: __   FPS en lo más denso del bosque: __
Pasos de la tabla que fallaron (números): ...
Errores rojos de la Console (copiados): ...
Capturas (si algo se ve mal): ...
```

## 5. Lo que NO está verificado (para que sepas dónde mirar con lupa)
- **Todo el gameplay en movimiento**: no hubo Play Mode.
- **Sonido**: los generadores pasaron tests de forma de onda (frecuencia, volumen, bucle), pero nadie los escuchó. Pueden necesitar ajuste de volumen o timbre.
- **Poses del personaje** al cavar, recoger y limpiar (calculadas por código, solo verificadas con registros), y el detector en la mano.
- **Jeep**: ruedas separadas de la malla por geometría; la dirección de giro o el centrado pueden requerir ajuste. Licencia CC-BY-SA (ver `CREDITS_ASSETS.md`).
- **Rendimiento real** con ~48.700 instancias de vegetación; medido solo por software.
- **Animaciones propias**: cuando tengas las de Mixamo, seguí `ANIMACIONES.md`.
