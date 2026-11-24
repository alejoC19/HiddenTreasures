# Estado del proyecto

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
