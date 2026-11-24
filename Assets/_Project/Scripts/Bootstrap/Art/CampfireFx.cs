using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Fuego de campamento con ParticleSystem real, generado por codigo: llamas (aditivo), humo (alfa) y chispas.
    /// Las texturas (gota de llama, bocanada de humo, punto) se pintan en memoria; no hay archivos externos.
    /// </summary>
    public static class CampfireFx
    {
        static Texture2D _flame, _smoke, _dot;

        static Texture2D Make(string name, int w, int h, System.Func<float, float, float> alpha)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a = Mathf.Clamp01(alpha(x / (float)(w - 1), y / (float)(h - 1)));
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            t.SetPixels32(px); t.Apply(true);
            return t;
        }

        static Texture2D FlameTex() => _flame != null ? _flame : _flame = Make("FlameTex", 64, 96, (u, v) =>
        {
            float width = Mathf.Sin(Mathf.Clamp01(v) * Mathf.PI) * (1f - v * 0.5f) * 0.45f + 0.02f;
            float wob = (Mathf.PerlinNoise(u * 5f, v * 4f) - 0.5f) * 0.2f * v;
            float d = Mathf.Abs(u - 0.5f + wob) / width;
            return (1f - d * d) * Mathf.Clamp01(v * 6f) * Mathf.Clamp01((1f - v) * 2.5f);
        });

        static Texture2D SmokeTex() => _smoke != null ? _smoke : _smoke = Make("SmokeTex", 64, 64, (u, v) =>
        {
            float dx = u - 0.5f, dy = v - 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
            float n = Mathf.PerlinNoise(u * 6f, v * 6f) * 0.5f + Mathf.PerlinNoise(u * 13f + 7f, v * 13f) * 0.5f;
            return Mathf.Clamp01(1f - r) * Mathf.Clamp01(0.45f + n * 0.9f) * Mathf.Clamp01(1f - r * r);
        });

        static Texture2D DotTex() => _dot != null ? _dot : _dot = Make("SparkTex", 16, 16, (u, v) =>
        {
            float r = Mathf.Sqrt((u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f)) * 2f;
            return Mathf.Pow(Mathf.Clamp01(1f - r), 1.6f);
        });

        static Material Mat(Texture2D tex, bool additive)
        {
            var sh = Shader.Find("HuesoOxido/AtmosphereSoft");
            Material m;
            if (sh != null)
            {
                m = new Material(sh);
                m.SetTexture("_MainTex", tex);
                m.SetColor("_Color", Color.white);
                m.SetFloat("_SrcBlend", 5f);
                m.SetFloat("_DstBlend", additive ? 1f : 10f);
                m.SetVector("_NearFade", new Vector4(0f, 0.15f, 0, 0));
            }
            else
            {
                m = new Material(MaterialUtil.ParticleShader()) { mainTexture = tex };
            }
            m.name = additive ? "FireAdd" : "FireAlpha";
            return m;
        }

        static ParticleSystem Emitter(string name, Transform parent, Vector3 pos, Texture2D tex, bool additive, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // emite hacia arriba
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mat(tex, additive);
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = order;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static AnimationCurve Curve(params float[] v)
        {
            var k = new Keyframe[v.Length / 2];
            for (int i = 0; i < k.Length; i++) k[i] = new Keyframe(v[i * 2], v[i * 2 + 1]);
            var c = new AnimationCurve(k);
            for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0.3f);
            return c;
        }

        /// <summary>Crea el sistema de particulas del fuego como hijo de <paramref name="parent"/>. Devuelve su raiz.</summary>
        public static Transform Create(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("FireFx").transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;

            // Llamas: nucleo amarillo que sube y se enfria a naranja/rojo.
            var flame = Emitter("Flames", root, Vector3.zero, FlameTex(), true, 2);
            var m = flame.main;
            m.loop = true; m.playOnAwake = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 0.95f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.0f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.55f, 0.85f);
            m.startRotation = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            m.maxParticles = 60;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startColor = new Color(1f, 0.8f, 0.45f, 0.9f);
            var em = flame.emission; em.rateOverTime = 22f;
            var sh = flame.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.22f;
            var col = flame.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.45f), new GradientColorKey(new Color(0.7f, 0.12f, 0.02f), 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.95f, 0.12f), new GradientAlphaKey(0.6f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = flame.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 0.7f, 0.3f, 1f, 1f, 0.15f));
            var noise = flame.noise; noise.enabled = true; noise.strength = 0.35f; noise.frequency = 1.2f; noise.scrollSpeed = 0.8f;

            // Humo: bocanadas grises que se ensanchan y se pierden con el viento.
            var smoke = Emitter("Smoke", root, new Vector3(0f, 0f, 0.5f), SmokeTex(), false, 1);
            var sm = smoke.main;
            sm.loop = true; sm.playOnAwake = false;
            sm.startLifetime = new ParticleSystem.MinMaxCurve(3.2f, 5f);
            sm.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            sm.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            sm.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            sm.maxParticles = 50;
            sm.simulationSpace = ParticleSystemSimulationSpace.World;
            sm.startColor = new Color(0.55f, 0.52f, 0.5f, 0.28f);
            var se = smoke.emission; se.rateOverTime = 9f;
            var ss = smoke.shape; ss.shapeType = ParticleSystemShapeType.Circle; ss.radius = 0.15f;
            var sc = smoke.colorOverLifetime; sc.enabled = true;
            var sg = new Gradient();
            sg.SetKeys(new[] { new GradientColorKey(new Color(0.6f, 0.5f, 0.42f), 0f), new GradientColorKey(new Color(0.42f, 0.42f, 0.42f), 1f) },
                       new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.5f, 0.15f), new GradientAlphaKey(0f, 1f) });
            sc.color = sg;
            var ssz = smoke.sizeOverLifetime; ssz.enabled = true; ssz.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 0.4f, 1f, 2.6f));
            var srot = smoke.rotationOverLifetime; srot.enabled = true; srot.z = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            var sn = smoke.noise; sn.enabled = true; sn.strength = 0.7f; sn.frequency = 0.35f; sn.scrollSpeed = 0.2f;
            var sv = smoke.velocityOverLifetime; sv.enabled = true; sv.space = ParticleSystemSimulationSpace.World; sv.x = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);

            // Chispas: puntos pequenos y brillantes que suben y se apagan.
            var spark = Emitter("Sparks", root, new Vector3(0f, 0f, 0.15f), DotTex(), true, 3);
            var pm = spark.main;
            pm.loop = true; pm.playOnAwake = false;
            pm.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            pm.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 2.2f);
            pm.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
            pm.maxParticles = 30;
            pm.gravityModifier = -0.05f;
            pm.simulationSpace = ParticleSystemSimulationSpace.World;
            pm.startColor = new Color(1f, 0.7f, 0.25f, 1f);
            var pe = spark.emission; pe.rateOverTime = 5f;
            var ps2 = spark.shape; ps2.shapeType = ParticleSystemShapeType.Cone; ps2.angle = 22f; ps2.radius = 0.15f;
            var pc = spark.colorOverLifetime; pc.enabled = true;
            var pg = new Gradient();
            pg.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.05f), 1f) },
                       new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.6f), new GradientAlphaKey(0f, 1f) });
            pc.color = pg;
            var pn = spark.noise; pn.enabled = true; pn.strength = 0.5f; pn.frequency = 1.5f;

            flame.Play(); smoke.Play(); spark.Play();
            return root;
        }

        /// <summary>Adelanta la simulacion (para vistas previas estaticas y para que no arranque vacio).</summary>
        public static void Prewarm(Transform fireRoot, float seconds)
        {
            if (fireRoot == null) return;
            foreach (var ps in fireRoot.GetComponentsInChildren<ParticleSystem>())
                ps.Simulate(seconds, true, true, true);
            foreach (var ps in fireRoot.GetComponentsInChildren<ParticleSystem>()) ps.Play();
        }
    }
}
