# Hidden Treasures

Juego single-player de exploración y búsqueda de tesoros (arqueología y paleontología). **Unity 6 (6000.0 LTS), URP, C#, PC Windows.**
Namespaces técnicos internos: `HuesoOxido.*`. Estado detallado, qué está verificado y qué no: [`STATUS.md`](STATUS.md). Recursos pendientes: [`ASSETS_TODO.md`](ASSETS_TODO.md).

> **Importante:** el proyecto se **abrió, importó y compiló en un Editor de Unity 6 real** (0 errores, 0 advertencias) y `Main.unity` se resuelve bien, pero **nunca se ejecutó en Play Mode** (el Editor exige licencia para eso y el entorno de desarrollo no tiene). La primera ejecución es la prueba real; ver `INFORME_TECNICO.md` y el checklist de `STATUS.md`.

## Primera apertura
Pasos exactos, riesgos y la evidencia de qué se verificó en un Editor real: **[`INFORME_TECNICO.md`](INFORME_TECNICO.md)** (sección 10).
Resumen: instalá **Unity 6000.0.58f2** (cualquier 6000.0.x), abrí la carpeta desde Unity Hub, esperá la importación, ejecutá **Hidden Treasures → Verificar proyecto** (debe dar OK), corré los tests (Test Runner) y abrí `Main.unity` → **Play**.

- El proyecto ya trae la configuración oficial de Unity 6 + URP (`ProjectSettings` y URP Assets de la plantilla): **no hace falta reiniciar el Editor** ni configurar el Input System (queda en «Both»).
- Si algo se ve **magenta**: **Hidden Treasures → Desactivar URP (usar pipeline integrado)**. El juego funciona en ambos pipelines (solo se pierde el post-proceso). **Hidden Treasures → Configurar proyecto** repite los ajustes.
- Hacé clic en la vista *Game* para capturar el mouse.
- El juego **se construye solo al dar Play**: `Main.unity` contiene un único objeto `GameBootstrap`. No hay prefabs a propósito.

## Menús
- **Menú principal** al iniciar: Continuar (si hay guardado), Nueva partida, Controles, Sensibilidad, Salir.
- **Pausa** con **Esc** durante el juego: Reanudar, Guardar, Cargar, Controles, Sensibilidad, Menú principal, Salir.
- Con cualquier menú o panel abierto el juego queda en pausa (tiempo y física).

## Controles
| Acción | Teclado y mouse | Gamepad |
|---|---|---|
| Mover / mirar | WASD / mouse | Stick izq. / der. |
| Correr / agacharse | Shift / C o Ctrl | Click stick izq. / B |
| Cámara 1ª / 3ª persona | V | Click stick der. |
| Excavar (mantener sobre señal fuerte) | Clic izquierdo | RT |
| Interactuar | E | A |
| Inventario / mapa | Tab / M | Y / Select |
| Guardar / pausa | F5 / Esc | — |
| Vehículo | W/S acelerar y frenar, A/D girar, E bajar | Stick izq. |

## El loop de la demo
1. En el campamento, **mostrador** (E): comprá el permiso de excavación ($40; arrancás con $120).
2. Subí a la camioneta (E), cargá combustible en el surtidor si hace falta, y manejá al bosque y las ruinas (mapa con M).
3. Bajá (E) y barré con el detector. La barra **SEÑAL** sube al acercarte; la tierra removida es una pista visual.
4. Con la señal fuerte, mantené el clic: excava con barra de progreso, animación y tierra.
5. El hallazgo entra en la mochila (**8 kg base**) y ocupa peso. Mochila llena: queda en el suelo. Cargar peso frena y gasta más energía.
6. ¿Volvés a asegurar lo hallado o arriesgás por una pieza más? Energía, clima, noche y caídas te empujan a volver. **Sin energía te desmayás y 40% de la mochila queda tirada donde caíste** (marcado en el mapa).
7. Guardá en el **baúl** del vehículo (E atrás), volvé al campamento, **vendé / restaurá / exhibí en el museo**, comprá mejoras y dormí en la carpa (guarda la partida).

## Estructura del repositorio
```
Assets/_Project/Scenes/            Main.unity (escena mínima: solo el GameBootstrap)
Assets/_Project/Scripts/           Core, Player, Camera, Interaction, Detection, Digging, Inventory,
                                   Vehicle, Economy, Collection, Survival, SaveSystem, UI, Bootstrap(+Editor)
Assets/_Project/Tests/EditMode/    Tests (Unity Test Framework / NUnit)
Assets/_Project/Settings/          URP Assets oficiales (PC/Mobile), volúmenes y GlobalSettings
Packages/manifest.json             Paquetes (Unity 6)
ProjectSettings/                   Ajustes oficiales de la plantilla URP de Unity 6 (Input = Both, Linear, URP asignado)
tools/                             Auditoría y validación fuera de Unity
```
Una asmdef por sistema. `Bootstrap` es la raíz de composición: el único sitio que conoce a todos los sistemas.

## Herramientas de verificación
- `tools/setup_validation.sh` (una vez): instala .NET 8 y compila el Input System desde su fuente oficial.
- `tools/validate.sh`: genera los `.meta` que falten, **audita el repositorio** (`tools/audit_repo.py`: meta, GUIDs, asmdef, escena, ajustes), **compila** cada asmdef contra las referencias de UnityEngine y **ejecuta los tests**.
- `tools/verify_in_unity.sh <Editor>`: con un Editor real compila con Unity y ejecuta `ProjectVerifier` (escena, GUIDs, URP, Input, paquetes). `tools/run_unity_tests.sh <Editor> EditMode|PlayMode` corre los tests dentro de Unity (**requiere licencia activa**).
- Compilar con .NET prueba la sintaxis y los tipos; **no** prueba cómo se comporta Unity al ejecutar.

## Convenciones
- `HuesoOxido.Camera` puede ocultar `UnityEngine.Camera`: usar `UnityEngine.Camera` completo.
- Datos configurables como ScriptableObjects; el guardado usa solo IDs y DTOs, nunca referencias a assets.
- Git LFS para texturas, modelos y audio (`.gitattributes`). No se versionan `Library/`, `Temp/`, `Logs/` ni `UserSettings/`.
- Ramas: `claude/focused-darwin-ps2pj7` (desarrollo) y `stable` (versión consolidada de la demo).
