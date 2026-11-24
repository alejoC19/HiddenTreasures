using System.Collections.Generic;
using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Player
{
    /// <summary>
    /// Pega la maquina de estados (PlayerStateMachine) con el juego: la alimenta cada frame, bloquea el movimiento mientras el
    /// personaje recoge/limpia, y dispara los TRIGGERS del Animator (Idle, Crouch, Dig, PickUp, Clean) si hay un Animator Controller
    /// asignado (ver ANIMACIONES.md). Tambien se comporta como IDigActivity para ocultar el detector mientras trabaja en el suelo.
    /// </summary>
    public sealed class PlayerStateDriver : MonoBehaviour, IDigActivity
    {
        public PlayerStateMachine Machine { get; } = new PlayerStateMachine();

        PlayerMotor _motor;
        IDigActivity _dig;
        PlaceholderCharacter _body;
        Animator _anim;
        HashSet<string> _params;
        bool _lockedByUs;

        public bool IsDigging => Machine.State == PlayerAction.Digging || Machine.IsBusy;
        public float Progress => Machine.State == PlayerAction.Digging && _dig != null ? _dig.Progress : Machine.Progress01;

        public void Initialize(PlayerMotor motor, IDigActivity dig, PlaceholderCharacter body)
        {
            _motor = motor; _dig = dig; _body = body;
            _anim = body != null ? body.ModelAnimator : null;
            Machine.Changed += OnChanged;
            CacheParams();
        }

        void CacheParams()
        {
            _params = new HashSet<string>();
            if (_anim == null || _anim.runtimeAnimatorController == null) return;
            foreach (var p in _anim.parameters) _params.Add(p.name);
        }

        /// <summary>True si hay un Animator Controller con parametros: en ese caso mandan las animaciones del usuario.</summary>
        public bool HasController => _params != null && _params.Count > 0;

        void OnChanged(PlayerAction from, PlayerAction to)
        {
            if (!HasController) return;
            foreach (var a in new[] { PlayerAction.Idle, PlayerAction.Crouching, PlayerAction.Digging, PlayerAction.PickingUp, PlayerAction.Cleaning })
            {
                string t = PlayerAnimatorParams.TriggerFor(a);
                if (_params.Contains(t)) _anim.ResetTrigger(t);
            }
            string trig = PlayerAnimatorParams.TriggerFor(to);
            if (_params.Contains(trig)) _anim.SetTrigger(trig);
        }

        void Update()
        {
            if (_motor == null) return;
            Machine.Tick(Time.deltaTime, _motor.IsCrouched, _dig != null && _dig.IsDigging);

            // Bloqueo de movimiento durante PickingUp/Cleaning (sin pisar al vehiculo, que usa el mismo flag).
            bool lockNow = Machine.LocksMovement;
            if (lockNow && !_lockedByUs && _motor.ControlEnabled) { _motor.ControlEnabled = false; _lockedByUs = true; }
            else if (!lockNow && _lockedByUs) { _motor.ControlEnabled = true; _lockedByUs = false; }

            if (HasController)
            {
                if (_params.Contains(PlayerAnimatorParams.IsMoving)) _anim.SetBool(PlayerAnimatorParams.IsMoving, _motor.IsMoving);
                if (_params.Contains(PlayerAnimatorParams.Speed)) _anim.SetFloat(PlayerAnimatorParams.Speed, _motor.Speed01);
            }
        }

        void OnDestroy() { Machine.Changed -= OnChanged; }
    }
}
