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
| Hallazgos y objetos | Parcial | 12+3 objetos con rareza, peso, valor y condición (`Condition`). **Faltan**: antigüedad, procedencia, historia, autenticidad, identificación. |
| Restauración / venta / colección / museo | Implementado, no probado | Restaurar por costo, vender con total, exhibir. **Falta**: limpieza e identificación como actividades; valor histórico. |
| Inventario con peso, mochila y baúl | Implementado, no probado | Tests puros OK (6). Falta jugarlo; el peso no tiene "espacio" ni equipamiento. |
| Vehículo (conducción, combustible, baúl, luces) | Implementado, no probado | Silverado real integrada. **Faltan**: daños/desgaste, mejoras, consumo según terreno; la conducción nunca se manejó. |
| Supervivencia | Parcial | Solo **energía** (con peso, clima y noche). **Faltan**: hambre, sed, temperatura, lesiones, hidratación, protección ambiental. |
| Clima y ciclo día/noche | Parcial | Claro, nublado, lluvia, niebla; sin tormenta, nieve, viento ni efecto en transitabilidad. |
| Economía y mejoras | Parcial | 6 mejoras, permisos, combustible. **Faltan**: gastos de expedición (provisiones, reparaciones), precios que obliguen a decidir. |
| Misiones / contratos | Parcial | `QuestManager`: una misión (3 reliquias). **Faltan**: contratos (objeto, región, recompensa, condiciones), expediciones especiales, misterios narrativos. |
| Regiones / biomas | Parcial | 4 perfiles (`BiomeProfile`) con relieve, suelo, vegetación, atmósfera y permiso propios y **viaje** entre ellos (sin jugar). Sin mecánicas ni peligros propios por región. |
| Preparación de expedición | **Pendiente** | No hay selección de destino con información, ni equipamiento exigido por región, ni "qué necesito para salir". |
| Equipamiento (linterna, GPS, cuerda, botiquín, ropa…) | **Pendiente** | No existe sistema de equipo/loadout. |
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
