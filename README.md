# Hidden Treasures

Juego single-player de exploración y búsqueda de tesoros (arqueología y paleontología). **Unity 6 (6000.0 LTS), URP, C#, PC Windows.**
Namespaces técnicos internos: `HuesoOxido.*`. Estado detallado, qué está verificado y qué no: [`STATUS.md`](STATUS.md). Recursos pendientes: [`ASSETS_TODO.md`](ASSETS_TODO.md).

> **Importante:** este código se desarrolló sin acceso al Editor de Unity. Compila con .NET y su lógica pura tiene tests, pero **nunca se ejecutó dentro de Unity**. La primera apertura es la prueba real; seguí el checklist de `STATUS.md`.

## Primera apertura (paso a paso)
1. Instalá **Unity 6 LTS** (Unity Hub, módulo Windows Build Support si vas a compilar). El proyecto declara `6000.0.58f2`; si instalaste otra 6000.0.x, aceptá el aviso de "cambiar de versión".
2. Unity Hub → **Add** → elegí esta carpeta. Esperá la importación y la resolución de paquetes (Input System, URP, uGUI, Test Framework).
   Si el Package Manager marca error de versión, abrí Window → Package Manager y poné la versión que ofrece tu Editor para ese paquete.
3. Al terminar, el script `ProjectSetup` se ejecuta solo:
   - activa el **nuevo Input System** y **pide reiniciar el Editor** (aceptá; sin esto el juego no recibe teclado ni mouse y muestra un aviso en pantalla);
   - pone espacio de color Linear;
   - crea el **URP Asset** (`Assets/_Project/Settings`) y lo asigna.
4. Abrí `Assets/_Project/Scenes/Main.unity` (también está en Build Settings) y dale **Play**.
5. Mirá la **Console** y pasame todo lo que salga en rojo o amarillo.

Si algo se ve **magenta**: menú **Hidden Treasures → Desactivar URP (usar pipeline integrado)**. El juego funciona en ambos pipelines (solo se pierde el post-proceso). Para volver: **Hidden Treasures → Activar URP**. **Hidden Treasures → Configurar proyecto** repite todos los pasos.

El juego **se construye solo al dar Play** (mundo, personaje, vehículo, UI): `Main.unity` contiene un único objeto `GameBootstrap`; incluso en una escena vacía el arranque automático lo crea. No hay prefabs a propósito: todo se arma por código con placeholders.

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
Assets/_Project/Settings/          Aquí se crea el URP Asset
Packages/manifest.json             Paquetes (Unity 6)
ProjectSettings/                   ProjectVersion.txt y EditorBuildSettings.asset; el resto lo genera Unity
tools/                             Auditoría y validación fuera de Unity
```
Una asmdef por sistema. `Bootstrap` es la raíz de composición: el único sitio que conoce a todos los sistemas.

## Herramientas fuera de Unity (no reemplazan al Editor)
- `tools/setup_validation.sh` (una vez): instala .NET 8 y compila el Input System desde su fuente oficial.
- `tools/validate.sh`: genera los `.meta` que falten, **audita el repositorio** (`tools/audit_repo.py`: meta, GUIDs, asmdef, escena, ajustes), **compila** cada asmdef contra las referencias de UnityEngine y **ejecuta los tests**.
- Compilar con .NET prueba la sintaxis y los tipos; **no** prueba cómo se comporta Unity al ejecutar.

## Convenciones
- `HuesoOxido.Camera` puede ocultar `UnityEngine.Camera`: usar `UnityEngine.Camera` completo.
- Datos configurables como ScriptableObjects; el guardado usa solo IDs y DTOs, nunca referencias a assets.
- Git LFS para texturas, modelos y audio (`.gitattributes`). No se versionan `Library/`, `Temp/`, `Logs/` ni `UserSettings/`.
- Ramas: `claude/focused-darwin-ps2pj7` (desarrollo) y `stable` (versión consolidada de la demo).
