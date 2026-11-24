using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Survival
{
    /// <summary>
    /// Atmosfera cercana a la camara: bruma baja que deriva entre los arboles y rayos de sol (solo con sol bajo).
    /// Es la alternativa barata a la niebla volumetrica, que URP no incluye. Todo es procedural y sigue a la camara.
    /// </summary>
    public sealed class AtmosphereFx : MonoBehaviour
    {
        const int ShaftCount = 16;

        Transform _cam;
        System.Func<float, float, float> _heightAt;
        Light _sun;
        GameClock _clock;
        WeatherModel _weather;
        ParticleSystem _mist;
        ParticleSystemRenderer _mistRenderer;
        Material _mistMat, _shaftMat;
        Transform[] _shafts;
        Vector3 _shaftCenter = new Vector3(1e6f, 0f, 0f);
        System.Random _rng = new System.Random(77);
        float _mistAlpha, _shaftAlpha;

        public void Initialize(Transform camera, Light sun, GameClock clock, WeatherModel weather, System.Func<float, float, float> heightAt)
        {
            _heightAt = heightAt;
            _cam = camera; _sun = sun; _clock = clock; _weather = weather;
            var shader = Shader.Find("HuesoOxido/AtmosphereSoft");
            if (shader == null || !MaterialUtil.UsingScriptablePipeline) { enabled = false; return; }

            var soft = SoftTexture(128, 128, 0);
            _mistMat = new Material(shader) { name = "Mist", mainTexture = soft };
            _mistMat.SetFloat("_SrcBlend", 5f); _mistMat.SetFloat("_DstBlend", 10f);
            _shaftMat = new Material(shader) { name = "Shafts", mainTexture = SoftTexture(64, 256, 1) };
            _shaftMat.SetFloat("_SrcBlend", 5f); _shaftMat.SetFloat("_DstBlend", 1f); // aditivo
            _shaftMat.SetVector("_NearFade", new Vector4(4f, 12f, 0f, 0f));

            BuildMist();
            BuildShafts();
        }

        static Texture2D SoftTexture(int w, int h, int mode)
        {
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w * 2f - 1f, v = (y + 0.5f) / h * 2f - 1f, a;
                    if (mode == 0) a = Mathf.Clamp01(1f - Mathf.Sqrt(u * u + v * v)); // circulo suave
                    else a = Mathf.Clamp01(1f - Mathf.Abs(u)) * Mathf.Clamp01(1f - Mathf.Abs(v) * 0.9f); // haz: centro y extremos suaves
                    a = a * a * (3f - 2f * a);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        void BuildMist()
        {
            var go = new GameObject("Mist");
            go.transform.SetParent(transform, false);
            _mist = go.AddComponent<ParticleSystem>();
            var main = _mist.main;
            main.loop = true; main.duration = 30f;
            main.startLifetime = 38f;
            main.startSize = new ParticleSystem.MinMaxCurve(14f, 30f);
            main.startSpeed = 0f;
            main.startColor = new Color(1f, 1f, 1f, 1f);
            main.maxParticles = 220;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var emission = _mist.emission; emission.rateOverTime = 6f;
            var shape = _mist.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(130f, 2.5f, 130f);
            var vel = _mist.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.2f, 0.6f); vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.25f);
            var col = _mist.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            _mistRenderer = go.GetComponent<ParticleSystemRenderer>();
            _mistRenderer.sharedMaterial = _mistMat;
            _mistRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            _mistRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mist.Play();
        }

        void BuildShafts()
        {
            _shafts = new Transform[ShaftCount];
            var mesh = new Mesh { name = "ShaftQuad" };
            mesh.vertices = new[] { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(0.5f, 1, 0), new Vector3(-0.5f, 1, 0) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            for (int i = 0; i < ShaftCount; i++)
            {
                var go = new GameObject("Shaft" + i);
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = _shaftMat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                _shafts[i] = go.transform;
            }
        }

        /// <summary>Fuerza segun hora (sol bajo) y clima (la niebla refuerza la bruma, la lluvia la apaga un poco).</summary>
        void Factors(out float mist, out float shafts)
        {
            float hour = _clock != null ? _clock.Hour : 17f;
            float elev = Mathf.Sin((hour - 6f) / 24f * Mathf.PI * 2f);
            float low = Mathf.Clamp01(1f - Mathf.Abs(elev - 0.22f) / 0.30f) * (elev > 0f ? 1f : 0f); // maximo con sol bajo
            float night = _clock != null && _clock.IsNight ? 1f : 0f;
            WeatherKind w = _weather != null ? _weather.Current : WeatherKind.Clear;
            mist = Mathf.Lerp(0.035f, 0.075f, low) * (w == WeatherKind.Fog ? 3.2f : (w == WeatherKind.Rain ? 1.6f : 1f)) * (1f - 0.5f * night);
            shafts = low * 0.16f * (w == WeatherKind.Clear ? 1f : (w == WeatherKind.Cloudy ? 0.35f : 0f));
        }

        void LateUpdate()
        {
            if (_cam == null || _mist == null) return;
            ApplyNow();
        }

        /// <summary>Actualiza colores, posiciones y orientacion (tambien lo usan las herramientas de Editor).</summary>
        public void ApplyNow()
        {
            if (_cam == null || _mist == null) return;
            Factors(out float mist, out float shafts);
            Vector3 cp = _cam.position;
            float gy = _heightAt != null ? _heightAt(cp.x, cp.z) : cp.y - 1.7f;
            _mist.transform.position = new Vector3(cp.x, gy + 1.2f, cp.z);
            Color sunC = _sun != null ? _sun.color : new Color(1f, 0.8f, 0.6f);
            _mistMat.SetColor("_Color", new Color(Mathf.Lerp(0.85f, sunC.r, 0.45f), Mathf.Lerp(0.88f, sunC.g, 0.45f), Mathf.Lerp(0.92f, sunC.b, 0.45f), mist));
            _shaftMat.SetColor("_Color", new Color(sunC.r, sunC.g * 0.92f, sunC.b * 0.8f, shafts));

            Vector3 toSun = _sun != null ? -_sun.transform.forward : Vector3.up;
            if ((cp - _shaftCenter).sqrMagnitude > 60f * 60f || shafts > 0f && _shafts[0].localScale.y < 1f) Scatter(cp);
            for (int i = 0; i < _shafts.Length; i++)
            {
                var t = _shafts[i];
                t.gameObject.SetActive(shafts > 0.005f);
                Vector3 axis = (-toSun).normalized;                    // direccion de propagacion (hacia el suelo)
                Vector3 toCam = cp - t.position; toCam -= axis * Vector3.Dot(toCam, axis);
                if (toCam.sqrMagnitude < 1e-4f) toCam = Vector3.right;
                t.rotation = Quaternion.LookRotation(toCam.normalized, -axis) * Quaternion.Euler(0f, 0f, 0f);
            }
        }

        void Scatter(Vector3 center)
        {
            _shaftCenter = center;
            Vector3 down = _sun != null ? _sun.transform.forward : Vector3.down;
            for (int i = 0; i < _shafts.Length; i++)
            {
                float a = (float)_rng.NextDouble() * Mathf.PI * 2f, r = 10f + (float)_rng.NextDouble() * 45f;
                float gx = center.x + Mathf.Cos(a) * r, gz = center.z + Mathf.Sin(a) * r;
                Vector3 ground = new Vector3(gx, _heightAt != null ? _heightAt(gx, gz) : center.y - 1.7f, gz);
                float slope = Mathf.Max(0.22f, -down.normalized.y);
                float len = Mathf.Min(14f + (float)_rng.NextDouble() * 12f, 11f / slope), width = 1.2f + (float)_rng.NextDouble() * 1.8f;
                // el haz baja desde la copa (arriba, contra el sol) hasta el suelo
                Vector3 top = ground - down.normalized * len;
                _shafts[i].position = top;
                _shafts[i].localScale = new Vector3(width, len, 1f);
            }
        }

        /// <summary>Para las vistas previas: simula la bruma durante unos segundos sin jugar.</summary>
        public void PrewarmForPreview(float seconds)
        {
            if (_mist == null) return;
            _mist.Simulate(seconds, true, true, true);
            _mist.Play();
        }
    }
}
