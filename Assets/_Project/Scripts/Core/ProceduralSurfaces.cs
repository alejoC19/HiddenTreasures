using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Materiales PBR pintados por codigo (sin archivos): pintura gastada con oxido, tela, cuero, caucho y metal.
    /// Cada textura es pequena (256 px) y se cachea; sirven al vehiculo, al personaje y a las herramientas.
    /// </summary>
    public static class ProceduralSurfaces
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        static float Fbm(float x, float y, int oct)
        {
            float a = 0.5f, s = 0f, f = 1f;
            for (int i = 0; i < oct; i++) { s += a * Mathf.PerlinNoise(x * f + i * 17.3f, y * f + i * 9.1f); a *= 0.5f; f *= 2f; }
            return s;
        }

        static Texture2D Paint(string name, int size, System.Func<float, float, Color> f)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, anisoLevel = 4 };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = f(x / (float)size, y / (float)size);
            t.SetPixels32(px); t.Apply(true);
            return t;
        }

        static Material Make(string key, Texture2D tex, Color tint, float smooth, float metal)
        {
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = MaterialUtil.Lit(tint, smooth, metal, tex);
            m.name = key;
            Cache[key] = m;
            return m;
        }

        /// <summary>Pintura de carroceria envejecida: base de color con variacion, rayones claros y manchas de oxido.</summary>
        public static Material WornPaint(string id, Color baseColor, float rust = 0.5f)
        {
            string key = "paint_" + id;
            if (Cache.TryGetValue(key, out var c) && c != null) return c;
            var tex = Paint(key, 256, (u, v) =>
            {
                float n = Fbm(u * 6f, v * 6f, 4);
                Color col = baseColor * (0.82f + n * 0.36f);
                float chip = Fbm(u * 18f + 4f, v * 18f, 3);
                float r = Mathf.Clamp01((chip - (0.62f - rust * 0.12f)) * 7f) * Mathf.Clamp01(Fbm(u * 3f + 9f, v * 3f, 2) * 1.6f - 0.35f + rust * 0.3f);
                col = Color.Lerp(col, new Color(0.42f, 0.2f, 0.09f), r);
                float scratch = Mathf.Pow(Mathf.PerlinNoise(u * 90f, v * 4f), 14f);
                col = Color.Lerp(col, new Color(0.75f, 0.74f, 0.68f), scratch * 0.5f);
                float grime = Mathf.Clamp01((0.35f - v) * 1.2f) * Fbm(u * 10f, v * 10f, 3) * 0.5f; // suciedad abajo
                col = Color.Lerp(col, new Color(0.22f, 0.17f, 0.11f), grime);
                col.a = 1f; return col;
            });
            return Make(key, tex, Color.white, 0.38f, 0.28f);
        }

        /// <summary>Tela gruesa (lona, camisa, pantalon) con trama visible.</summary>
        public static Material Fabric(string id, Color baseColor, float weaveScale = 70f)
        {
            string key = "fabric_" + id;
            if (Cache.TryGetValue(key, out var c) && c != null) return c;
            var tex = Paint(key, 256, (u, v) =>
            {
                float weave = (Mathf.Sin(u * weaveScale * 6.283f) * Mathf.Sin(v * weaveScale * 6.283f)) * 0.5f + 0.5f;
                float n = Fbm(u * 8f, v * 8f, 4);
                Color col = baseColor * (0.78f + n * 0.3f + weave * 0.12f);
                col.a = 1f; return col;
            });
            return Make(key, tex, Color.white, 0.05f, 0f);
        }

        public static Material Leather(string id, Color baseColor)
        {
            string key = "leather_" + id;
            if (Cache.TryGetValue(key, out var c) && c != null) return c;
            var tex = Paint(key, 256, (u, v) =>
            {
                float grain = Mathf.PerlinNoise(u * 60f, v * 60f);
                float wear = Fbm(u * 5f, v * 5f, 4);
                Color col = baseColor * (0.7f + wear * 0.5f) * (0.92f + grain * 0.16f);
                col.a = 1f; return col;
            });
            return Make(key, tex, Color.white, 0.22f, 0f);
        }

        /// <summary>Caucho de neumatico: casi negro con ruido fino y algo de polvo.</summary>
        public static Material Rubber()
        {
            const string key = "rubber";
            if (Cache.TryGetValue(key, out var c) && c != null) return c;
            var tex = Paint(key, 128, (u, v) =>
            {
                float n = Mathf.PerlinNoise(u * 40f, v * 40f);
                float rib = Mathf.Abs(Mathf.Sin(u * 24f * 3.1416f));
                float g = 0.07f + n * 0.05f + rib * 0.035f;
                float dust = Fbm(u * 6f, v * 6f, 3) * 0.06f;
                return new Color(g + dust, g + dust * 0.9f, g + dust * 0.8f, 1f);
            });
            return Make(key, tex, Color.white, 0.12f, 0f);
        }

        /// <summary>Metal desgastado (acero cepillado / llantas / herramientas).</summary>
        public static Material Steel(string id, Color baseColor, float smooth = 0.5f, float metal = 0.75f)
        {
            string key = "steel_" + id;
            if (Cache.TryGetValue(key, out var c) && c != null) return c;
            var tex = Paint(key, 128, (u, v) =>
            {
                float brushed = Mathf.PerlinNoise(u * 4f, v * 120f);
                float patina = Fbm(u * 7f, v * 7f, 3);
                Color col = baseColor * (0.75f + brushed * 0.3f) * (0.85f + patina * 0.3f);
                col.a = 1f; return col;
            });
            return Make(key, tex, Color.white, smooth, metal);
        }

        /// <summary>Cristal: oscuro y muy brillante (refleja el cielo sin transparencia para ser barato).</summary>
        public static Material Glass() => MaterialUtil.Lit(new Color(0.09f, 0.13f, 0.15f), 0.95f, 0.1f);

        public static Material Skin(Color c) => MaterialUtil.Lit(c, 0.28f, 0f);
    }
}
