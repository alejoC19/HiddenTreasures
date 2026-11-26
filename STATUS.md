# Estado del proyecto

> **Documentos vigentes** (esta página conserva el historial de etapas y algunas cifras antiguas): visión [`GAME_VISION.md`](GAME_VISION.md) · sistemas y auditoría [`GAMEPLAY_SYSTEMS.md`](GAMEPLAY_SYSTEMS.md) · regiones [`REGION_DESIGN.md`](REGION_DESIGN.md) · hoja de ruta [`ROADMAP.md`](ROADMAP.md) · arquitectura [`TECHNICAL_ARCHITECTURE.md`](TECHNICAL_ARCHITECTURE.md) · arte [`ARTE_PLAN.md`](ARTE_PLAN.md) · pruebas [`PRUEBAS.md`](PRUEBAS.md).
> Cifras actuales: **85 tests EditMode** (pasan en .NET), 11 PlayMode escritos y **sin ejecutar**, compilación verificada en Unity 6000.0.58f2, renders verificados por software; gameplay y rendimiento en hardware real **sin verificar**.

**Versión:** demo v0.1.0 (primera apertura pendiente). **Ramas:** `stable` (consolidada) y `claude/focused-darwin-ps2pj7` (desarrollo).

## Lo único que importa: qué se verificó y qué no
Detalle completo, con evidencia y riesgos: **[`INFORME_TECNICO.md`](INFORME_TECNICO.md)**. Primera ejecución en Windows: **[`PRIMERA_EJECUCION_WINDOWS.md`](PRIMERA_EJECUCION_WINDOWS.md)**.

| Verificación | Resultado | Entorno |
|---|---|---|
| Resolución de paquetes (26) | OK | **Editor real 6000.0.58f2** |
| Compilación de 17 asmdef (juego, Editor, tests) | OK, 0 errores y 0 advertencias | **Editor real** |
| Importación del proyecto y de los URP Assets | OK | **Editor real** |
| `Main.unity` abre, script por GUID, URP + SSAO, Input System, paquetes (`ProjectVerifier`, 17 comprobaciones) | OK | **Editor real** |
| Auditoría estática del repo | OK (125 `.meta`, GUIDs únicos, sin ciclos) | script |
| 34 tests EditMode (lógica pura) | OK | .NET (no dentro de Unity) |
| 11 tests PlayMode (arranque, loop, vehículo, guardado…) | **Escritos y compilados; NO ejecutados** | requieren licencia de Unity |
| **Ejecución del juego (Play Mode), render, física, UI en pantalla, rendimiento** | **NO VERIFICADO** | requiere licencia (y GPU para render) |

### Por qué no se ejecutó
Play Mode y el Test Runner dieron «No valid Unity Editor license found». No se activó ninguna licencia (requiere la cuenta o los términos del usuario). Ver `INFORME_TECNICO.md` §12.

### Riesgos principales
1. Errores de ejecución al dar Play por primera vez (el código nunca corrió). 2. Render de terreno/URP/cielo/post-proceso (solo se probó con `-nographics`). 3. Física del vehículo y balance. 4. Layout de UI. 5. Rendimiento. 6. Builds de jugador (shaders con `Shader.Find`).

## Qué contiene la demo (implementado)
Personaje (caminar/correr/agacharse), cámara 1ª/3ª persona (V) con transición y colisión, bosque con ruinas y campamento (placeholders), detector (señal, sonido, indicador, modelo en mano), excavación (barra, animación, golpes), 12 hallazgos con rareza/peso/valor/descripción y nota real o ficción, inventario con peso y baúl, camioneta con combustible, mostrador (vender, restaurar, mejoras, permisos), museo, energía, hora y clima (con lluvia), mapa, HUD, **menú principal, pausa y controles**, guardado local con versión de formato, y post-proceso.

