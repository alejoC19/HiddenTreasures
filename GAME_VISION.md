# Hidden Treasures — Visión del juego

Documento de referencia. La versión completa del documento maestro del fundador es la fuente; aquí queda lo que guía cada decisión. Si algo contradice esta página, se discute antes de implementarlo.

## Qué es
Aventura de **exploración y expediciones** para PC (Windows), Unity 6 (6000.0.58f2, URP), C#, un jugador, guardado local, primera y tercera persona. Un explorador independiente con una camioneta usada y equipo básico recorre regiones remotas, encuentra tesoros, investiga misterios, afronta peligros y vuelve con el hallazgo.

> Preparar una expedición, viajar hacia lo desconocido, descubrir algo extraordinario, afrontar los peligros y conseguir regresar con el hallazgo.

No es un simulador de detector de metales: cada tesoro tiene contexto, esfuerzo, riesgo y recompensa.

## Pilares (todo sistema debe servir a alguno)
1. **Exploración**: motivos para recorrer el mundo (lugares ocultos, pistas, clima y hora que cambian la región).
2. **Preparación**: elegir destino, informarse, preparar vehículo, equipo y provisiones; el equipo no sirve igual en todas partes.
3. **Descubrimiento y recuperación**: objetos con rareza, conservación, antigüedad, procedencia, historia, autenticidad y valor; limpiar, identificar, restaurar.
4. **Supervivencia y peligro**: decisiones, no tareas molestas. Sin muerte permanente.
5. **Progresión**: mejores herramientas, vehículo, capacidad, regiones, campamento, colección y reputación.

## Ciclo principal
Información/destino → investigar riesgos → preparar vehículo y equipo → viajar → explorar → localizar → resolver desafíos → recuperar → decidir (seguir o volver) → regresar → limpiar/identificar/restaurar → vender, conservar o exhibir → cobrar → mejorar y desbloquear.

## Regiones (visión; se construyen de a una)
Bosque de las Ruinas (1.ª, aprendizaje) · Monasterio de las Sombras (misterio) · Selva Amazónica (dificultad alta, sigilo) · Abismo Marino (buceo, módulo propio) · Cordillera Congelada (supervivencia extrema). Detalle y estado actual: [`REGION_DESIGN.md`](REGION_DESIGN.md).

## Dirección artística (obligatoria, 100 % del juego)
Realismo con modelos y materiales reales gratuitos (Poly Haven, ambientCG, Sketchfab con licencia compatible, otras bibliotecas verificadas). Prohibido como solución final: árboles caricaturescos, rocas con primitivas, ruinas de cajas, vehículos de juguete, placeholders presentados como arte definitivo. Procedural solo para terreno, generación o elementos secundarios. Todo con licencia registrada (`CREDITS_ASSETS.md`, `CREDITS_BIOMAS.md`, créditos dentro del juego). Detalle del pipeline: [`ARTE_PLAN.md`](ARTE_PLAN.md).

## Rendimiento
Referencia: RX 580 8 GB, 1080p, 60 FPS ideal. **No se afirma rendimiento sin medir** (F9 en el juego genera `perf_summary.md`). Primero medir, después decidir.

## Cómo distinguimos el estado de algo
Código implementado → compilación verificada → tests ejecutados → render generado → gameplay probado → rendimiento medido en hardware real. Un render por software no es una prueba de juego.

## Reglas de trabajo (resumen)
No empezar de cero; no duplicar sistemas; no acumular scripts sin integración; commits pequeños; `stable` y desarrollo coherentes; docs y licencias al día; sin multijugador ni comercio online; sin convertir el juego en un shooter; no usar credenciales expuestas (las nuevas, por canal seguro).
