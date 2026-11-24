# Hidden Treasures

Juego single-player de exploración y búsqueda de tesoros. Unity 6 (URP), C#, PC Windows.
Namespaces técnicos: `HuesoOxido.*`.

## Abrir el proyecto
1. Instalar **Unity 6 LTS** (6000.0.x) desde Unity Hub.
2. Unity Hub → Add → seleccionar esta carpeta. Si pide cambiar de versión de Editor, aceptar la instalada.
3. Esperar la importación. Al pedir el backend del Input System, aceptar y reiniciar.
4. Revisar en Window → Package Manager que URP, Input System y Cinemachine (3.x) estén instalados.
5. Abrir Console y reportar cualquier error.

## Convención de namespaces
Existe el namespace `HuesoOxido.Camera`, que puede ocultar `UnityEngine.Camera`.
Dentro de ese código usar `using Cam = UnityEngine.Camera;` o `UnityEngine.Camera` completo.

## Git
- Git LFS para texturas, modelos y audio (ver `.gitattributes`).
- No se versionan `Library/`, `Temp/`, `Logs/` ni `UserSettings/`.

Recursos pendientes: ver `ASSETS_TODO.md`.
