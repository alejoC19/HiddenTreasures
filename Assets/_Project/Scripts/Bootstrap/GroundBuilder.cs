using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>Malla del suelo (con colision) y su material de capas PBR. Reemplaza al Terrain de Unity para controlar el aspecto y el camino.</summary>
    public static class GroundBuilder
    {
        const int MapTex = 1024;

        public static GameObject BuildMesh(HeightField f, Transform parent)
        {
            int n = HeightField.Nodes;
            var verts = new Vector3[n * n];
            var uvs = new Vector2[n * n];
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    verts[z * n + x] = new Vector3(-HeightField.Half + x * HeightField.Step, f.H[z, x], -HeightField.Half + z * HeightField.Step);
                    uvs[z * n + x] = new Vector2(x / (n - 1f), z / (n - 1f));
                }
            var tris = new int[(n - 1) * (n - 1) * 6];
            int t = 0;
            for (int z = 0; z < n - 1; z++)
                for (int x = 0; x < n - 1; x++)
                {
                    int i00 = z * n + x, i10 = i00 + 1, i01 = i00 + n, i11 = i01 + 1;
                    tris[t++] = i00; tris[t++] = i01; tris[t++] = i11;
                    tris[t++] = i00; tris[t++] = i11; tris[t++] = i10;
                }
            var mesh = new Mesh { name = "GroundMesh" };
            mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();

            var go = new GameObject("Ground");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.sharedMaterial = MaterialUtil.Lit(new Color(0.3f, 0.3f, 0.2f), 0.05f); // se reemplaza en ApplyMaterial
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        static float Smooth(float d, float a, float b)
        {
            float t = Mathf.Clamp01((d - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        static float Bilinear(float[,] g, float fx, float fz)
        {
            int w = g.GetLength(1), h = g.GetLength(0);
            fx = Mathf.Clamp(fx, 0f, w - 1.001f); fz = Mathf.Clamp(fz, 0f, h - 1.001f);
            int ix = (int)fx, iz = (int)fz; float tx = fx - ix, tz = fz - iz;
            return (g[iz, ix] * (1 - tx) + g[iz, ix + 1] * tx) * (1 - tz) + (g[iz + 1, ix] * (1 - tx) + g[iz + 1, ix + 1] * tx) * tz;
        }

        /// <summary>Genera el mapa de control (pasto/camino/tierra roja) y el macro de color, y arma el material del suelo.</summary>
        public static void ApplyMaterial(GameObject ground, HeightField f, float[,] forestMask, PerlinNoise noise)
        {
            float size = HeightField.Size, half = HeightField.Half;
            // Distancia al camino en una grilla de 1 m (evita recorrer el camino por cada pixel).
            int rg = (int)size + 1;
            var roadDist = new float[rg, rg];
            for (int z = 0; z < rg; z++)
                for (int x = 0; x < rg; x++)
                    roadDist[z, x] = f.Road != null ? f.Road.DistanceTo(-half + x, -half + z) : 99f;

            var control = new Color32[MapTex * MapTex];
            var macro = new Color32[MapTex * MapTex];
            int mw = forestMask.GetLength(1);
            for (int py = 0; py < MapTex; py++)
            {
                float z = -half + (py + 0.5f) / MapTex * size;
                for (int px = 0; px < MapTex; px++)
                {
                    float x = -half + (px + 0.5f) / MapTex * size;
                    float jitter = (noise.Noise(x * 0.7f, z * 0.7f) - 0.5f) * 1.6f;
                    float rd = Bilinear(roadDist, x + half, z + half);
                    float wRoad = 1f - Smooth(rd + jitter, 1.9f, 3.4f);

                    float dc = Mathf.Sqrt((x - WorldLayout.CampX) * (x - WorldLayout.CampX) + (z - WorldLayout.CampZ) * (z - WorldLayout.CampZ));
                    wRoad = Mathf.Max(wRoad, 1f - Smooth(dc + jitter * 3f, 8f, 13f));

                    float dr = Mathf.Sqrt((x - WorldLayout.RuinsX) * (x - WorldLayout.RuinsX) + (z - WorldLayout.RuinsZ) * (z - WorldLayout.RuinsZ));
                    float wRed = 1f - Smooth(dr + jitter * 4f, 6f, 13f);
                    wRed = Mathf.Max(wRed, Smooth(noise.Fbm(x * 0.045f + 40f, z * 0.045f + 40f, 2), 0.80f, 0.90f) * 0.8f);

                    float fm = Bilinear(forestMask, (x + half) / size * (mw - 1), (z + half) / size * (mw - 1));
                    float gn = noise.Fbm(x * 0.016f + 7f, z * 0.016f + 3f, 3);
                    float wGrass = Smooth(gn, 0.40f, 0.54f) * (1f - fm * 0.55f);

                    wRed *= 1f - wRoad; wGrass *= (1f - wRoad) * (1f - wRed);
                    control[py * MapTex + px] = new Color32((byte)(wGrass * 255f), (byte)(wRoad * 255f), (byte)(wRed * 255f), 255);

                    float m = 0.5f + (noise.Fbm(x * 0.03f + 90f, z * 0.03f + 20f, 3) - 0.5f) * 0.36f + (noise.Noise(x * 0.55f, z * 0.55f) - 0.5f) * 0.14f;
                    m *= 1f - 0.30f * fm;
                    float tint = (noise.Fbm(x * 0.011f + 5f, z * 0.011f + 55f, 2) - 0.5f) * 0.16f;
                    macro[py * MapTex + px] = new Color32(
                        (byte)(Mathf.Clamp01(m * (0.92f + tint)) * 255f), (byte)(Mathf.Clamp01(m * 1.10f) * 255f), (byte)(Mathf.Clamp01(m * (0.85f - tint)) * 255f), 255);
                }
            }

            var ctrlTex = new Texture2D(MapTex, MapTex, TextureFormat.RGBA32, true, true) { name = "GroundControl", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            ctrlTex.SetPixels32(control); ctrlTex.Apply(true, false);
            var macroTex = new Texture2D(MapTex, MapTex, TextureFormat.RGB24, true) { name = "GroundMacro", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            macroTex.SetPixels32(macro); macroTex.Apply(true, false);

            var shader = Shader.Find("HuesoOxido/GroundSplat");
            var renderer = ground.GetComponent<MeshRenderer>();
            if (shader == null || !MaterialUtil.UsingScriptablePipeline)
            {
                // Respaldo (sin URP o sin shader): una sola textura con el color del macro.
                renderer.sharedMaterial = MaterialUtil.Lit(new Color(0.42f, 0.46f, 0.28f), 0.05f, 0f, macroTex);
                return;
            }
            var mat = new Material(shader) { name = "GroundSplat" };
            mat.SetTexture("_Control", ctrlTex);
            mat.SetTexture("_Macro", macroTex);
            Set(mat, "_Diff0", "_Nor0", "forest_floor");
            Set(mat, "_Diff1", "_Nor1", "grass");
            Set(mat, "_Diff2", "_Nor2", "road");
            Set(mat, "_Diff3", "_Nor3", "red_earth");
            var rock = TextureLibrary.Get("rock_diff");
            if (rock != null) mat.SetTexture("_DiffRock", rock);
            renderer.sharedMaterial = mat;
        }

        static void Set(Material m, string diffProp, string norProp, string name)
        {
            var d = TextureLibrary.Get(name + "_diff"); if (d != null) m.SetTexture(diffProp, d);
            var n = TextureLibrary.Get(name + "_nor"); if (n != null) m.SetTexture(norProp, n);
        }
    }
}
