# Cómo poner tus animaciones de Mixamo

El personaje (`Resources/Art/Models/Soldier.glb`) ya se anima solo con Idle/Walk/Run y poses por código para cavar, recoger y limpiar.
Cuando tengas tus animaciones, **no hay que tocar código**:

1. En Unity: *Assets > Create > Animation > Animator Controller* y guardalo exactamente como
   `Assets/_Project/Resources/Art/Animation/PlayerController.controller`.
2. Importá tus FBX de Mixamo (con *Animation Type: Generic*, o *Humanoid* si usás otro personaje) y arrastrá los clips al controller.
3. Creá estos **parámetros** (los nombres importan):

| Parámetro | Tipo | Cuándo se dispara |
|---|---|---|
| `Idle` | Trigger | vuelve al reposo |
| `Crouch` | Trigger | el jugador se agacha (C) |
| `Dig` | Trigger | empieza a cavar (mantener clic) |
| `PickUp` | Trigger | al aparecer la pieza (≈0,9 s) |
| `Clean` | Trigger | limpiar con el cepillo (≈1,9 s) |
| `IsMoving` | Bool | el jugador se mueve |
| `Speed` | Float 0..1 | de caminar (≈0,5) a correr (1) |

4. Armá las transiciones: `Any State → Dig` con el trigger `Dig`, `Dig → PickUp` con `PickUp`, `PickUp → Clean` con `Clean`, `Clean → Idle` con `Idle`
   (y `Idle ⇄ Crouch`, y un Blend Tree Idle/Walk/Run con `Speed`).

Si el controller existe, el juego lo usa (`PlaceholderCharacter.HasAnimatorController`) y deja de aplicar las poses por código;
sigue mostrando la pala (al cavar) y el cepillo (al limpiar). La lógica está en `Core/PlayerStateMachine.cs` y `Player/PlayerStateDriver.cs`.