## Checklist de primera apertura (probar en este orden)
0. Menú **Hidden Treasures → Verificar proyecto** → OK. Test Runner: EditMode (34) y PlayMode (11).
1. Abrir el proyecto; sin errores rojos en la Console tras la importación.
2. `Main.unity` → Play. Aparece el menú principal con el campamento detrás. **Si no, anotar el primer error de la Console.**
3. Nueva partida → se ve el campamento al atardecer; WASD y mouse responden; V cambia de cámara sin cortes.
4. Mostrador (E) → comprar permiso → el HUD baja a $80.
5. Camioneta (E) → conducir (W/A/S/D) → bajar (E). Probar el surtidor.
6. Caminar hasta las ruinas, ver subir la señal, excavar con clic mantenido → aviso de hallazgo, peso en el HUD.
7. Llenar la mochila → la pieza siguiente queda en el suelo → recogerla con E tras dejar espacio.
8. Guardar en el baúl (E atrás del vehículo) → Tab muestra mochila y baúl.
9. Vender / restaurar / exhibir en el mostrador y en el museo. Comprar una mejora.
10. Dormir en la carpa (guarda). Esc → Menú principal → Continuar: el estado se restaura.
11. Dejar caer la energía a 0 (correr con la mochila llena): desmayo, objetos tirados, marcador en el mapa.
12. Menú de pausa: sensibilidad, controles, reanudar.
Para cada fallo: copiar el texto de la Console.

## Decisiones técnicas (no estaban en el prompt original)
- Sin Cinemachine: cámara propia (`CameraRig`).
- Input por código (sin `.inputactions`), mundo y UI por código (sin prefabs), una asmdef por sistema, `Bootstrap` como raíz de composición.
- Base de ajustes: `ProjectSettings` y URP Assets **oficiales** de la plantilla URP de Unity 6 (verificado que un proyecto generado por Unity nace con el Input System desactivado).
- Los shaders se eligen según el pipeline activo (URP Lit o Standard) para evitar magenta; el juego funciona sin URP.
- Mochila base 8 kg (un test mostró que 12 kg no obligaba a decidir).
- Desmayo: pérdida del 40% de la mochila (objetos en el suelo) y despertar en el campamento a las 7:00 — es una consecuencia del tira-y-afloja central; cambiar la regla es un solo método (`GameDirector.OnExhausted`).

## Pendiente (después de la primera apertura)
1. Corregir lo que aparezca al ejecutar por primera vez (prioridad máxima).
2. Reemplazar placeholders por assets reales (`ASSETS_TODO.md`): personaje Mixamo, vegetación, ruinas, detector, audio.
3. Tuning del vehículo, del balance y del rendimiento.
5. Más terrenos y regiones (fuera del MVP).

## Bosque salvaje, misiones y HUD de misión (última etapa)

- **Vegetación densa con GPU Instancing** (`Bootstrap/Art/InstancedVegetation.cs`): ~1500 árboles y ~45 700 plantas (pasto 26 000, pasto alto 9 000, helechos 6 500, arbustos 1 600, flores 2 600) en 36 capas instanciadas (`Graphics.RenderMeshInstanced`), con culling por frustum y distancia por trozos de 40 m. Muralla de árboles en el borde del mapa.
- **Niebla**: densidad base 0,0022 → 0,0085 (nublado 0,0115, lluvia 0,017, niebla 0,036): a ~150 m casi no se ve nada y el borde del mapa queda oculto.
- **Misiones**: `Core/QuestManager.cs` (lógica pura, 5 tests). Misión inicial "Encontrar 3 reliquias prehispánicas": 3 sitios nuevos (`relic_site_00..02`) repartidos al azar sobre el terreno (≥55 m del campamento y entre sí), reliquias `relic_*` en el catálogo, progreso en el guardado (`QuestId/QuestProgress/QuestCompleted`), recompensa $400 al cumplir.
- **HUD**: panel "OBJETIVO" bajo el panel de estado (arriba a la izquierda, sin solaparse), con "Reliquias: n/3". Las barras de energía y peso siguen donde estaban.
- **Verificado**: 45 tests .NET, compilación real en Unity 6 y renders del Editor (`tools/render_previews.sh`). **No verificado**: rendimiento en una GPU real (los renders usan llvmpipe), la misión jugándose de punta a punta (sin Play Mode), ni el HUD en pantalla.

