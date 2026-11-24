using UnityEngine;

namespace HuesoOxido.Survival
{
    /// <summary>Luz que se enciende al caer la tarde y se apaga al amanecer (linternas, braseros, faros).</summary>
    public sealed class NightLight : MonoBehaviour
    {
        Light _light;
        GameClock _clock;
        float _base;
        bool _always;

        public void Initialize(Light light, GameClock clock, float baseIntensity, bool alwaysOn = false)
        {
            _light = light; _clock = clock; _base = baseIntensity; _always = alwaysOn;
            Apply();
        }

        /// <summary>0 de dia, 1 de noche; rampa entre 18:30-20:30 y 5:30-7:00.</summary>
        public static float NightFactor(float hour)
        {
            if (hour >= 18.5f && hour < 20.5f) return Mathf.SmoothStep(0f, 1f, (hour - 18.5f) / 2f);
            if (hour >= 20.5f || hour < 5.5f) return 1f;
            if (hour < 7f) return 1f - Mathf.SmoothStep(0f, 1f, (hour - 5.5f) / 1.5f);
            return 0f;
        }

        void Update() { Apply(); }

        void Apply()
        {
            if (_light == null || _clock == null) return;
            float f = _always ? 1f : NightFactor(_clock.Hour);
            _light.intensity = _base * f;
            _light.enabled = f > 0.01f;
        }
    }
}
