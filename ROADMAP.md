# Roadmap

Realista, por etapas con dependencias. Cada etapa deja una mejora **concreta y verificable**. Las regiones nuevas no empiezan hasta cerrar la Fase 1.

## Estado de partida
Mundo y arte del bosque a buen nivel visual (render verificado, sin medir en GPU real); lógica pura con 85 tests; **gameplay sin probar de punta a punta**; supervivencia solo con energía; sin preparación ni equipo ni contratos.

## Fase 0 — Auditoría (hecha en esta entrega)
Informe en [`GAMEPLAY_SYSTEMS.md`](GAMEPLAY_SYSTEMS.md), arquitectura en [`TECHNICAL_ARCHITECTURE.md`](TECHNICAL_ARCHITECTURE.md).

## Fase 1 — Primera expedición completa (el bosque) — **siguiente**
Dependencia previa y bloqueante: **verificar en una PC real lo que ya existe** (jugar, F9, capturas, ejecutar los 11 tests PlayMode).

1. **1.0 Verificación real**: jugar el bucle actual, corregir errores de ejecución, medir rendimiento en la RX 580, ajustar distancias/LOD/cantidades con datos.
2. **1.1 Equipamiento mínimo funcional**: loadout (linterna, GPS/brújula, botiquín, cuerda…), cada pieza con utilidad real; el vehículo y el baúl amplían capacidad.
3. **1.2 Preparación**: pantalla de expedición (destino, riesgos conocidos, requisitos, combustible, provisiones) antes de salir.
4. **1.3 Supervivencia conectada**: sed, hambre, temperatura, peso y fatiga con consecuencias claras (sobre `EnergyModel`), sin microgestión molesta.
5. **1.4 Clima con consecuencias**: tormenta que cierra zonas o reduce visibilidad/transitabilidad.
6. **1.5 Hallazgos con contexto**: metadatos (antigüedad, procedencia, historia, autenticidad) + limpiar/identificar/restaurar como actividad.
7. **1.6 Contratos** (sobre `QuestManager`) y reputación inicial.
8. **1.7 Cierre del ciclo**: regreso, resultado de la expedición, venta/colección, mejora; guardado con migración.
9. **1.8 Arte restante del bosque**: arbustos reales, franjas del suelo, surtidor verificado, vehículo (daños/suciedad).

Criterio de salida: una persona sin herramientas de desarrollo completa una expedición satisfactoria en 30–60 min, con FPS medidos.

### Avance de la Fase 1 en paralelo a la verificación (modelos puros, sin conectar)
Hechos y probados en lógica: necesidades (1.3), equipamiento (1.1), preparación y registro de expedición (1.2), metadatos/limpieza/identificación (1.5), contratos (1.6) y su guardado. **Pendiente de la verificación real y de la integración**: todo lo que se vea o se juegue. Orden y criterios de cableado: ver [`INTEGRATION_DESIGN.md`](INTEGRATION_DESIGN.md) (necesidades → equipo y preparación → mesa de limpieza → contratos → cierre del ciclo), siempre detrás de interruptores y después de la prueba integral en PC.

## Fase 2 — Consolidación de supervivencia, progresión y campamento
Lesiones, protección ambiental, economía con gastos reales, mejoras de vehículo/campamento, instalaciones desbloqueables, contratos variados.

## Fase 3 — Región de misterio (Monasterio de las Sombras)
Diseñar y validar una experiencia acotada: historia, interiores, linterna/oscuridad, fenómenos con reglas, descubrimientos únicos.

## Fase 4 — Selva y amenazas humanas
## Fase 5 — Buceo y recuperación submarina (módulo especializado)
## Fase 6 — Expansión (Cordillera, nuevas regiones, vehículos, historias)
## Fase 7 — Pulido y comercialización
Optimización, interfaz, audio/música, accesibilidad, guardado robusto, tutorial, opciones gráficas, presentación.

## Riesgos que condicionan el calendario
1. Nunca se ejecutó el juego en una GPU real: puede haber errores de ejecución y problemas de rendimiento ocultos. 2. Arte gratuito: llega a "convincente", no a escaneos de gama alta. 3. Peso del repositorio (≈140 MB de arte nuevo, texturas por LFS). 4. Supervivencia: el riesgo de diseño es la microgestión molesta.