## Libreta, estados del jugador, pisadas y detalles (última etapa)

- **Libreta de campo** (tecla **J**, también gamepad ↑; Tab sigue siendo el inventario): `UI/JournalView.cs` + `Core/FieldJournal.cs` (lógica pura, testeada). Lista las 3 reliquias y los 30 sitios del bosque; lo no desenterrado es "???" y lo desenterrado muestra nombre, rareza y descripción.
- **Excavación cinemática**: `Core/PlayerStateMachine.cs` (Idle, Crouching, Digging, PickingUp, Cleaning; 5 tests) + `Player/PlayerStateDriver.cs`. Al aparecer la pieza: recoger (0,9 s) → limpiar con cepillo (1,9 s), con movimiento bloqueado y barra de progreso en el HUD. Triggers del Animator listos (`Idle, Crouch, Dig, PickUp, Clean` + `IsMoving`, `Speed`): ver **ANIMACIONES.md**. Sin controller, poses procedurales (pala al cavar, cepillo al limpiar).
- **Cubo de reliquia + marca en el mapa**: `Bootstrap/Art/FindReveal.cs` (cubo dorado que sube del hoyo y gira mientras se recoge/limpia, destello y chispas con el color de la rareza); las reliquias ya halladas se marcan en el mapa ("Reliquia hallada").
- **Pisadas**: `Bootstrap/Art/FootprintSystem.cs` (quads con alfa, pool circular de 48, se desvanecen en ~28 s) sobre el camino de tierra, el campamento y las ruinas.
- **Ambiente**: `Survival/AmbientMotes.cs` (polen de día, luciérnagas al atardecer/noche) y filtrado anisotrópico forzado.
- **Verificado**: 51 tests .NET y compilación real en Unity 6. **No verificado** (sin Play Mode): la libreta en pantalla, las transiciones de estado jugando, el cubo, las pisadas, las luciérnagas y las poses del personaje.

## Audio, venta, guardado total y menú cinemático (última etapa)

- **Audio 100 % procedural** (`Core/ProceduralSounds.cs`, PCM por código, 8 tests; `Bootstrap/GameAudio.cs`): viento en los pinos (bucle sin saltos, más fuerte con lluvia), pájaros de día, grillos de noche, pisadas en tierra / pasto / piedra (según `WorldData.SurfaceAt`), pala, cepillo (3 pasadas al limpiar), "clink" de reliquia (más fuerte que el de un hallazgo común) y moneda al vender. El bip del detector sube de 520 a 1500 Hz y de 1,1 s a 0,07 s entre bips con la proximidad.
- **Venta**: el mostrador del campamento es el comerciante. Su pestaña "Vender" ahora muestra el valor total de los hallazgos, ordena por valor y se guarda sola tras cada venta (`TradingService.Sold`).
- **Guardado**: `SaveSystem/GameSaveCodec.cs` pasa todos los modelos puros a `SaveData` y de vuelta (dinero, mochila, baúl, mejoras, permisos, colección, energía, hora, clima, combustible, misión); el director agrega posiciones, sitios excavados (de ahí sale la libreta) y objetos en el suelo; se guarda además la semilla del mundo. Autoguardado: al vender, al dormir, al llegar al campamento (cada 60 s como mínimo) y con F5. Tests de ida y vuelta.
- **Menú principal cinemático** (`Bootstrap/MenuCinematic.cs`): la cámara orbita el campamento con fuego y bruma vivos (tiempo sin escala), HUD oculto, título a la izquierda, "Continuar" seleccionado si hay partida. El juego es de una sola escena (`Main.unity`), así que Continuar / Nueva partida no cambian de escena: cargan o reinician el estado. Etalonaje con más contraste y viñeta.
- **Verificado**: 62 tests .NET y compilación real en Unity 6; cuadros del recorrido del menú. **No verificado** (sin Play Mode): cómo suenan los sonidos, el menú en movimiento, el autoguardado jugando y la carga desde disco real.

## Presentación de hallazgos y timbre del detector (última etapa)

