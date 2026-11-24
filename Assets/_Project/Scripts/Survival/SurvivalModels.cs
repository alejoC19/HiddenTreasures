using System;
using HuesoOxido.Core;

namespace HuesoOxido.Survival
{
    public struct EnergyContext
    {
        public bool Moving, Sprinting, Crouched;
        public float LoadFraction;
        public float WeatherMultiplier;
        public float NightMultiplier;
    }

    /// <summary>Energia del explorador. Todo se paga con energia: correr, cargar peso, excavar y el clima.</summary>
    public sealed class EnergyModel
    {
        public const float WalkDrain = 0.12f, SprintDrain = 0.9f, CrouchDrain = 0.08f, IdleDrain = 0.03f;

        float _current;
        public event Action Changed;
        public event Action Exhausted;

        public EnergyModel(float max) { Max = max; _current = max; }

        public float Max { get; private set; }
        public float Current => _current;
        public float Fraction => Max <= 0f ? 0f : _current / Max;
        public bool IsExhausted => _current <= 0.01f;
        public bool CanSprint => _current > Max * 0.08f;

        public void SetMax(float max)
        {
            Max = Math.Max(1f, max);
            _current = Math.Min(_current, Max);
            Changed?.Invoke();
        }

        public void Set(float value) { _current = Math.Max(0f, Math.Min(Max, value)); Changed?.Invoke(); }

        public static float DrainPerSecond(EnergyContext c)
        {
            float rate = !c.Moving ? IdleDrain : (c.Sprinting ? SprintDrain : (c.Crouched ? CrouchDrain : WalkDrain));
            float load = 1f + 0.8f * c.LoadFraction * c.LoadFraction;
            float w = c.WeatherMultiplier <= 0f ? 1f : c.WeatherMultiplier;
            float n = c.NightMultiplier <= 0f ? 1f : c.NightMultiplier;
            return rate * load * w * n;
        }

        public void Tick(float dt, EnergyContext c) => Drain(DrainPerSecond(c) * dt);

        public void Drain(float amount)
        {
            if (amount <= 0f) return;
            bool was = IsExhausted;
            _current = Math.Max(0f, _current - amount);
            Changed?.Invoke();
            if (!was && IsExhausted) Exhausted?.Invoke();
        }

        public bool TryConsume(float amount)
        {
            if (_current < amount) return false;
            Drain(amount);
            return true;
        }

        public void Restore(float amount)
        {
            _current = Math.Min(Max, _current + Math.Max(0f, amount));
            Changed?.Invoke();
        }

        public void RestoreFull() { _current = Max; Changed?.Invoke(); }
    }

    public sealed class GameClock
    {
        /// <summary>Una jornada de 24 h dura 20 minutos reales.</summary>
        public const float RealSecondsPerDay = 1200f;

        public GameClock(float startHour = 17f, int startDay = 1) { Hour = startHour; Day = startDay; }

        public float Hour { get; private set; }
        public int Day { get; private set; }

        public bool IsNight => Hour >= 20.5f || Hour < 5.5f;

        public void Advance(float realSeconds)
        {
            Hour += realSeconds * 24f / RealSecondsPerDay;
            while (Hour >= 24f) { Hour -= 24f; Day++; }
        }

        /// <summary>Salta hasta la hora indicada (si ya paso, al dia siguiente).</summary>
        public void SkipTo(float hour)
        {
            if (hour <= Hour) Day++;
            Hour = hour;
        }

        public void Set(float hour, int day) { Hour = Math.Max(0f, Math.Min(23.999f, hour)); Day = Math.Max(1, day); }

        public string Format()
        {
            int h = (int)Hour;
            int m = (int)((Hour - h) * 60f);
            return h.ToString("00") + ":" + m.ToString("00");
        }
    }

    public sealed class WeatherModel
    {
        Random _rng;
        float _remaining;

        public WeatherModel(int seed) { _rng = new Random(seed); Current = WeatherKind.Clear; _remaining = NextDuration(); }

        public WeatherKind Current { get; private set; }
        public event Action<WeatherKind> Changed;

        float NextDuration() => 90f + (float)_rng.NextDouble() * 150f;

        public float EnergyMultiplier
        {
            get
            {
                switch (Current)
                {
                    case WeatherKind.Rain: return 1.35f;
                    case WeatherKind.Fog: return 1.1f;
                    default: return 1f;
                }
            }
        }

        public float FogDensity
        {
            get
            {
                switch (Current)
                {
                    case WeatherKind.Fog: return 0.028f;
                    case WeatherKind.Rain: return 0.011f;
                    case WeatherKind.Cloudy: return 0.004f;
                    default: return 0.0022f;
                }
            }
        }

        /// <summary>Menos luz solar en mal clima (multiplicador 0..1).</summary>
        public float LightMultiplier
        {
            get
            {
                switch (Current)
                {
                    case WeatherKind.Rain: return 0.55f;
                    case WeatherKind.Fog: return 0.7f;
                    case WeatherKind.Cloudy: return 0.8f;
                    default: return 1f;
                }
            }
        }

        public void Tick(float dt)
        {
            _remaining -= dt;
            if (_remaining > 0f) return;
            double r = _rng.NextDouble();
            WeatherKind next;
            switch (Current)
            {
                case WeatherKind.Clear: next = r < 0.7 ? WeatherKind.Cloudy : WeatherKind.Fog; break;
                case WeatherKind.Cloudy: next = r < 0.45 ? WeatherKind.Clear : (r < 0.8 ? WeatherKind.Rain : WeatherKind.Fog); break;
                case WeatherKind.Rain: next = r < 0.7 ? WeatherKind.Cloudy : WeatherKind.Clear; break;
                default: next = r < 0.6 ? WeatherKind.Clear : WeatherKind.Cloudy; break;
            }
            Set(next);
        }

        public void Set(WeatherKind kind)
        {
            Current = kind;
            _remaining = NextDuration();
            Changed?.Invoke(kind);
        }
    }
}
