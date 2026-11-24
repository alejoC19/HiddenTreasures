# Hidden Treasures

Juego single-player de exploración y búsqueda de tesoros (arqueología y paleontología). Unity 6 (URP), C#, PC Windows.
Namespaces técnicos internos: `HuesoOxido.*`. Estado y decisiones: ver [`STATUS.md`](STATUS.md). Recursos pendientes: [`ASSETS_TODO.md`](ASSETS_TODO.md).

## Cómo probar la demo
1. Instalar **Unity 6 LTS** (6000.0.x) y abrir esta carpeta desde Unity Hub (acepta la versión instalada si pide cambiarla).
2. Esperar la importación. El script `ProjectSetup` hace solo, la primera vez: activa el nuevo Input System (**pide reiniciar el Editor**), pone espacio de color Linear, crea el URP Asset y la escena `Assets/_Project/Scenes/Main.unity`.
   Si algo no se aplicó: menú **Hidden Treasures → Configurar proyecto**.
3. Abrir `Main.unity` y dar **Play**. El juego se construye solo al arrancar (mundo, personaje, vehículo, UI). Funciona incluso en una escena vacía.
4. Revisar la Console y reportar cualquier error o advertencia.

> Nunca se abrió en el Editor desde el entorno de desarrollo (no hay Unity allí): el código compila y su lógica pura está testeada con .NET, pero la primera ejecución en Unity es la prueba real. Ver `STATUS.md`.

## Controles
| Acción | Teclado y mouse | Gamepad |
|---|---|---|
| Mover / mirar | WASD / mouse | Stick izq. / der. |
| Correr / agacharse | Shift / C o Ctrl | Click stick izq. / B |
| Cambiar cámara 1ª/3ª persona | V | Click stick der. |
| Excavar (mantener sobre señal fuerte) | Clic izquierdo | RT |
| Interactuar | E | A |
| Inventario / mapa | Tab / M | Y / Select |
| Guardar | F5 | — |
| Cerrar menús | Esc | — |

## El loop de la demo
1. En el campamento, **mostrador** (E): comprar el permiso de excavación ($40; arrancás con $120).
2. Subir a la camioneta (E), cargar combustible en el surtidor si hace falta, y manejar al bosque y las ruinas.
3. Bajar (E) y barrer con el detector. La barra "SEÑAL" sube al acercarte; el cuadro de tierra removida es una pista visual.
4. Con la señal fuerte (bobina sobre el punto), mantener clic: excava con barra de progreso, animación y feedback.
5. El hallazgo entra en la mochila y **ocupa peso**. Mochila llena: queda en el suelo. Cargar peso frena y gasta más energía.
6. ¿Volvés a asegurar lo hallado o arriesgás por otra pieza? La energía, el clima, la noche y las caídas te empujan a volver. **Si te quedás sin energía te desmayás y 40% de la mochila queda tirada donde caíste.**
7. Guardar en el **baúl** del vehículo (E en la parte trasera), volver al campamento, **vender / restaurar / exhibir en el museo**, comprar mejoras y dormir en la carpa (guarda la partida).

## Estructura
```
Assets/_Project/Scripts/   Core, Player, Camera, Interaction, Detection, Digging, Inventory,
                           Vehicle, Economy, Collection, Survival, SaveSystem, UI, Bootstrap
Assets/_Project/Tests/     Tests EditMode (Unity Test Framework / NUnit)
tools/                     validate.sh + gen_validation.py (compilación y tests con .NET fuera de Unity)
```
Una asmdef por sistema. `Bootstrap` es la raíz de composición: el único sitio que conoce a todos los sistemas.

## Validación sin Unity
`tools/validate.sh` genera un proyecto .NET por asmdef, compila contra las referencias de UnityEngine, el código fuente real del Input System y uGUI, y ejecuta los tests. Primero `tools/setup_validation.sh` (instala .NET 8 si falta y compila el Input System desde su fuente oficial), después `tools/validate.sh`. No reemplaza probar en el Editor.

## Convenciones
- `HuesoOxido.Camera` puede ocultar `UnityEngine.Camera`: usar `UnityEngine.Camera` completo.
- Datos configurables como ScriptableObjects (`ItemDefinition`, `ItemCatalog`); el guardado solo usa IDs y DTOs, nunca referencias a assets.
- Git LFS para texturas, modelos y audio (`.gitattributes`). No se versionan `Library/`, `Temp/`, `Logs/` ni `UserSettings/`.
