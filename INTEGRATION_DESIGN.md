# Diseño de integración de los sistemas de expedición

**Estado: diseño. Nada de esto está conectado al juego.** Los cinco modelos (necesidades, equipamiento, preparación, hallazgos, contratos) más dos de apoyo (efectos del equipo, sesión de limpieza) son lógica pura con tests en .NET y compilación verificada en Unity. No hay UI, ni eventos del juego, ni guardado activo en `GameDirector`. Se conectan **solo después** de la prueba integral en PC (PlayMode, F9, rendimiento).

Archivos: `Survival/NeedsModel.cs`, `Survival/Provisions.cs`, `Core/RescueService.cs`, `Core/Equipment.cs`, `Core/EquipmentEffects.cs`, `Core/ExpeditionPlanner.cs`, `Core/FindMetadata.cs`, `Core/CleaningSession.cs`, `Core/ContractBoard.cs`, `SaveSystem/*`.

## 1. Reglas de integración
1. **Un sistema por vez**, cada uno detrás de un interruptor (`GameplayFlags`, una clase estática con booleanos, apagados por defecto → el juego sigue siendo el que ya se probó). Si algo falla en PC se apaga el interruptor, no se revierte código.
2. **El director cablea, los modelos no se conocen entre sí.** Hoy `FindWorkbook` no sabe de `ContractBoard`; solo el `GameDirector` los une. Se mantiene.
3. **Cada paso deja algo jugable y verificable**, con criterio de salida medible en PC.
4. **Sin cambios de formato que rompan guardados** (ver §7).
5. **Nada nuevo en el camino crítico del bucle actual** (detector → excavar → recoger → vender) hasta que ese bucle esté probado.

## 2. Mapa de dependencias
```
Reloj (GameClock) ───────────────► NeedsModel ◄──── EquipmentEffects ◄──── Loadout ◄──── EquipmentShop (nuevo, Economy)
Clima (WeatherModel) ────────────►     │                    │                  │
Región (BiomeProfile: Temperature) ──► ExpeditionPlanner ◄──┘                  │
                                         │                                       │
                                         ▼                                       │
                                   ExpeditionLog ──► resumen ──► Wallet/Reputación
                                                                                 
Excavación (DigController) ──► ItemInstance ──► FindWorkbook ◄── CleaningSession ◄── EquipmentEffects.CleanQuality
                                                    │                 │
                                                    │                 └─► Clue/Appraisal ──► UI de la mesa
                                                    ▼
                                              ContractBoard ◄── Deliver(item) / ReportSite(site)
                                                    │
                                                    └──► Wallet (recompensa) + Reputación ──► desbloquea contratos/equipo
SaveData ◄── Capture/Restore (GameSaveCodec) de: Needs, Loadout, FindWorkbook, ContractBoard
```
Reglas de dependencia (asmdef): los modelos viven en `Core`/`Survival`/`Economy` (puros). `Bootstrap` y `UI` los usan; ellos no usan `UI`.

## 3. Eventos (los que hay y los que se agregan al cablear)
| Emisor | Evento | Payload | Consumidores |
|---|---|---|---|
| `NeedsModel` | `Changed` | — | HUD (solo si cambió un aviso) |
| `NeedsModel` | `Collapsed` | — | `GameDirector.OnExhausted` (mismo desmayo que hoy; pérdida = `EquipmentEffects.CollapseLossFraction`) |
| `Loadout` | `Changed` | — | UI de preparación, peso de carga, `EquipmentEffects` (recalcular) |
| Radio (nuevo, `GameDirector`) | `RescueRequested` / `RescueArrived` | `RescueQuote` | HUD, reloj (pasa el ETA), `Wallet` (tarifa), recuperación de objetos caídos, guardado |
| `ExpeditionLog` | `PhaseChanged` | fase | HUD, guardado automático al salir/volver, `ContractBoard.ExpireOverdue` al volver |
| `FindWorkbook` | `Changed` | — | UI de la mesa, guardado |
| `CleaningSession` | `ClueRevealed` | `Clue` | UI de la mesa, audio |
| `ContractBoard` | `Completed` | definición | `GameDirector` (paga y avisa), reputación |
| `ContractBoard` | `Changed` | — | libreta/HUD de contratos |
| `DigController.FindUnearthed` (existe) | — | sitio, pieza | **nuevo**: `ExpeditionLog.RecordFind`, `FindWorkbook.State` (crea suciedad) |
| `GameDirector.OnSold` (existe) | — | total, cantidad | `ExpeditionLog`, reputación (opcional) |
| `Travel`/`Depart` (nuevo) | — | región | `ExpeditionPlanner.Evaluate` antes de permitir salir |
Eventos nuevos a crear: `ExpeditionLog.Summary` (al volver) y un evento `SiteReached(siteId)` en el sensor de interacción para `ContractBoard.ReportSite`.

