using System;

namespace HuesoOxido.Core
{
    /// <summary>Estados de accion del jugador (la locomocion normal, caminar/correr, la maneja el motor).</summary>
    public enum PlayerAction { Idle, Crouching, Digging, PickingUp, Cleaning }

    /// <summary>
    /// Maquina de estados de la excavacion "cinematica": Idle/Crouching -> Digging -> (al aparecer la pieza) PickingUp -> Cleaning -> Idle/Crouching.
    /// Logica pura (sin motor): se alimenta con Tick(dt, agachado, cavando) y NotifyFind(). PickingUp y Cleaning son
    /// estados con duracion fija que no se pueden interrumpir y que bloquean el movimiento.
    /// Los nombres de los triggers del Animator estan en <see cref="PlayerAnimatorParams"/>.
    /// </summary>
    public sealed class PlayerStateMachine
    {
        public const float PickUpSeconds = 0.9f;
        public const float CleanSeconds = 1.9f;

        float _time;
        bool _crouched;

        public PlayerAction State { get; private set; } = PlayerAction.Idle;
        /// <summary>Segundos desde que se entro al estado actual.</summary>
        public float StateTime => _time;
        /// <summary>Duracion del estado (0 = abierto: sin fin propio).</summary>
        public float Duration => State == PlayerAction.PickingUp ? PickUpSeconds : (State == PlayerAction.Cleaning ? CleanSeconds : 0f);
        /// <summary>Avance 0..1 de los estados con duracion (PickingUp y Cleaning); 0 en el resto.</summary>
        public float Progress01 => Duration <= 0f ? 0f : Math.Min(1f, _time / Duration);
        public bool IsBusy => State == PlayerAction.PickingUp || State == PlayerAction.Cleaning;
        /// <summary>Mientras recoge o limpia el personaje no se mueve.</summary>
        public bool LocksMovement => IsBusy;

        /// <summary>(estado anterior, estado nuevo).</summary>
        public event Action<PlayerAction, PlayerAction> Changed;

        public void Tick(float dt, bool crouched, bool digging)
        {
            _crouched = crouched;
            _time += dt;
            switch (State)
            {
                case PlayerAction.PickingUp:
                    if (_time >= PickUpSeconds) Enter(PlayerAction.Cleaning);
                    break;
                case PlayerAction.Cleaning:
                    if (_time >= CleanSeconds) Enter(Rest());
                    break;
                default:
                    if (digging) { if (State != PlayerAction.Digging) Enter(PlayerAction.Digging); }
                    else Enter(Rest());
                    break;
            }
        }

        /// <summary>La excavacion termino y salio una pieza: pasa a recogerla (si no esta ya en esa secuencia).</summary>
        public void NotifyFind()
        {
            if (IsBusy) return;
            Enter(PlayerAction.PickingUp);
        }

        PlayerAction Rest() => _crouched ? PlayerAction.Crouching : PlayerAction.Idle;

        void Enter(PlayerAction next)
        {
            if (next == State) return;
            var prev = State;
            State = next;
            _time = 0f;
            Changed?.Invoke(prev, next);
        }
    }

    /// <summary>Nombres de los parametros del Animator Controller que el usuario crea luego (ver ANIMACIONES.md).</summary>
    public static class PlayerAnimatorParams
    {
        public const string Idle = "Idle";
        public const string Crouch = "Crouch";
        public const string Dig = "Dig";
        public const string PickUp = "PickUp";
        public const string Clean = "Clean";
        public const string IsMoving = "IsMoving";     // bool
        public const string Speed = "Speed";           // float 0..1

        public static string TriggerFor(PlayerAction a)
        {
            switch (a)
            {
                case PlayerAction.Crouching: return Crouch;
                case PlayerAction.Digging: return Dig;
                case PlayerAction.PickingUp: return PickUp;
                case PlayerAction.Cleaning: return Clean;
                default: return Idle;
            }
        }
    }
}
