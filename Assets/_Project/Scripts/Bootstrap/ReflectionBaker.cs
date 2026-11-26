using UnityEngine;
using HuesoOxido.Survival;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Reflejos del entorno real del juego: renderiza un cubemap pequeno (128 px) del bosque y lo usa como reflexion por defecto,
    /// asi la pintura, el vidrio y los cromados reflejan arboles y cielo de ese momento en vez de un cielo generico brillante.
    /// Se refresca al cambiar la hora (±30 min de juego) o el clima, repartido en 6 cuadros (una cara por cuadro) para no generar tirones.
    /// </summary>
    public sealed class ReflectionBaker : MonoBehaviour
    {
        const int Size = 128;
        RenderTexture _cube;
        UnityEngine.Camera _cam;
        Transform _focus;
        GameClock _clock;
        WeatherModel _weather;
        float _lastHour = -99f;
        WeatherKind _lastWeather;
        int _face = 6;

        public void Initialize(Transform focus, GameClock clock, WeatherModel weather)
        {
            _focus = focus; _clock = clock; _weather = weather;
            _cube = new RenderTexture(Size, Size, 16, RenderTextureFormat.DefaultHDR) { dimension = UnityEngine.Rendering.TextureDimension.Cube, useMipMap = true, autoGenerateMips = false, name = "ForestReflection" };
            _cube.Create();
            var go = new GameObject("ReflectionCamera");
            go.transform.SetParent(transform, false);
            _cam = go.AddComponent<UnityEngine.Camera>();
            _cam.enabled = false;
            _cam.clearFlags = CameraClearFlags.Skybox;
            _cam.cullingMask = ~0;
            _cam.nearClipPlane = 0.3f; _cam.farClipPlane = 300f;
            _cam.allowHDR = true; _cam.allowMSAA = false;
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            // customReflectionTexture existe en Unity 6 (antes "customReflection"); por reflexion para compilar en cualquier version.
            var prop = typeof(RenderSettings).GetProperty("customReflectionTexture") ?? typeof(RenderSettings).GetProperty("customReflection");
            prop?.SetValue(null, _cube);
            RenderSettings.reflectionIntensity = 1f;
        }

        /// <summary>Hornea las 6 caras de una vez (inicio del juego y vistas previas).</summary>
        public void BakeAll(Vector3 position)
        {
            if (_cam == null) return;
            _cam.transform.position = position;
            _cam.RenderToCubemap(_cube, 63);
            _cube.GenerateMips();
            _face = 6;
            if (_clock != null) { _lastHour = _clock.Hour; _lastWeather = _weather != null ? _weather.Current : WeatherKind.Clear; }
        }

        void Update()
        {
            if (_cam == null || _focus == null || _clock == null) return;
            if (_face < 6)
            {
                _cam.transform.position = _focus.position + Vector3.up * 1.8f;
                _cam.RenderToCubemap(_cube, 1 << _face);
                if (++_face == 6) _cube.GenerateMips();
                return;
            }
            float dh = Mathf.Abs(_clock.Hour - _lastHour); if (dh > 12f) dh = 24f - dh;
            var w = _weather != null ? _weather.Current : WeatherKind.Clear;
            if (dh > 0.5f || w != _lastWeather) { _lastHour = _clock.Hour; _lastWeather = w; _face = 0; }
        }

        void OnDestroy()
        {
            if (_cube != null) { _cube.Release(); Destroy(_cube); }
        }
    }
}
