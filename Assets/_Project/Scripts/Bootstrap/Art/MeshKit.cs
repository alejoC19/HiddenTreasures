using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>Generadores de mallas con UV a escala del mundo (la textura mantiene su tamano real sin importar las dimensiones).</summary>
    public static class MeshKit
    {
        /// <summary>Caja con UV proyectadas en metros: las caras usan metersPerTile para repetir la textura.</summary>
        public static Mesh Box(Vector3 size, float metersPerTile)
        {
            float hx = size.x * 0.5f, hy = size.y * 0.5f, hz = size.z * 0.5f;
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            void Face(Vector3 origin, Vector3 u, Vector3 w, Vector3 normal)
            {
                int b = v.Count;
                v.Add(origin); v.Add(origin + u); v.Add(origin + u + w); v.Add(origin + w);
                for (int i = 0; i < 4; i++) n.Add(normal);
                float su = u.magnitude / metersPerTile, sw = w.magnitude / metersPerTile;
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(su, 0)); uv.Add(new Vector2(su, sw)); uv.Add(new Vector2(0, sw));
                t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b); t.Add(b + 3); t.Add(b + 2);
            }
            // winding hacia afuera (sentido horario visto desde fuera)
            Face(new Vector3(-hx, -hy, hz), new Vector3(size.x, 0, 0), new Vector3(0, size.y, 0), Vector3.forward);   // +z
            Face(new Vector3(hx, -hy, -hz), new Vector3(-size.x, 0, 0), new Vector3(0, size.y, 0), Vector3.back);     // -z
            Face(new Vector3(hx, -hy, hz), new Vector3(0, 0, -size.z), new Vector3(0, size.y, 0), Vector3.right);     // +x
            Face(new Vector3(-hx, -hy, -hz), new Vector3(0, 0, size.z), new Vector3(0, size.y, 0), Vector3.left);     // -x
            Face(new Vector3(-hx, hy, -hz), new Vector3(size.x, 0, 0), new Vector3(0, 0, size.z), Vector3.up);        // +y
            Face(new Vector3(-hx, -hy, hz), new Vector3(size.x, 0, 0), new Vector3(0, 0, -size.z), Vector3.down);     // -y
            var m = new Mesh { name = "Box" };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Roca irregular: esfera subdividida con relieve de ruido y base aplanada. UV esfericas a escala de mundo.</summary>
        public static Mesh Rock(int seed, float radius, Vector3 stretch, float metersPerTile = 2.2f)
        {
            var noise = new PerlinNoise(seed);
            var basis = Mesh_Icosphere(3, out var tris);
            var verts = new Vector3[basis.Count];
            var uv = new Vector2[basis.Count];
            for (int i = 0; i < basis.Count; i++)
            {
                Vector3 d = basis[i];
                float bump = noise.Fbm(d.x * 1.6f + 10f, d.y * 1.6f + d.z * 1.3f + 10f, 4, 2.1f, 0.5f);
                float chip = noise.Noise(d.x * 5f + 3f, d.z * 5f + d.y * 4f);
                float r = radius * (0.72f + bump * 0.55f + chip * 0.10f);
                Vector3 p = new Vector3(d.x * stretch.x, d.y * stretch.y, d.z * stretch.z) * r;
                if (p.y < -radius * 0.25f * stretch.y) p.y = -radius * 0.25f * stretch.y; // base plana
                verts[i] = p;
                uv[i] = new Vector2((p.x + p.z * 0.5f) / metersPerTile, (p.y + p.z * 0.3f) / metersPerTile);
            }
            var m = new Mesh { name = "Rock" };
            m.vertices = verts; m.triangles = tris.ToArray(); m.uv = uv;
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static List<Vector3> Mesh_Icosphere(int subdiv, out List<int> triangles)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new List<Vector3> {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) };
            for (int i = 0; i < v.Count; i++) v[i] = v[i].normalized;
            var f = new List<int> { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                                    3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            for (int s = 0; s < subdiv; s++)
            {
                var cache = new Dictionary<long, int>(); var nf = new List<int>();
                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
                    if (cache.TryGetValue(key, out int idx)) return idx;
                    v.Add(((v[a] + v[b]) * 0.5f).normalized); idx = v.Count - 1; cache[key] = idx; return idx;
                }
                for (int i = 0; i < f.Count; i += 3)
                {
                    int a = f[i], b = f[i + 1], c = f[i + 2], ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    nf.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                f = nf;
            }
            triangles = f;
            return v;
        }

        /// <summary>Cilindro cónico (tronco): radio base -> punta, con UV de corteza a escala de mundo.</summary>
        public static Mesh Trunk(float height, float radiusBase, float radiusTop, int sides, int rings, float metersPerTile, float bend, int seed)
        {
            var rng = new System.Random(seed);
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            float lean = (float)(rng.NextDouble() - 0.5) * bend, lean2 = (float)(rng.NextDouble() - 0.5) * bend;
            for (int r = 0; r <= rings; r++)
            {
                float k = r / (float)rings, y = k * height;
                float rad = Mathf.Lerp(radiusBase, radiusTop, Mathf.Pow(k, 0.8f)) * (r == 0 ? 1.12f : 1f);
                float ox = lean * k * k * height, oz = lean2 * k * k * height;
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * rad + ox, y, Mathf.Sin(a) * rad + oz));
                    uv.Add(new Vector2(s / (float)sides * (2f * Mathf.PI * radiusBase) / metersPerTile, y / metersPerTile));
                }
            }
            int row = sides + 1;
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < sides; s++)
                {
                    int a = r * row + s, b = a + 1, c = a + row, d = c + 1;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                }
            var m = new Mesh { name = "Trunk" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }
    }
}