## 4. Datos compartidos y fuente de verdad
| Dato | Dueño (única fuente) | Lectores |
|---|---|---|
| Horas de juego | `GameClock` (1 h = 50 s reales) | `NeedsModel` (vía `GameHoursFromSeconds`) |
| Clima/temperatura ambiente | `WeatherModel` + `BiomeProfile.Temperature` | `NeedsContext.AmbientC`, `Wet` |
| Equipo y provisiones | `Loadout` | `EquipmentEffects`, planner, peso, UI |
| Peso transportado | `InventoryModel` + `Loadout.EquipmentWeightKg()` | `EnergyModel`, `NeedsContext.LoadFraction` |
| Estado de cada pieza | `FindWorkbook` (por uid) | tienda, contratos, museo, UI |
| Condición de la pieza | `ItemInstance.Condition` (ya existe) | limpieza (puede bajarla), venta, restauración |
| Reputación | `ContractBoard.Reputation` | desbloqueo de contratos, regiones y equipo |
| Fase de la expedición | `ExpeditionLog` | HUD, director, guardado |

## 5. Orden de integración (cada paso: entrada → trabajo → salida verificable)
**Paso 0 — Prueba integral en PC (bloqueante).** Jugar el bucle actual, F9, PlayMode. Salida: lista de defectos corregida y FPS medidos.

**Paso 1 — Necesidades (invisible al principio).** Entrada: paso 0. Trabajo: un `NeedsComponent` que cada segundo llama `Tick(GameHoursFromSeconds(dt), contexto)` con el contexto del jugador; consumo automático con `Provisions.AutoConsume`; efectos solo sobre `EnergyDrainMultiplier` y `SpeedMultiplier`; **sin HUD nuevo** salvo un icono al cruzar un umbral; colapso → `OnExhausted`. Salida en PC: sin provisiones, aviso suave a los ~20 min y colapso no antes de ~28 min; con 3 L y 2 raciones, 75 min de exploración seguida sin avisos críticos (los tests ya fijan estos números; la prueba en PC confirma que se *sienten* bien).

**Paso 2 — Equipo y preparación.** Trabajo: tienda del mostrador (pestaña "Equipo") sobre `Loadout`; pantalla de preparación en la carpa; `EquipmentEffects` conectados de a uno (primero ropa → `NeedsContext`, linterna → visibilidad nocturna, pala/pico → velocidad de excavación según suelo). Salida: salir de noche sin linterna se bloquea con explicación; con ropa térmica en el Pinar Nevado el frío no colapsa.

**Paso 3 — Mesa de limpieza e identificación.** Trabajo: pestaña/estación en el campamento sobre `CleaningSession` + `Appraisal`; la excavación empieza a registrar suciedad. Salida: una pieza pasa de sucia → pistas → opinión → certeza, y su precio cambia de forma entendible.

**Paso 4 — Contratos.** Trabajo: tablón en el mostrador; entrega desde el inventario; `ReportSite` desde el sensor de interacción; libreta con contratos activos. Salida: se completa una cadena Reconocer → Diario sin herramientas de desarrollo.

**Paso 5 — Cierre del ciclo.** `ExpeditionLog` al viajar/volver, resumen de expedición, guardado automático, reputación como desbloqueo. Salida: una expedición completa de 30–60 min.

Cada paso tiene su prueba PlayMode y su entrada en `PRUEBAS.md`.

## 6. Diseño por área (los cinco cuidados)

### 6.1 Necesidades: planificar, no una cuenta regresiva
- **El jugador no administra barras.** El HUD no muestra sed/hambre/temperatura de forma permanente: un icono suave aparece solo al cruzar un aviso y se va al resolverlo.
- **No hay reloj que apure.** La autonomía se **compra con peso y preparación**, no con apuro: cada litro de agua suma ~22 min de exploración y pesa 1 kg (capacidad máxima 4 L); las raciones sostienen mucho más tiempo que el agua. Con 3 L y 2 raciones se explora **más de 90 min seguidos** sin mirar nada. El limitante real es cuánto peso decidís cargar y las **fuentes de agua**: el agua se rellena gratis en el campamento y en manantiales/arroyos marcados en el mapa (a colocar en el bosque al integrar).
- **Consumo automático** (activado por defecto): si llevás agua o comida, bebés/comés solos al cruzar el aviso. Quien se preparó no atiende nada; quien no llevó provisiones recibe el aviso y debe buscar una fuente o volver.
- **Sed y hambre "normales" solo avisan**: las penalizaciones (excavar más lento, gastar más energía, caminar más lento) empiezan en el estado *crítico*; el colapso (= el desmayo actual, sin muerte) llega al final. Así hay margen amplio para reaccionar.
- **Autonomía visible en la preparación** (`Provisions.Estimate`): "con lo que llevás alcanzás para ~95 min de agua / ~140 min de comida en este clima". Sirve para planificar, no para presionar durante la expedición.
- **Calibración** contra el reloj real (1 h de juego = 50 s): sin provisiones ni fuentes, aviso suave a los ~20 min y colapso a los ~31; el clima (calor, frío) y el esfuerzo (correr, excavar) la modifican, y la ropa y el descanso la mejoran.
- Riesgo de diseño: que moleste o que se sienta como temporizador. Medidas en PC: acciones del jugador por necesidades en 30 min (objetivo ≤ 3), y si alguna vez se sintió "apurado" por sed/hambre.

