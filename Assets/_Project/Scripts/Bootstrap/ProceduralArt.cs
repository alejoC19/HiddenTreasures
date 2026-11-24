using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>Texturas y mallas procedurales para placeholders (hasta integrar assets definitivos).</summary>
    public static class ProceduralArt
    {
        public static Texture2D NoiseTexture(int size, Color a, Color b, float scale, int seed, float contrast = 1f)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var px = new Color[size * size];
            float ox = seed * 17.31f, oy = seed * 9.77f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size, v = y / (float)size;
                    float n = Mathf.PerlinNoise(ox + u * scale, oy + v * scale) * 0.6f
                            + Mathf.PerlinNoise(ox + u * scale * 3.1f, oy + v * scale * 3.1f) * 0.3f
                            + Mathf.PerlinNoise(ox + u * scale * 9f, oy + v * scale * 9f) * 0.1f;
                    n = Mathf.Clamp01((n - 0.5f) * contrast + 0.5f);
                    px[y * size + x] = Color.Lerp(a, b, n);
                }
            }
            tex.SetPixels(px);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply(true);
            return tex;
        }

        /// <summary>Cono de base 1 y altura 1 con caras planas (aspecto de conifera facetada).</summary>
        public static Mesh Cone(int sides)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                int b = verts.Count;
                verts.Add(new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)));
                verts.Add(new Vector3(0f, 1f, 0f));
                verts.Add(new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)));
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            }
            var m = new Mesh { name = "Cone" };
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public static Mesh PrimitiveMesh(PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            return mesh;
        }
    }
}
