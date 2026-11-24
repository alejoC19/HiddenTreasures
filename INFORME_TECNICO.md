# Informe técnico — auditoría final de la versión estable

Fecha de la auditoría: 2026-11-24 · Proyecto: Hidden Treasures v0.1.0-demo · Editor objetivo: **Unity 6000.0.58f2 (LTS)**

## 1. Resumen ejecutivo
- Por primera vez el proyecto se abrió, importó y compiló en un **Editor de Unity real** (6000.0.58f2, instalado en el contenedor con el Unity CLI oficial).
- **Resultado:** 17 ensamblados (`HuesoOxido.*`, incluidos el código de Editor y los dos juegos de tests) compilan con el compilador de Unity con **0 errores y 0 advertencias**; se resuelven 26 paquetes; `Main.unity` se abre y su script se resuelve por GUID.
- **No se pudo ejecutar el juego.** Entrar en Play Mode y el Test Runner exigen una licencia de Unity activa («No valid Unity Editor license found») y este entorno no tiene ninguna. **Nada de lo que ocurre al dar Play está verificado.** Los 11 tests de PlayMode ya escritos lo comprobarían en cuanto exista una licencia.
- La auditoría encontró y corrigió **problemas reales de configuración** que habrían afectado la primera apertura (§3).

## 2. Qué se ejecutó de verdad (y qué no)
| Comprobación | Cómo | Resultado |
|---|---|---|
| El Editor existe en el catálogo oficial | `unity releases` (CLI oficial) | `6000.0.58f2` es una LTS válida. La serie 6000.0 llega hoy a 6000.0.86f1; hay además una LTS más nueva, 6000.3. |
| Resolución de paquetes | Editor real, log de batch | 26 paquetes registrados sin errores; Input System 1.11.2 desde el registro de Unity. |
| Compilación | Editor real (Bee/Roslyn) | 17 asmdef, 0 errores, 0 advertencias. Incluye `Bootstrap.Editor`, `Tests.EditMode` y `Tests.PlayMode`. |
| Importación del proyecto | Editor real | Sin errores de importación de nuestros assets ni de los URP Assets. |
| `Main.unity`, GUIDs, URP, Input System, paquetes | `ProjectVerifier` por `-executeMethod` | 17 comprobaciones OK (ver §6). |
| Lógica pura | 34 tests EditMode compilados con .NET | OK. **No** corridos dentro de Unity (el Test Runner exige licencia). |
| Auditoría estática | `tools/audit_repo.py` | 125 archivos con `.meta`, GUIDs únicos, asmdef sin ciclos. |
| **Play Mode / bootstrap en ejecución / loop / física / UI en pantalla / render** | — | **NO VERIFICADO.** |

Limitación adicional: se usó `-nographics` (dispositivo gráfico nulo). Aunque haya licencia en un entorno sin GPU, el render y los shaders (terreno URP, cielo, post-proceso) tampoco se validan ahí; los avisos «All subshaders removed» de los shaders de terreno aparecen solo por eso.

