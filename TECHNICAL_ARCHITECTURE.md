# Arquitectura técnica

## Estructura actual (se mantiene)
Una asmdef por sistema bajo el namespace `HuesoOxido.*`; `Bootstrap` es la raíz de composición (`GameBootstrap` crea el mundo, los sistemas y la UI en tiempo de ejecución desde una única escena `Main.unity`). La lógica pura (sin `UnityEngine`) vive en `Core`, `Economy`, `Inventory`, `Survival` (modelos), `SaveSystem`, y se prueba con .NET (`tools/validate.sh`); lo dependiente del motor queda en capas finas.

| Módulo | Contenido |
|---|---|
| `Core` | Contratos, ítems, `BiomeProfile`, `HeightField`, `QuestManager`, `FieldJournal`, `ArtLibrary`, materiales |
| `Bootstrap` | Composición, mundo (`WorldBuilder`, vegetación, ruinas, campamento), director de partida |
| `Survival` | Energía, reloj, clima, día/noche, atmósfera |
| `Inventory`, `Economy`, `Collection` | Peso/baúl, permisos/mejoras/venta, museo |
| `Detection`, `Digging` | Detector y excavación |
| `Vehicle`, `Player`, `Camera`, `Interaction`, `UI`, `SaveSystem` | Como indica el nombre |
| `tools/art` | Pipeline de arte (Node + Python): modelos Sketchfab/Poly Haven → LOD + texturas |

## Principios para integrar la visión sin duplicar
1. **Región = `BiomeProfile` ampliado** (no un sistema nuevo): se le agregan reglas (peligros, clima permitido, equipo requerido, contratos, hallazgos). El viaje y el permiso ya existen.
2. **Necesidades = generalización de `EnergyModel`**: una pequeña interfaz `ISurvivalStat` (valor, drenaje por contexto, umbral, efecto) con instancias para energía, sed, hambre, temperatura. Se reutiliza `EnergyContext`.
3. **Equipamiento = servicio puro** `EquipmentService` (loadout, requisitos, utilidad) con `ItemDefinition` ampliado (`Tipo`, `Peso`, `Utilidad`). El inventario existente lo sirve a todas las regiones.
4. **Hallazgos con contexto**: extender `ItemInstance` con metadatos opcionales (antigüedad, procedencia, autenticidad, estado de limpieza). Migración de `SaveData` por versión.
5. **Contratos = `QuestManager` generalizado** (definición con región, condiciones, recompensa, riesgos opcionales).
6. **Expedición = estado explícito** (`ExpeditionState`: destino, preparación, en curso, regreso, resultado) coordinado por `GameDirector`; evita lógica dispersa.
7. **Buceo = módulo separado** (`HuesoOxido.Underwater`) que reutiliza inventario, equipo, guardado y economía.
8. **Arte**: todo modelo entra por `tools/art` → `Resources/Art/World/<nombre>/` → `ArtLibrary` → `ArtScatter`/`InstancedVegetation`. No se agregan caminos paralelos.

## Riesgos técnicos
- **Sin ejecución real previa**: PlayMode (11 tests) nunca corrió; riesgo de fallos de arranque/UI/física.
- **Rendimiento sin medir** en RX 580: ~50 k instancias de vegetación, modelos de 3–7 k tris con LOD de 2 niveles, texturas 2K; sin occlusion culling configurado; VRAM sin presupuesto medido.
- **Escena única reconstruida por viaje** (`LoadScene` + regenerar mundo): tiempos de carga sin medir; ok para ahora.
- **Datos por código**: el contenido (ítems, regiones, contratos) está en C#; crecerá mal. Conviene migrar a `ScriptableObject`/JSON antes de multiplicar regiones.
- **Repo**: ≈140 MB de arte; mantener LFS y limpiar candidatos descartados.
- **Licencias**: CC-BY exige atribución visible (hecho en créditos del juego); mantener al día.
- **Compilación de la lógica pura vs Unity**: dos entornos (stubs .NET y Unity real); cualquier cambio debe pasar ambos.

## Verificación (vocabulario obligatorio)
Código implementado · compilación verificada (.NET y Unity) · tests ejecutados (EditMode .NET; PlayMode pendiente) · render generado (software) · gameplay probado (**pendiente**) · rendimiento medido (**pendiente**, F9 en hardware real).
