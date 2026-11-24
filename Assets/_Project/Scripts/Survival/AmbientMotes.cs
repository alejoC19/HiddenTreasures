using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Survival
{
    /// <summary>
    /// Vida ambiental alrededor del jugador: polen dorado flotando de dia y luciernagas verdes que parpadean al atardecer y de noche.
    /// Dos ParticleSystem que siguen a la camara (volumen de 36 x 5 x 36 m), con texturas de punto pintadas por codigo.
    /// Se apagan con lluvia fuerte.
    /// </summary>
    public sealed class AmbientMotes : MonoBehaviour
    {
        ParticleSystem _pollen, _fireflies;
        Transform _cam;
        GameClock _clock;
        WeatherModel _weather;

        public void Initialize(Transform camera, GameClock clock, WeatherModel weather)
        {
            _cam = camera; _clock = clock; _weather = weather;
            var tex = BuildDot();
            _pollen = Build("Pollen", tex, new Color(1f, 0.92f, 0.62f, 0.55f), 0.025f, 0.05f, 6f, 10f, 0.15f, false);
            _fireflies = Build("Fireflies", tex, new Color(0.75f, 1f, 0.4f, 1f), 0.07f, 0.12f, 5f, 9f, 0.5f, true);
        }

        static Texture2D BuildDot()
        {
            var t = new Texture2D(16, 16, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "MoteDot" };
            var px = new Color32[256];
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
            {
                float r = Mathf.Sqrt((x - 7.5f) * (x - 7.5f) + (y - 7.5f) * (y - 7.5f)) / 8f;
                px[y * 16 + x] = new Color32(255, 255, 255, (byte)(Mathf.Pow(Mathf.Clamp01(1f - r), 1.8f) * 255f));
            }
            t.SetPixels32(px); t.Apply(true);
            return t;
        }

        ParticleSystem Build(string name, Texture2D tex, Color color, float sizeMin, float sizeMax, float lifeMin, float lifeMax, float speed, bool blink)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            var sh = Shader.Find("HuesoOxido/AtmosphereSoft");
            Material mat;
            if (sh != null)
            {
                mat = new Material(sh); mat.SetTexture("_MainTex", tex); mat.SetColor("_Color", Color.white);
                mat.SetFloat("_SrcBlend", 5f); mat.SetFloat("_DstBlend", 1f); mat.SetVector("_NearFade", new Vector4(0.3f, 1.2f, 0f, 0f));
            }
            else mat = new Material(MaterialUtil.ParticleShader()) { mainTexture = tex };
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var m = ps.main;
            m.loop = true; m.playOnAwake = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            m.startSpeed = speed;
            m.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            m.maxParticles = 220;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startColor = color;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(36f, 5f, 36f);
            var noise = ps.noise; noise.enabled = true; noise.strength = blink ? 0.9f : 0.5f; noise.frequency = 0.25f; noise.scrollSpeed = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            if (blink)
                g.SetKeys(new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0.1f, 0.28f), new GradientAlphaKey(1f, 0.42f),
                                  new GradientAlphaKey(0.1f, 0.6f), new GradientAlphaKey(0.9f, 0.76f), new GradientAlphaKey(0f, 1f) });
            else
                g.SetKeys(new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            ps.Play();
            return ps;
        }

        void LateUpdate()
        {
            if (_cam == null) return;
            transform.position = _cam.position + Vector3.down * 0.8f;
            float night = NightLight.NightFactor(_clock.Hour);
            float rain = _weather != null && _weather.Current == WeatherKind.Rain ? 0f : 1f;
            float fog = _weather != null && _weather.Current == WeatherKind.Fog ? 0.6f : 1f;
            var pe = _pollen.emission; pe.rateOverTime = (1f - night) * 10f * rain;
            var fe = _fireflies.emission; fe.rateOverTime = night * 9f * rain * fog;
        }
    }
}
