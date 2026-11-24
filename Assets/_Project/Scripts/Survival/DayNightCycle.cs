using UnityEngine;
using UnityEngine.Rendering;

namespace HuesoOxido.Survival
{
    /// <summary>Sol, luna, ambiente, niebla y cielo segun la hora y el clima. Arranca en hora dorada.</summary>
    public sealed class DayNightCycle : MonoBehaviour
    {
        Light _sun, _moon;
        GameClock _clock;
        WeatherModel _weather;
        Material _sky;
        UnityEngine.Camera _cam;

        static readonly Color SunLow = new Color(1.00f, 0.50f, 0.20f);
        static readonly Color SunHigh = new Color(1.00f, 0.96f, 0.88f);
        static readonly Color Moon = new Color(0.45f, 0.55f, 0.85f);

        public void Initialize(Light sun, Light moon, GameClock clock, WeatherModel weather, Material skybox, UnityEngine.Camera cam)
        {
            _cam = cam;
            _sun = sun; _moon = moon; _clock = clock; _weather = weather; _sky = skybox;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            if (_sky != null) RenderSettings.skybox = _sky;
            Apply();
        }

        void Update() { if (_clock != null) Apply(); }

        /// <summary>Aplica el estado actual de hora y clima sin esperar a Update (herramientas de Editor).</summary>
        public void ApplyNow() { if (_clock != null) Apply(); }

        void Apply()
        {
            float pitch = (_clock.Hour - 6f) / 24f * 360f;  // 6h: horizonte, 12h: cenit, 18h: horizonte
            float elev = Mathf.Sin(pitch * Mathf.Deg2Rad);
            float light = _weather != null ? _weather.LightMultiplier : 1f;

            float dayAmt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.06f, 0.3f, elev));
            float warm = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.5f, elev));

            _sun.transform.rotation = Quaternion.Euler(Mathf.Max(3f, pitch > 180f ? 3f : pitch), 250f, 0f);
            _sun.color = Color.Lerp(SunHigh, SunLow, warm);
            _sun.intensity = Mathf.Lerp(1.35f, 2.1f, warm) * dayAmt * light;
            _sun.enabled = _sun.intensity > 0.01f;

            _moon.transform.rotation = Quaternion.Euler(48f, 140f, 0f);
            _moon.color = Moon;
            _moon.intensity = 0.16f * (1f - dayAmt) * Mathf.Lerp(0.6f, 1f, light);
            _moon.enabled = _moon.intensity > 0.005f;

            var daySky = new Color(0.62f, 0.72f, 0.90f);
            var duskSky = new Color(0.80f, 0.58f, 0.48f);
            var nightSky = new Color(0.05f, 0.07f, 0.14f);
            Color sky = Color.Lerp(nightSky, Color.Lerp(daySky, duskSky, warm * 0.8f), dayAmt);
            RenderSettings.ambientSkyColor = sky * light * Mathf.Lerp(1f, 0.8f, warm);
            RenderSettings.ambientEquatorColor = Color.Lerp(sky, new Color(0.55f, 0.50f, 0.42f), 0.35f) * 0.85f * light;
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.22f, 0.16f) * Mathf.Lerp(0.3f, 1f, dayAmt);

            Color fogDay = Color.Lerp(new Color(0.62f, 0.7f, 0.76f), new Color(0.85f, 0.58f, 0.40f), warm * 0.8f);
            RenderSettings.fogColor = Color.Lerp(new Color(0.03f, 0.04f, 0.08f), fogDay, dayAmt) * Mathf.Lerp(0.55f, 1f, light);
            float night = 1f - dayAmt;
            RenderSettings.fogDensity = (_weather != null ? _weather.FogDensity : 0.006f) * (1f + night * 0.6f);

            if (_sky == null && _cam != null)
            {
                _cam.clearFlags = CameraClearFlags.SolidColor;
                _cam.backgroundColor = RenderSettings.fogColor;
            }
            if (_sky != null)
            {
                if (_sky.HasProperty("_Exposure")) _sky.SetFloat("_Exposure", Mathf.Lerp(0.08f, 1.15f, dayAmt) * Mathf.Lerp(0.6f, 1f, light));
                if (_sky.HasProperty("_AtmosphereThickness")) _sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(0.7f, 1.1f, warm));
            }
        }
    }
}
