# Estado del proyecto

**Versión:** demo v0.1.0 (primera apertura pendiente). **Ramas:** `stable` (consolidada) y `claude/focused-darwin-ps2pj7` (desarrollo).

## Lo único que importa: qué se verificó y qué no

| Verificación | Resultado | Qué prueba realmente |
|---|---|---|
| Auditoría del repo (`tools/audit_repo.py`) | OK | Todos los archivos y carpetas tienen `.meta`, GUIDs únicos, asmdef sin ciclos ni referencias rotas, la escena apunta a un script existente, `EditorBuildSettings` apunta a la escena, namespaces y nombres de clase/archivo coherentes. |
| Compilación .NET (15 asmdef) | OK, 0 errores | Sintaxis y tipos contra UnityEngine 2021.3, el código real del Input System 1.11.2 y uGUI. **No** garantiza que compile igual en Unity 6 (APIs que cambian) ni que funcione al ejecutar. |
| Tests EditMode (34) | OK | Lógica pura: inventario y peso, economía, mejoras, permisos, museo, combustible, energía, reloj, clima, colección, archivo de guardado, consistencia del catálogo y **simulación del loop central** (permiso → hallazgo → peso → baúl → venta → mejora). |
| **Ejecución en Unity 6** | **NO REALIZADA** | Nada se corrió en el Editor. |

### No verificado por no haber Editor (riesgos conocidos)
1. **Todo el arranque** (`GameBootstrap`): orden de inicialización, referencias nulas, excepciones de primera ejecución.
2. **`ProjectSetup` (código de Editor)**: no hay `UnityEditor.dll` para compilarlo; se revisó a mano. Cambia `activeInputHandler` (reinicio del Editor) y crea el URP Asset por reflexión; si falla, avisa en consola y el juego sigue en pipeline integrado.
3. **Versiones**: `ProjectVersion.txt` (6000.0.58f2) y `manifest.json` (URP 17.0.3, Input System 1.11.2, uGUI 2.0.0, Test Framework 1.4.5) están escritos de memoria y **no** se pudieron resolver contra el registro de Unity 6.
4. **`ProjectSettings`**: solo existen `ProjectVersion.txt` y `EditorBuildSettings.asset`. No se escribieron a mano `ProjectSettings.asset`, `GraphicsSettings.asset`, etc. porque una edición manual incorrecta puede corromperlos; Unity los genera y `ProjectSetup` ajusta lo necesario.
5. **`Main.unity`** está escrita a mano (un solo GameObject con `GameBootstrap`). Si Unity la rechaza, borrarla: `ProjectSetup` la regenera.
6. **Visual**: URP vs. magenta, terreno con capas procedurales, cielo procedural, post-proceso por reflexión, lluvia, UI (tamaños, layouts de uGUI).
7. **Física del vehículo** (suspensión por raycast), **sensación de movimiento/cámara**, **rendimiento** y **balance** (precios, energía, probabilidades): valores de primera aproximación.

## Qué contiene la demo (implementado)
Personaje (caminar/correr/agacharse), cámara 1ª/3ª persona (V) con transición y colisión, bosque con ruinas y campamento (placeholders), detector (señal, sonido, indicador, modelo en mano), excavación (barra, animación, golpes), 12 hallazgos con rareza/peso/valor/descripción y nota real o ficción, inventario con peso y baúl, camioneta con combustible, mostrador (vender, restaurar, mejoras, permisos), museo, energía, hora y clima (con lluvia), mapa, HUD, **menú principal, pausa y controles**, guardado local con versión de formato, y post-proceso.

## Checklist de primera apertura (probar en este orden)
1. Abrir el proyecto; sin errores rojos en la Console tras la importación. Aceptar el reinicio del Input System.
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
- Los shaders se eligen según el pipeline activo (URP Lit o Standard) para evitar magenta; el juego funciona sin URP.
- Mochila base 8 kg (un test mostró que 12 kg no obligaba a decidir).
- Desmayo: pérdida del 40% de la mochila (objetos en el suelo) y despertar en el campamento a las 7:00 — es una consecuencia del tira-y-afloja central; cambiar la regla es un solo método (`GameDirector.OnExhausted`).

## Pendiente (después de la primera apertura)
1. Corregir lo que aparezca al ejecutar por primera vez (prioridad máxima).
2. Reemplazar placeholders por assets reales (`ASSETS_TODO.md`): personaje Mixamo, vegetación, ruinas, detector, audio.
3. Tuning del vehículo, del balance y del rendimiento.
4. Ambient Occlusion: agregar a mano el Renderer Feature SSAO en `Settings/HT_Renderer`.
5. Más terrenos y regiones (fuera del MVP).