### 6.2 Cada pieza de equipo cambia una decisión
| Pieza | Efecto concreto (`EquipmentEffects`) | Decisión que cambia |
|---|---|---|
| Pala / Pico | Velocidad según suelo (`SoilKind`) | Qué llevar según dónde excavar (pala en tierra blanda, pico en suelo duro/raíces/piedra) |
| Linterna | Visibilidad nocturna 7 → 24 m | Si es seguro explorar de noche y bajo tierra |
| Brújula / GPS | Orientación; GPS marca posición y vehículo en el mapa | Si se puede internar en zonas sin referencias o con niebla; exigida por contratos de reconocimiento |
| Cuerda | Pendiente máxima 32° → 42° | Qué laderas y desniveles son transitables |
| Botiquín | Trata lesiones | Si se arriesga en terreno de caídas |
| Radio | **Pedir asistencia** (`RescueService`): evacuación, recuperación de lo caído, coordenadas | Cuánto se arriesga lejos de la base y en regiones remotas; con GPS la ayuda llega antes y recupera todo |
| Machete | Abre vegetación densa | Rutas alternativas en la selva |
| Repelente | Protección contra insectos | Entrar a zonas con plaga |
| Cámara | Permite documentar | Contratos de documentación, reputación |
| Cepillo | Calidad de limpieza 0,2 → 0,7 | Cuánto se arriesga la pieza al limpiar |
| Ropa térmica / ligera | Abrigo vs. protección del calor | Qué región se puede hacer sin colapsar (se nota en `NeedsModel`) |
Regla: **ninguna pieza es "+5 %"**; todas habilitan, bloquean o cambian un riesgo. Peso y costo las equilibran (se lleva lo necesario, no todo).

#### La radio (`RescueService`)
La radio **no** reduce castigos: abre una *opción* con costo y condiciones.
- **Sin radio no hay llamada.** Con radio hace falta **cobertura**: el bosque la tiene (0,9), el Pinar Nevado (0,5) y el Pantano (0,4) son más difíciles, y lejos de la base (>400 m) cae. En una zona sin señal no se puede pedir ayuda: la preparación lo advierte ("Radio recomendada: cobertura baja").
- **Coordenadas**: radio sola = ubicación *aproximada* (la ayuda tarda ~1,8× y recupera la mitad de lo caído); radio + GPS = ubicación *precisa* (llega antes y recupera todo). El GPS sin radio no transmite nada.
- **Clima**: una tormenta fuerte (>0,75) impide que salga el rescate hasta que amaine; la lluvia lo retrasa.
- **Cuándo**: *antes* de colapsar (varado, sin combustible, herido) evita el desmayo y la pérdida de carga; *después* (con objetos caídos) la ayuda trae lo caído según la precisión de las coordenadas.
- **Costo**: tarifa 40 + distancia/25 (+30 de noche) y el tiempo de espera (ETA en horas de juego). Es una decisión económica y de tiempo, no un seguro gratis.
- La pérdida base al desmayarse (40 %) es igual para todos: el equipo no la "compra".

