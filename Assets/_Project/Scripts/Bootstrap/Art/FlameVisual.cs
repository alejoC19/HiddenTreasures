using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Core
{
    /// <summary>Llama de fogata: planos cruzados con alfa en forma de gota, mezcla aditiva (naranja exterior, amarillo interior). Sin esferas.</summary>
    public static class FlameVisual
    {
        static Texture2D _tex;

        static Texture2D Tex()
        {
            if (_tex != null) return _tex;
            const int w = 64, h = 128;
            _tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "FlameAlpha" };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float v = y / (float)(h - 1);                       // 0 base, 1 punta
                float width = Mathf.Sin(Mathf.Clamp01(v * 1.05f) * Mathf.PI) * (1f - v * 0.55f) * 0.5f + 0.02f;
                for (int x = 0; x < w; x++)
                {
                    float u = (x / (float)(w - 1) - 0.5f) * 2f * 0.5f;
                    float wob = (Mathf.PerlinNoise(x * 0.09f, y * 0.06f) - 0.5f) * 0.18f * v;
                    float d = Mathf.Abs(u + wob) / width;
                    float a = Mathf.Clamp01(1f - d * d);
                    a *= Mathf.Clamp01(v * 8f) * Mathf.Clamp01((1f - v) * 3f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            }
            _tex.SetPixels32(px); _tex.Apply();
            return _tex;
        }

        static Mesh Cross(float width, float height)
        {
            var m = new Mesh { name = "FlameCross" };
            var vs = new Vector3[12]; var uv = new Vector2[12]; var tr = new int[18];
            for (int p = 0; p < 3; p++)
            {
                float a = p * Mathf.PI / 3f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (width * 0.5f);
                int i = p * 4;
                vs[i] = -dir; vs[i + 1] = dir; vs[i + 2] = dir + Vector3.up * height; vs[i + 3] = -dir + Vector3.up * height;
                uv[i] = new Vector2(0, 0); uv[i + 1] = new Vector2(1, 0); uv[i + 2] = new Vector2(1, 1); uv[i + 3] = new Vector2(0, 1);
                int t = p * 6;
                tr[t] = i; tr[t + 1] = i + 2; tr[t + 2] = i + 1; tr[t + 3] = i; tr[t + 4] = i + 3; tr[t + 5] = i + 2;
            }
            m.vertices = vs; m.uv = uv; m.triangles = tr; m.RecalculateBounds();
            return m;
        }

        static Material Mat(Color c)
        {
            var sh = Shader.Find("HuesoOxido/AtmosphereSoft");
            if (sh == null) return MaterialUtil.Emissive(c);
            var m = new Material(sh) { name = "Flame" };
            m.SetTexture("_MainTex", Tex());
            m.SetColor("_Color", c);
            m.SetFloat("_SrcBlend", 5f); m.SetFloat("_DstBlend", 1f);
            m.SetVector("_NearFade", new Vector4(0f, 0.05f, 0, 0));
            m.renderQueue = 3100;
            return m;
        }

        /// <summary>Crea la llama como hijo de <paramref name="parent"/>; devuelve el transform que el parpadeo escala.</summary>
        public static Transform Create(Transform parent, Vector3 localPos)
        {
            var root = new GameObject("Flame").transform;
            root.SetParent(parent, false);
            root.localPosition = localPos;
            Layer(root, "Outer", 0.62f, 0.95f, new Color(1f, 0.42f, 0.08f, 0.85f));
            Layer(root, "Inner", 0.34f, 0.6f, new Color(1f, 0.85f, 0.35f, 0.9f));
            return root;
        }

        static void Layer(Transform root, string name, float w, float h, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = Cross(w, h);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Mat(c);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }
}
