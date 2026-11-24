# Primera ejecución en Windows — checklist

> **Versión actual de la guía de pruebas: `PRUEBAS.md`** (62 tests EditMode, Git LFS obligatorio, recorrido de 15 minutos con teclas de depuración F1–F8). Este archivo queda como referencia de instalación.

Marcá cada punto. **Si algo falla, detenete en ese punto** y mandame lo que se pide en «Qué enviarme» (al final).
Contexto: este proyecto se compiló y abrió en Unity 6000.0.58f2 real (sin errores), pero **nunca se ejecutó en Play Mode**: tu PC será la primera ejecución real.

## A. Preparar el PC
- [ ] **Git for Windows** instalado (incluye Git LFS). Verificá en PowerShell: `git --version`.
- [ ] **Unity Hub** instalado, con sesión iniciada y una **licencia Personal** (gratuita) activa: Hub → *Account/Licenses*.
- [ ] **Unity 6000.0.58f2** instalado desde el Hub: *Installs* → *Install Editor* → *Archive* → *Unity 6* → **6000.0.58f2**.
      Alternativa (enlace del Hub, no probado aquí): `unityhub://6000.0.58f2/92dee566b325`.
      No hace falta ningún módulo extra para probar en el Editor.
- [ ] Unos **10 GB libres** en disco.

## B. Clonar
> El repositorio es privado: Git te pedirá iniciar sesión en GitHub la primera vez (Git Credential Manager abre el navegador).
> Clonalo en una ruta **corta y fuera de OneDrive/Escritorio/Documentos** (la sincronización rompe la carpeta `Library`).

```powershell
git clone -b stable https://github.com/alejoC19/HiddenTreasures.git C:\Dev\HiddenTreasures
cd C:\Dev\HiddenTreasures
git log --oneline -1
```
- [ ] El último commit mostrado es el esperado (hoy `stable` apunta a la versión de la auditoría; si hay uno más nuevo, está bien).
- [ ] Existen las carpetas `Assets`, `Packages` y `ProjectSettings`.

## C. Abrir en Unity
- [ ] Unity Hub → **Add** → **Add project from disk** → elegir `C:\Dev\HiddenTreasures`.
- [ ] En la lista, la versión del proyecto aparece como **6000.0.58f2**. Abrirlo con esa versión. *(Si el Hub ofrece otra, elegí otra 6000.0.x; **no** uses 6000.3 u otra serie.)*
- [ ] Esperá la **primera importación** (puede tardar varios minutos: resuelve paquetes y compila shaders). No cierres ventanas.
- [ ] **Console** (Window → General → Console): sin errores rojos. *(Los avisos amarillos de paquetes de Unity son normales.)*
  Si el Editor pregunta por **Safe Mode**, **no** elijas *Ignore*: copiá los errores de la Console y detenete.
- [ ] Si aparece un diálogo sobre el **Input System / reiniciar el Editor**: aceptá (no debería, pero es inofensivo).

## D. Verificación automática de configuración
- [ ] Menú **Hidden Treasures → Verificar proyecto**. En la Console debe terminar con `=> OK` (17 líneas «OK»).
      Comprueba: escena Main, script del bootstrap, URP + SSAO, Input System, paquetes.

## E. Tests dentro de Unity
Window → General → **Test Runner**.
- [ ] Pestaña **EditMode** → **Run All** → esperados **34 verdes**.
- [ ] Pestaña **PlayMode** → **Run All** → esperados **11 verdes** (unos 2–3 minutos).
      Mientras corren **no toques teclado ni mouse** y dejá abierta la vista *Game*. Los tests crean sus propios dispositivos virtuales.
      Son dependientes entre sí (comparten la partida): si el primero falla, los demás fallan en cascada. Mirá solo el primero que falló.
- [ ] Anotá cuáles fallaron y copiá el mensaje (clic en el test → panel inferior).

*Por línea de comandos (opcional)* — con **el Editor cerrado** (no se puede ejecutar con el proyecto abierto):
```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.0.58f2\Editor\Unity.exe"
$proj  = "C:\Dev\HiddenTreasures"
# EditMode
Start-Process -Wait -FilePath $unity -ArgumentList "-batchmode","-projectPath",$proj,"-runTests","-testPlatform","EditMode","-testResults","$proj\TestResults-EditMode.xml","-logFile","$proj\editor-editmode.log"
# PlayMode
Start-Process -Wait -FilePath $unity -ArgumentList "-batchmode","-projectPath",$proj,"-runTests","-testPlatform","PlayMode","-testResults","$proj\TestResults-PlayMode.xml","-logFile","$proj\editor-playmode.log"
```
Resultados: `TestResults-*.xml` (NUnit) y el log del Editor. *(Estos comandos no se probaron en Windows; el equivalente de Linux sí funcionó hasta el punto donde exige licencia.)*

## F. Jugar (Play Mode)
- [ ] Abrí `Assets/_Project/Scenes/Main.unity` y presioná **Play**.
- [ ] Aparece el **menú principal** con el campamento girando detrás. **Hacé clic en la vista Game** para que capture el mouse cuando juegues.
- [ ] **Nueva partida** → se ve el campamento al atardecer; **WASD** y mouse responden; **V** cambia entre 1ª y 3ª persona sin cortes.
- [ ] **Mostrador** (apuntá y **E**): comprar el permiso → el dinero baja de $120 a $80.
- [ ] **Camioneta** (**E**): conducir con W/A/S/D, bajar con **E** (a baja velocidad). Probar el **surtidor**.
- [ ] Ir a las ruinas (mapa: **M**). La barra **SEÑAL** sube al acercarte a un punto; **mantené clic izquierdo** con la señal fuerte → barra de progreso, animación y aviso de hallazgo.
- [ ] El peso sube en el HUD; con la mochila (8 kg) llena, la pieza siguiente queda en el suelo.
- [ ] **Baúl** de la camioneta (**E** atrás) y **Tab** (inventario con mochila y baúl).
- [ ] En el campamento: **vender / restaurar / museo / mejoras**; **dormir en la carpa** (guarda la partida).
- [ ] **Esc → Menú principal → Continuar**: el dinero y el inventario se restauran.
- [ ] Correr con la mochila llena hasta quedarte sin energía: **desmayo**, objetos en el suelo, marcador en el mapa.
- [ ] **Esc** (pausa): sensibilidad, controles, reanudar.

## G. Si algo falla (rápido)
| Síntoma | Qué hacer |
|---|---|
| Todo **magenta/rosa** | Menú **Hidden Treasures → Desactivar URP (usar pipeline integrado)** y Play de nuevo. |
| El mouse/teclado no responden | Clic en la vista *Game*. Si sigue, Project Settings → Player → *Active Input Handling* = *Both* o *Input System Package*; reiniciar el Editor. Debería mostrar un aviso rojo en pantalla si el Input System no está activo. |
| Errores de compilación al abrir | No sigas; copiá la Console. |
| Se congela en «Importing»/«Compiling» | Esperá 10 min; si no avanza, cerrá el Hub, borrá la carpeta `Library` del proyecto y reabrí. |
| Un paquete no se resuelve | Window → Package Manager; verificá conexión a internet/proxy; reintentá. |

## Qué enviarme
1. El texto de la **Console** (clic derecho → *Copy*) o el archivo `%LOCALAPPDATA%\Unity\Editor\Editor.log`.
2. Qué tests de PlayMode fallaron y su mensaje.
3. En qué punto de este checklist te detuviste y, si algo se ve mal, una captura de pantalla.