## 3. Hallazgos de esta auditoría y su corrección
| # | Hallazgo (comprobado con el Editor real) | Impacto | Corrección |
|---|---|---|---|
| 1 | Un proyecto nuevo generado por Unity nace con **`activeInputHandler: 0`** (Input Manager viejo) y sin URP asignado. | Teclado/mouse muertos o reinicio forzado del Editor en la primera apertura. | Se adoptó como base el **`ProjectSettings` completo de la plantilla oficial URP de Unity 6** (24 archivos): Input = «Both» (2, no requiere reinicio), Linear, URP asignado. |
| 2 | `manifest.json` fijaba URP 17.0.3 y Test Framework 1.4.5; el Editor trae **17.0.4** y **1.5.1**. | Unity sustituye las versiones (funcionó), pero el manifest mentía. | Manifest alineado: URP 17.0.4, Test Framework 1.5.1. |
| 3 | No había URP Assets ni Renderer en el repo; el plan era crearlos por reflexión (frágil). | Riesgo de render roto / magenta. | Se incluyen los **URP Assets oficiales** (`PC_RPAsset`/`PC_Renderer` con **SSAO**, `Mobile_*`, volúmenes, `GlobalSettings`) en `Assets/_Project/Settings`. El Renderer ya trae Screen Space Ambient Occlusion. |
| 4 | Faltaba `packages-lock.json`. | Resoluciones distintas entre máquinas. | Versionado. |
| 5 | `ProjectSettings` solo tenía 2 archivos. | Ajustes por defecto no controlados. | 24 archivos (ver #1). |
| 6 | (Auditorías previas) `MaterialUtil` elegía el shader de URP aunque el pipeline fuera el integrado; faltaban `.meta` y escena; dos clases en archivos de otro nombre. | Magenta, referencias rotas. | Corregido: shaders según pipeline, 125 `.meta` estables, `Main.unity`, clases separadas. |

## 4. Compatibilidad de versiones (verificada en el Editor real)
| Componente | Declarado en el proyecto | Resuelto por el Editor 6000.0.58f2 | Origen |
|---|---|---|---|
| Unity | 6000.0.58f2 (92dee566b325) | 6000.0.58f2 (92dee566b325) | Editor instalado |
| URP | 17.0.4 | 17.0.4 | Incluido en el Editor |
| Core RP / Shader Graph | (dependencia) | 17.0.4 | Incluido en el Editor |
| uGUI | 2.0.0 | 2.0.0 | Incluido en el Editor |
| Test Framework | 1.5.1 | 1.5.1 | Incluido en el Editor |
| Input System | 1.11.2 | 1.11.2 | Registro de Unity (descargado) |
| Burst / Collections / Mathematics | (transitivas) | 1.8.24 / 2.5.1 / 1.3.2 | Registro de Unity |

Nota de versión: la serie **6000.0** sigue siendo LTS y es la que se probó. Si instalás **6000.3** (otra LTS, más nueva), Unity ofrecerá actualizar y el Package Manager moverá URP y los paquetes núcleo a su serie 17.3; **eso no se probó**. Recomendado: 6000.0.58f2 o cualquier 6000.0.x.

## 5. Input System, URP y ajustes de proyecto (estado real)
- Active Input Handling = **2 (Ambos)**: el Input System funciona y no hace falta reiniciar. El juego no usa el Input Manager viejo (auditado: no hay `UnityEngine.Input`).
- Espacio de color **Linear**. Pipeline por defecto: **`PC_RPAsset`** (URP) con renderer **`PC_Renderer`** + SSAO.
- Post-proceso: Volume global creado por código (`PostProcessSetup`, reflexión sobre URP). **Sin ejecutar nunca**: si falla, avisa en consola y el juego sigue.
- `ProjectSetup` (Editor) queda como red de seguridad idempotente: con la base actual no hace nada.
- Los shaders se eligen según el pipeline activo (URP Lit / Standard); el juego también funciona sin URP.

## 6. `Main.unity`, GUIDs y referencias (verificado en el Editor real)
`ProjectVerifier` abrió la escena en Unity y comprobó: la escena carga; contiene el objeto `GameBootstrap`; **el componente se resuelve por GUID** (no es «Missing Script»); no hay MonoBehaviours con script faltante; el campo serializado `worldSeed` se deserializa (1337); la escena está en Build Settings. El GUID del script (`guid` de `GameBootstrap.cs.meta`) coincide con la referencia de la escena (también lo audita `tools/audit_repo.py`). La escena es mínima a propósito: un solo objeto; todo lo demás lo construye el código al arrancar.

## 7. El bootstrap: qué crea y en qué orden (revisión de código; NO ejecutado)
`GameBootstrap.Awake` crea, en este orden: Input y guardado → mundo (terreno, ~560 árboles combinados en mallas por zonas, rocas, ruinas, campamento, ~30 sitios enterrados, sol/luna/cielo, mapa) → jugador (CharacterController, cuerpo placeholder, animador) → cámara (+ post-proceso, mirada, rig 1ª/3ª) → supervivencia (energía, reloj, clima, lluvia, ciclo día/noche) → vehículo → detector, audio, excavación, modelo del detector → sensor de interacción → UI (HUD, inventario, tienda, mapa, menús) → `GameDirector` (reglas y cableado) → menú principal. El orden se auditó contra las dependencias de cada `Initialize`; **si el orden o alguna referencia falla, solo se verá al ejecutar**.
Los 11 tests de PlayMode (`Tests/PlayMode`) comprueban, en ejecución real: construcción de todos los sistemas, nueva partida, movimiento con teclado virtual, cambio de cámara, detector y excavación con clic mantenido, vehículo (entrar, conducir, frenar, bajar), guardado/carga, venta y mejora, desmayo, paneles de UI y menú.

## 8. Dependencias y recursos externos
Auditado: **no hay** `Resources.Load`, `StreamingAssets`, carga de escenas, tags/capas propias, ni el Input Manager viejo. Todo el contenido (modelos, texturas, audio, UI) es procedural. Recursos integrados usados: fuente `LegacyRuntime.ttf` (existe en Unity 6), shaders `Standard`, `Skybox/Procedural`, `Particles/Standard Unlit`, `Sprites/Default` y los de URP. Riesgo conocido: en una **build de jugador**, `Shader.Find` solo encuentra shaders incluidos en la build (la plantilla ya incluye `Sprites/Default`; `Standard`/`Skybox/Procedural` podrían faltar). No afecta al Editor; afecta a una build futura.

## 9. Riesgos ordenados
| Riesgo | Prob. | Impacto | Mitigación |
|---|---|---|---|
| R1. El juego nunca corrió: errores de ejecución al dar Play (nulos, orden de inicialización) | Media | Alto | Ejecutar los 11 tests de PlayMode y el checklist; enviar la Console. |
| R2. Render: terreno/URP/cielo/post-proceso no validados (solo `-nographics`) | Media | Medio | Probar en el Editor; **Hidden Treasures → Desactivar URP** como plan B. |
| R3. Física del vehículo (suspensión por raycast) mal ajustada | Media | Medio | Valores en `VehicleController`; test de conducción lo mide. |
| R4. Layout de la UI (uGUI por código) | Media | Medio | Revisar a 1920×1080; tamaños son constantes en `UiFactory`/vistas. |
| R5. Rendimiento (≈560 árboles + 30 sitios + terreno 257 + sombras 110 m) | Baja-Media | Medio | Profiler; bajar `shadowDistance` o árboles en `WorldBuilder`. |
| R6. Captura del mouse en el Editor (hay que hacer clic en la vista Game) | Alta | Bajo | Documentado; el juego relockea al hacer clic. |
| R7. Pausa con `timeScale = 0` y código que use tiempo escalado | Baja | Bajo | Los tests usan solo frames dentro de menús. |
| R8. Otra versión de Unity (6000.3) | Media | Medio | Usar 6000.0.58f2. |
| R9. Build de jugador (shaders no incluidos) | Alta si se compila | Medio | Fuera de alcance de la demo en Editor. |
| R10. Licencia de Unity del usuario | — | Bloquea | Unity Personal es gratuita; se necesita iniciar sesión en el Hub. |

## 10. Pasos exactos para la primera ejecución
1. **Instalar el Editor.** Unity Hub → *Installs* → *Install Editor* → elegir **6000.0.58f2** (si no aparece, *Archive/Download archive*; sirve cualquier 6000.0.x). Iniciar sesión en el Hub y activar una licencia Personal si Unity la pide.
2. **Obtener el proyecto:** `git clone -b stable https://github.com/alejoC19/HiddenTreasures.git` (la rama `stable` es la consolidada).
3. **Abrir:** Hub → *Add* → *Add project from disk* → la carpeta clonada → abrir con 6000.0.58f2. Si ofrece «cambiar de versión», aceptá solo si es una 6000.0.x.
4. **Esperar la primera importación** (en el contenedor tardó ≈1 min más ≈25 s de paquetes; en una PC con interfaz puede tardar varios minutos). No cerrar ventanas.
5. **Revisar la Console:** debería estar sin errores rojos. Si el Editor entra en *Safe Mode*, copiá los errores y detenete ahí.
6. Menú **Hidden Treasures → Verificar proyecto** → debe imprimir `=> OK` (17 comprobaciones).
7. **Window → General → Test Runner → EditMode → Run All**: esperados **34 verdes**.
8. **Test Runner → PlayMode → Run All**: esperados **11 verdes** (≈2–3 min). No toques teclado ni mouse mientras corren (usan dispositivos virtuales). Anotá cuáles fallan y el mensaje.
9. **Abrir `Assets/_Project/Scenes/Main.unity` → Play.** Hacé clic en la vista *Game* para capturar el mouse. Debe aparecer el menú principal con el campamento detrás.
10. **Nueva partida** y seguí el checklist de `STATUS.md` (12 pasos: permiso, vehículo, detector, excavación, mochila llena, baúl, venta/mejora, dormir/guardar, continuar, desmayo, pausa).
11. Si la imagen está **magenta**: menú **Hidden Treasures → Desactivar URP (usar pipeline integrado)** y volvé a dar Play.
12. **Qué enviarme:** el texto de la Console (clic derecho → *Copy*) o `%LOCALAPPDATA%\Unity\Editor\Editor.log` (Windows), los tests PlayMode que fallaron y, si algo se ve mal, una captura.

## 11. Reproducir esta auditoría
- `tools/validate.sh` — meta, auditoría estática, compilación .NET y tests puros (no requiere Unity; ver `tools/setup_validation.sh`).
- `tools/verify_in_unity.sh <carpeta-del-Editor>` — con un Editor real: compila con Unity y ejecuta `ProjectVerifier` (no requiere licencia en batch).
- `tools/run_unity_tests.sh <carpeta-del-Editor> EditMode|PlayMode` — tests en el Editor real (**requiere licencia activa**).

## 12. Para ejecutar el juego aquí (pendiente de una decisión tuya)
Las opciones para licenciar este contenedor requieren tu cuenta o tus términos de licencia, así que no activé nada: (a) iniciar sesión con `unity auth login` y activar Personal aceptando vos los términos; (b) activación offline: generar un `.alf` aquí, activarlo vos en tu cuenta de Unity y devolverme el `.ulf`; (c) un serial o servidor flotante si tenés. Con una licencia activa, ejecuto los 11 tests de PlayMode y el EditMode dentro de Unity de inmediato.