- **Tarjeta de hallazgo** (`UI/FindCardView.cs` + `Core/FindPresentation.cs`, 5 tests): al terminar de limpiar la pieza (tras recoger y cepillar) aparece arriba al centro con el color de la rareza, nombre, categoría, descripción, valor estimado y "¡NUEVA EN TU COLECCIÓN!" si corresponde. Dura más cuanto más rara (3,5 s común … 7,3 s leyendaria; las reliquias, +0,6 s).
- **Timbre del detector**: la frecuencia sigue indicando la proximidad; el *timbre* (armónicos) indica el tipo de objetivo cuando la señal es fuerte (≥ 0,55): opaco (común), con brillo (poco común), cristalino (raro o mejor) y con vibrato (reliquia).
- **Verificado**: 67 tests .NET y compilación real en Unity 6. **No verificado**: cómo se ve la tarjeta y cómo suenan los timbres (sin Play Mode).

## Rendimiento (RX 580) y calidad gráfica (última etapa)

- **Causa probable del lag**: relleno de pantalla del follaje con transparencia, 4 cascadas de sombras de 2048, sombras suaves, SSAO a resolución completa y profundidad de campo; el conteo de triángulos es moderado (~1 000 por conífera).
- **Cambios**: LOD de árboles (`TreeFactory` con parámetro `detail`: 992 → 424 y 750 → 350 triángulos; se usa a más de ~85 m), sombras de árboles solo hasta ~55 m, distancias de dibujo de plantas más cortas (la niebla las oculta), base de URP más barata (2 cascadas de 1024, 40 m, sombras suaves bajas, SSAO a media resolución), **DirectX 11** como API en Windows.
- **Niveles de calidad** (`Core/QualityProfile.cs`, `Bootstrap/GraphicsQuality.cs`, 7 tests): Bajo / Medio / Alto (escala de render, distancias, sombras, SSAO, profundidad de campo), botón en el menú, nivel inicial según la memoria de video y **bajada automática** si los FPS son bajos (`AdaptiveQuality`). Los ajustes de URP se restauran al salir.
- **No verificado**: el efecto real en FPS (sin GPU real); lo medirá tu PC con F1.

## Perfilado reproducible (autorizado tras la auditoría)

- `Bootstrap/PerfRecorder.cs` (F9 o argumento `-htperf`), `Core/PerfStats.cs` (4 tests), `Bootstrap/Editor/PerfBuild.cs` (menú para compilar el build de rendimiento). Mide por lugar y nivel de calidad: FPS promedio, 1 % low, p99, tirones, CPU/GPU ms (FrameTimingManager activado en Player Settings), draw calls, SetPass, triángulos y memoria. Salida: `perf_summary.md` + `perf_frames.csv` en la carpeta de datos del juego.
- **Verificado**: compila en Unity 6 y 77 tests .NET. **No verificado**: la medición en sí (necesita GPU real); la primera línea base real la dará la RX 580.

## Camioneta moderna y reflejos (tras la auditoría)

- **Silverado Trail Boss Z71** (CC-BY, gratis con atribución) reemplaza al Willys: pipeline `gltf-transform` + meshoptimizer (478 874 → ~234 000 triángulos, 192 → 51 mallas, texturas intactas), orientación por marcas `MARK_FRONT/REAR`, escala real (5,75 m), 4 ruedas (llanta + rin) agrupadas por esquina para la física, colisionador ajustado. Respaldo: Willys y luego camioneta procedural.
- **Reflejos del bosque** (`Bootstrap/ReflectionBaker.cs`): cubemap de 128 px que se refresca cada ~30 min de juego o con el clima (una cara por cuadro). Materiales corregidos por tipo (pintura con clearcoat simulado, llantas sin metal, cromados, vidrio transparente).
- **Herramienta**: `tools/gen_meta.py` ya no genera `.meta` genéricos para modelos/texturas (rompían la importación de glTF).
- **Verificado**: compila en Unity 6 y renders del Editor (frente, lado, atrás). **No verificado**: la conducción con el vehículo nuevo (ruedas, suspensión, tamaño del colisionador), el rendimiento y los reflejos en juego.
