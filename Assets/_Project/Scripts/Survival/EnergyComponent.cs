using System;
using UnityEngine;
using HuesoOxido.Core;
using HuesoOxido.Player;

namespace HuesoOxido.Survival
{
    /// <summary>Conecta el modelo de energia con el movimiento, el peso, el clima y la hora.</summary>
    public sealed class EnergyComponent : MonoBehaviour, ISprintPermission, IEnergyConsumer
    {
        [SerializeField] float safeZoneRegenPerSecond = 5f;
        [SerializeField] float hardLandingThreshold = 10f;
        [SerializeField] float damagePerExtraMeterPerSecond = 2.4f;

        EnergyModel _model;
        PlayerMotor _motor;
        ILoadSource _load;
        WeatherModel _weather;
        GameClock _clock;
        Func<bool> _inSafeZone;
        Func<bool> _active;

        public EnergyModel Model => _model;
        public bool CanSprint => _model != null && _model.CanSprint;

        /// <summary>Se dispara cuando una caida fuerte le cuesta energia.</summary>
        public event Action<float> HardLanding;

        public void Initialize(EnergyModel model, PlayerMotor motor, ILoadSource load, WeatherModel weather,
                               GameClock clock, Func<bool> inSafeZone, Func<bool> active)
        {
            _model = model; _motor = motor; _load = load; _weather = weather; _clock = clock;
            _inSafeZone = inSafeZone; _active = active;
            _motor.Landed += OnLanded;
        }

        void OnDestroy() { if (_motor != null) _motor.Landed -= OnLanded; }

        void OnLanded(float impact)
        {
            if (impact <= hardLandingThreshold) return;
            float loss = (impact - hardLandingThreshold) * damagePerExtraMeterPerSecond;
            _model.Drain(loss);
            HardLanding?.Invoke(loss);
        }

        public bool TryConsume(float amount) => _model != null && _model.TryConsume(amount);

        void Update()
        {
            if (_model == null) return;
            float dt = Time.deltaTime;
            if (_active != null && !_active()) return;

            if (_inSafeZone != null && _inSafeZone())
            {
                _model.Restore(safeZoneRegenPerSecond * dt);
                return;
            }

            _model.Tick(dt, new EnergyContext
            {
                Moving = _motor.IsMoving,
                Sprinting = _motor.IsSprinting,
                Crouched = _motor.IsCrouched,
                LoadFraction = _load != null ? _load.LoadFraction : 0f,
                WeatherMultiplier = _weather != null ? _weather.EnergyMultiplier : 1f,
                NightMultiplier = _clock != null && _clock.IsNight ? 1.25f : 1f,
            });
        }
    }
}
