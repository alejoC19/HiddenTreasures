# Sistemas de juego: estado real y plan

Auditoría hecha sobre el repositorio (rama `claude/focused-darwin-ps2pj7`, ~12 000 líneas de C# en 14 módulos, 85 tests EditMode, 11 PlayMode escritos). Clasificación honesta:

- **Completo y probado**: lógica con tests que pasan *y* comprobada en juego.
- **Implementado, no probado**: existe y compila; solo hay tests puros o renders, nunca se jugó.
- **Parcial**: cubre una parte de lo que pide la visión.
- **Pendiente**: no existe.
- **Rediseño**: existe pero no sirve para la visión tal como está.

**Hoy no hay ningún sistema "Completo y probado" en el sentido estricto**: el juego nunca se ejecutó en Play Mode en un entorno con GPU verificado por mí; los tests PlayMode siguen sin ejecutarse. Lo que sí está probado es la lógica pura (tests .NET) y la compilación/render en Unity real.

| Sistema | Clasificación | Evidencia / qué falta |
|---|---|---|
| Menú principal, pausa, controles, créditos | Implementado, no probado | Código y compilación. Falta verlo en pantalla. |
| Personaje y cámara 1.ª/3.ª | Implementado, no probado | Movimiento, estados y poses nunca jugados. |
| Detector (señal por distancia/profundidad, audio) | Parcial | Intensidad por distancia y profundidad y timbre por rareza. **Faltan**: modos, interferencias, señales falsas, materiales, detectores especializados. Modelo 3D real integrado (orientación sin probar en movimiento). |
| Excavación | Parcial | Barra, golpes, recoger y limpiar con estados. **Faltan**: profundidad real, tipo de suelo, herramientas distintas, riesgo de dañar. |
| Hallazgos y objetos | Parcial | 12+3 objetos con rareza, peso, valor y condición. `FindProfiles` (antigüedad, procedencia, historia, riesgo de falsificación) y `FindWorkbook` (limpieza, identificación, autenticidad estable, valoración) existen como modelo puro y probado, **sin conectar** a la UI ni a la excavación. |
| Restauración / venta / colección / museo | Implementado, no probado | Restaurar por costo, vender con total, exhibir. **Falta**: limpieza e identificación como actividades; valor histórico. |
| Inventario con peso, mochila y baúl | Implementado, no probado | Tests puros OK (6). Falta jugarlo; el peso no tiene "espacio" ni equipamiento. |
| Vehículo (conducción, combustible, baúl, luces) | Implementado, no probado | Silverado real integrada. **Faltan**: daños/desgaste, mejoras, consumo según terreno; la conducción nunca se manejó. |
| Supervivencia | Parcial | En juego, solo **energía**. `NeedsModel` (sed, hambre, temperatura con inercia y efectos graduales) existe como modelo puro y probado, **sin conectar**. Faltan lesiones y la integración. |
| Clima y ciclo día/noche | Parcial | Claro, nublado, lluvia, niebla; sin tormenta, nieve, viento ni efecto en transitabilidad. |
| Economía y mejoras | Parcial | 6 mejoras, permisos, combustible. **Faltan**: gastos de expedición (provisiones, reparaciones), precios que obliguen a decidir. |
| Misiones / contratos | Parcial | En juego, `QuestManager` (3 reliquias). `ContractBoard` (6 contratos, reputación en 4 niveles, plazos sin castigo económico) existe como modelo puro y probado, **sin conectar**. Faltan expediciones especiales y misterios. |
| Regiones / biomas | Parcial | 4 perfiles (`BiomeProfile`) con relieve, suelo, vegetación, atmósfera y permiso propios y **viaje** entre ellos (sin jugar). Sin mecánicas ni peligros propios por región. |
| Preparación de expedición | **Modelo puro hecho, sin conectar** | `ExpeditionPlanner` deriva requisitos de cada región desde su clima; `ExpeditionLog` es la máquina de estados. **Falta la pantalla y el cableado al juego.** |
| Equipamiento (linterna, GPS, cuerda, botiquín, ropa…) | **Modelo puro hecho, sin conectar** | `Loadout` + `DefaultEquipment` (15 piezas, slots exclusivos, peso, provisiones). Falta tienda/UI y que cada pieza tenga efecto en el juego. |
| Campamento evolutivo | Parcial | Carpa, mostrador, museo, surtidor reales. Sin instalaciones desbloqueables. |
| Guardado | Implementado, no probado | Codec puro con tests de ida y vuelta; versión de formato; incluye bioma. |
| Audio | Implementado, no probado | 100 % procedural (viento, pájaros, pisadas, detector). Sin música ni ambientes por región. |
| HUD, mapa, libreta, inventario, tienda | Implementado, no probado | Existen; layout nunca visto jugando. |
| Arte del mundo (árboles, rocas, ruinas, sotobosque, campamento, camioneta, detector) | Implementado, render verificado | Modelos reales optimizados; renders en Unity por software. Rendimiento sin medir. |
| Amenazas (fauna, humanos, sobrenaturales, técnicas) | **Pendiente** | No hay ninguna. |
| Buceo / submarino | **Pendiente** | Módulo especializado futuro. |

## Qué se reutiliza (no se reescribe)
`InventoryModel` (peso) → todas las regiones y el buceo. `ItemInstance/IItemInfo` → se extiende con metadatos. `QuestManager` → base de contratos. `EnergyModel` + `EnergyContext` → patrón para el resto de necesidades. `BiomeProfile` → base de región. `GameSaveCodec/SaveData` → con migración de versión. `UpgradeService`, `PermitService`, `TradingService`, `FuelTank`. `InstancedVegetation`, `ArtLibrary/ArtScatter` y `tools/art` → todo el arte.

## Sistemas nuevos (en orden de necesidad)
1. **Equipamiento y requisitos de expedición** (qué lleva el jugador, qué exige cada región).
2. **Necesidades de supervivencia** generalizando `EnergyModel` (sed, hambre, temperatura) con consecuencias claras y sin tareas molestas.
3. **Metadatos de hallazgos** (antigüedad, procedencia, historia, autenticidad) + **limpieza/identificación**.
4. **Contratos** sobre `QuestManager`.
5. **Expedición como estado**: destino, preparación, regreso, resultado.
6. Más adelante: amenazas por región, buceo, campamento evolutivo.

## Modelos puros nuevos (esta etapa): qué son y qué NO son
Se construyeron **sin depender de que el juego haya sido probado en PC**: son lógica pura (sin motor), con 57 tests nuevos que pasan en .NET (142 en total), compilados también en Unity real, y con guardado compatible hacia atrás (un guardado anterior carga con los valores iniciales).

| Modelo | Archivo | Qué resuelve |
|---|---|---|
| Necesidades | `Survival/NeedsModel.cs` | Sed, hambre y temperatura corporal en horas de juego; efectos graduales (velocidad, excavación, drenaje de energía); colapso = el desmayo actual, no muerte. Calibrado con tests: una caminata de 4 h en el bosque no exige microgestión. |
| Equipamiento | `Core/Equipment.cs` | 15 piezas, slots exclusivos (un detector, pala o pico, una ropa), peso, agua y raciones. |
| Preparación | `Core/ExpeditionPlanner.cs` | Requisitos y recomendaciones derivados del perfil de la región; riesgo estimado; `ExpeditionLog` (preparar → salir → volver → resumen). |
| Hallazgos | `Core/FindMetadata.cs` | Suciedad, limpieza con riesgo según herramienta, identificación, autenticidad decidida una vez por pieza, valor según limpieza/identificación/antigüedad. |
| Contratos | `Core/ContractBoard.cs` | Encargos que exigen piezas limpias, identificadas, auténticas y en buen estado; reputación y niveles; vencimiento sin penalidad de dinero. |
| Guardado | `SaveSystem/*` | Bloques opcionales (`Needs`, `Loadout`, `FindStates`, `Contracts`) con migración. |

**Detalle del cableado, eventos, datos compartidos y orden de integración: [`INTEGRATION_DESIGN.md`](INTEGRATION_DESIGN.md).** Se sumaron además `Core/EquipmentEffects.cs` (qué cambia cada pieza) y `Core/CleaningSession.cs` (limpieza con pistas y opinión), y los contratos tienen 4 tipos y cadenas.

**No están conectados al juego.** Nada de esto se ve ni se juega todavía. Cuando se pruebe el juego en PC, la integración será por pasos pequeños: (1) `NeedsModel` con el reloj y el desmayo, (2) pantalla de preparación en el mostrador, (3) limpieza/identificación en la tienda, (4) tablón de contratos, (5) efectos del equipo (linterna, GPS, ropa).