### 6.3 Limpiar e identificar como actividad
- **Mesa de limpieza**: la pieza se ve dividida en 6 sectores cubiertos de tierra; el jugador elige dónde cepillar y con qué presión.
- **Tensión real**: más presión = más rápido pero daña si supera el límite seguro (depende de la herramienta y de lo frágil que esté la pieza). El límite se muestra con un indicador, no se oculta.
- **Cada sector puede esconder una pista** (inscripción, marcas de herramienta, pátina, material, desgaste, estilo) con tono *coherente*, *sospechoso* o *neutro*. Las falsificaciones suelen delatarse con 2 pistas sospechosas; las piezas auténticas pueden dar falsa alarma (15 %). La información reduce la duda, **no la elimina** (precisión medida en tests: ~80–99 %).
- **Opinión, no certeza** (`Appraisal`): el explorador sabe "probablemente auténtica (70 %)"; la **certeza** llega con una referencia (tener una pieza igual en la colección, un documento) o con el laboratorio del campamento (instalación a desbloquear). Decisión: vender ya con descuento por incertidumbre, o invertir tiempo/dinero en confirmar.
- **Recompensa más allá del dinero**: la historia de la pieza (`FindProfile.Story/Origin/AgeYears`) se revela al identificarla y alimenta el museo y los misterios.
- Sin minijuego de reflejos: es lectura, paciencia y riesgo. Duración objetivo: 30–90 s por pieza relevante; las piezas comunes pueden limpiarse "en lote" con calidad fija para no cansar.
- Medida de control en PC: tiempo medio por pieza y si el jugador omite la mesa (señal de que es administrativa).

### 6.4 Contratos variados y progresión coherente
- **Cuatro tipos** (ya implementados y probados): *Recuperar* (tipo o rareza), *Colección* (piezas distintas), *Documentar* (exige cámara), *Reconocer* (exige orientarse con brújula/GPS).
- **Cadenas** (`PrerequisiteId`): Reconocer las ruinas → El diario de Valdés (cierra una historia).
- **Reputación en 4 niveles** (Aprendiz 0, Explorador 100, Veterano 300, Leyenda 700): desbloquea contratos más ricos, y más adelante regiones y equipo.
- **Recompensas crecientes** (probado: el promedio de los contratos de nivel ≥ 100 supera al de nivel 0).
- **Plazos sin castigo económico**: vencer cuesta 10 de reputación, nunca dinero.
- Los contratos empujan a **usar el equipo y a explorar** (cámara, GPS, piezas limpias e identificadas), no solo a acumular objetos.

## 7. Compatibilidad de guardados (política)
1. **Solo se agregan campos**; nunca se quitan ni se renombran. Todo bloque nuevo es opcional con bandera `Has`.
2. **Un guardado anterior carga con valores iniciales** de los sistemas nuevos (test `OldSaveWithoutExpeditionDataRestoresStarterValues`); un guardado nuevo cargado sin esos sistemas conectados no falla (test `BundlesWithoutExpeditionSystemsStillWork`).
3. **Migración**: `SaveMigrator.Migrate` rellena los bloques que falten. Un cambio incompatible exigiría subir `SaveData.CurrentVersion` y escribir su migración + test.
4. **Datos desconocidos se ignoran**: ids de equipo o contratos que ya no existen se descartan al cargar (tests de `Loadout`/`ContractBoard`).
5. **Respaldo**: `SaveFileStore` ya conserva una copia anterior; antes de la primera integración se documenta en `PRUEBAS.md` cómo restaurarla.
6. **Verificación en PC**: cargar un guardado hecho con la versión actual de `stable` antes de integrar y comprobar que continúa.

## 8. Pantallas (esquema)
**Preparación (carpa/mostrador)**
```
EXPEDICIÓN → Bosque de las Ruinas        Salida: 10:30   Clima probable: nublado, 12 °C
REQUISITOS         [✓] Detector   [✓] Pala/Pico   [✓] Combustible 78 %   [✓] Agua 1,0 L
RECOMENDADO        [ ] Linterna (por si anochece)   [ ] Brújula o GPS   [ ] Ración
EQUIPO (peso 6,2 / 8 kg)   [Cambiar ropa] [Cambiar herramienta] [Tienda]
RIESGO ESTIMADO: BAJO        [ SALIR ]  (deshabilitado si falta un requisito, con el motivo)
```
**Mesa de limpieza**
```
Pieza: Denario romano   Condición 70 %   Suciedad ▓▓▓░░░
[sector 1][sector 2][sector 3]        Presión: ──●──  (límite seguro ▲)
[sector 4][sector 5][sector 6]        Pistas: ✓ Pátina profunda  ? Inscripción gastada  ✗ Letras demasiado nítidas
Opinión: probablemente FALSA (68 %)   [ Confirmar en el laboratorio ] [ Vender igual ] [ Guardar ]
```

## 9. Qué se verificará en PC al integrar
Cada paso: compila, tests PlayMode del paso, jugarlo 30 min, anotar (a) si algo molesta, (b) FPS con F9, (c) que un guardado anterior sigue cargando. Métricas de diseño: acciones por necesidades ≤ 3 en 30 min; uso real de las piezas de equipo; tiempo y omisión de la mesa; contratos completados por expedición.
