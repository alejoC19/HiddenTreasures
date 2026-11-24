# Estado del proyecto

Actualizado al cierre de la sesión de desarrollo autónomo inicial.

## Qué hay implementado (código completo, sin probar en el Editor)
| Objetivo de la demo | Estado | Dónde |
|---|---|---|
| Personaje controlable (caminar, correr, agacharse) | Implementado | `Player/PlayerMotor` |
| Primera y tercera persona (V), transición suave, colisión de cámara, sensibilidad | Implementado | `Camera/CameraRig`, `LookController` |
| Bosque explorable con ruinas | Implementado (placeholders) | `Bootstrap/WorldBuilder`, `CampBuilder` |
| Detector de metales: señal por proximidad, sonido, indicador visual y modelo en mano (1ª y 3ª) | Implementado | `Detection/*` |
| Excavación con barra, animación, golpes, tierra y aparición del objeto | Implementado | `Digging/*`, `Player/PlaceholderCharacterAnimator` |
| Hallazgos con rareza, peso, valor, descripción y nota real/ficción | Implementado (12 tipos) | `Core/DefaultItems` |
| Inventario con peso, mochila limitada, baúl separado, soltar/guardar | Implementado | `Inventory/*`, `UI/InventoryView` |
| Vehículo conducible, entrar/salir, baúl, combustible, colisiones | Implementado | `Vehicle/*` |
| Regreso al campamento, vender, restaurar, exhibir (museo), duplicados | Implementado | `Economy/*`, `Collection/*`, `UI/ShopView` |
| Mejoras (mochila, baúl, detector, pala, tanque, energía) y permisos de terreno | Implementado | `Economy/*` |
| Energía, hora del día, clima básico, caídas, desmayo con pérdida de objetos | Implementado | `Survival/*`, `Bootstrap/GameDirector` |
| Guardado local con versión de formato, DTOs, mochila, baúl, dinero, mejoras, permisos, colección, estado | Implementado | `SaveSystem/*` |
| HUD, mapa, inventario, tienda | Implementado | `UI/*` |
| Iluminación de hora dorada, niebla, post-proceso (bloom, grading, viñeta, DoF leve) | Implementado | `Survival/DayNightCycle`, `Bootstrap/PostProcessSetup` |

## Qué se verificó realmente
- **Compila**: 15 proyectos .NET (uno por asmdef) contra referencias de UnityEngine 2021.3, el código real del Input System 1.11.2 y uGUI. Cero errores.
- **28 tests pasan** (inventario y peso, economía, mejoras, permisos, museo, combustible, energía, reloj, clima determinista, colección, migración y archivo de guardado).

## Qué NO se verificó (requiere abrir Unity)
- Ejecución real: nada se corrió en el Editor. Esperar errores de primera ejecución (referencias nulas, orden de inicialización, APIs que cambian entre Unity 2021 y Unity 6).
- `Bootstrap/Editor/ProjectSetup.cs` (único código que usa UnityEditor) y la creación del URP Asset por reflexión.
- Post-procesado, que usa reflexión sobre URP y se desactiva con un aviso en consola si falla.
- Física del vehículo (suspensión por raycast) y sensación de manejo: los valores son una primera aproximación para ajustar jugando.
- Rendimiento, balance (precios, energía, probabilidades) y sensación del loop.

## Decisiones técnicas tomadas (no estaban en el prompt original)
- **Sin Cinemachine**: cámara propia (`CameraRig`) que se puede validar y controlar mejor.
- **Input por código**: las acciones se crean en C#, sin `.inputactions` a mano.
- **Mundo de la demo por código** (diseño fijo, misma semilla = mismo mundo) con geometría placeholder, para tener algo jugable sin assets. Los hallazgos con ubicación fija: torque de oro en el centro de las ruinas y el diario ficticio cerca de la entrada.
- **Desmayo**: al quedarte sin energía pierdas el 40% de la mochila (queda en el suelo, marcado en el mapa) y despertás en el campamento a las 7:00. Es una consecuencia del tira-y-afloja central; si querés otra regla, es un solo método (`GameDirector.OnExhausted`).
- Una asmdef por sistema, más `Bootstrap` como raíz de composición.

## Pendiente / próximos pasos
1. **Primera ejecución en Unity** y corregir lo que aparezca en Console (prioridad máxima).
2. Reemplazar placeholders por assets reales (ver `ASSETS_TODO.md`): personaje y animaciones Mixamo, vegetación, ruinas, detector, audio.
3. Tuning del vehículo y del balance económico y de energía.
4. Lluvia visible (hoy el clima afecta niebla, luz y energía, pero no hay partículas de lluvia).
5. Ambient Occlusion: agregar a mano el Renderer Feature "Screen Space Ambient Occlusion" en `Settings/HT_Renderer`.
6. Menú principal, pausa y ajustes (sensibilidad en pantalla).
