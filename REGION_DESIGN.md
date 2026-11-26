# Diseño de regiones

Principio: cada región es **una experiencia distinta**, no el mismo mapa con otras texturas. Lo sobrenatural y lo hostil aparecen solo en regiones específicas. Se construyen de a una; hasta que la primera esté completa y probada, no se empieza la siguiente.

## Estado actual de los biomas técnicos
Existen 4 perfiles (`Core/BiomeProfile.cs`). Los tres últimos se hicieron antes de esta visión; **no son regiones de diseño**, son prueba de que el motor sabe cambiar relieve, suelo, vegetación, clima y arte.

| Perfil actual | Mapea a la visión | Decisión propuesta |
|---|---|---|
| Bosque de las Ruinas (`forest_ruins`) | Región 1 | Es la región a terminar (Fase 1). |
| Pinar Nevado (`snow_pines`) | Base técnica de la Cordillera Congelada (R5) | Conservar como prototipo; rediseñar al llegar a esa fase (minas, refugios, frío real). |
| Pantano de la Niebla (`swamp_mire`) | Base técnica de partes de la Selva (R3: ríos y pantanos) | Conservar; rediseñar en la fase de la Selva. |
| Cañón Rojo (`desert_canyon`) | **No está en la visión** | Conservar como bioma "extra" para pruebas y posible expansión (Fase 6); no invertir diseño ahora. Pendiente de decisión del fundador. |

## Región 1 — Bosque de las Ruinas (en construcción)
- **Rol**: aprendizaje; el ciclo completo debe poder cerrarse aquí en 30–60 min.
- **Hoy**: bosque denso con pinos y hayas reales, camino, ruinas (templo, arco, pilares, antorchas), campamento (carpa, mostrador, surtidor, museo), 30 sitios, 3 reliquias, tormenta/lluvia/niebla, ciclo día-noche.
- **Falta para ser expedición completa**: preparación previa, equipo con función, hambre/sed/temperatura, peligros del brief (caídas, animales, pérdida de orientación, tormentas con efecto real), contratos, limpieza/identificación, historia de las ruinas, mapa con pistas.
- **Peligros previstos**: tormentas que bloquean zonas, caídas, terreno difícil, agotamiento, desorientación, fauna ligera.

## Región 2 — Monasterio de las Sombras (diseño, no construir aún)
Misterio y terror ambiental: monasterio, iglesia, cementerio, catacumbas, túneles. Peligros: derrumbes, oscuridad, inestabilidad, fenómenos con **reglas y razón narrativa**; sin combate masivo. Requiere: linterna con función real, interiores, iluminación dinámica, eventos escritos. Se valida primero una experiencia acotada.

## Región 3 — Selva Amazónica
Selva densa, ríos, cascadas, cuevas, asentamientos abandonados. Calor/humedad, fauna, insectos, inundaciones, amenazas humanas con **sigilo y evasión** (no guerra). Requiere: machete, GPS, comunicaciones, rutas alternativas, agua.

## Región 4 — Abismo Marino
Módulo especializado de buceo: oxígeno, profundidad, presión, corrientes, visibilidad, detector submarino, naufragios, recuperación de objetos pesados, equipo real. No se improvisa como "caminar bajo el agua".

## Región 5 — Cordillera Congelada
Frío extremo, avalanchas, hielo, minas y refugios; ropa térmica, combustible extra, GPS, equipo de emergencia, vehículos atascados.

## Qué debe tener toda región (checklist de diseño)
Identidad visual con el estándar de arte completo · actividades propias · peligros propios · equipo requerido/recomendado · permiso y costo · contratos · descubrimientos únicos · historia que se reconstruye con pistas · reglas de clima propias · pruebas jugadas y medición de rendimiento.
