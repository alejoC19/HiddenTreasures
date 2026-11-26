using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    public sealed class TreeVariant
    {
        public Mesh Trunk, Foliage;
        public float Height, TrunkRadius;
    }

    /// <summary>Arboles procedurales: troncos con corteza a escala real y follaje en tarjetas de ramas con hojas pintadas.</summary>
    public static class TreeFactory
    {
        sealed class Buf
        {
            public readonly List<Vector3> V = new List<Vector3>(); public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Vector2> UV = new List<Vector2>(); public readonly List<int> T = new List<int>();
            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name, indexFormat = V.Count > 60000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                m.SetVertices(V); m.SetNormals(N); m.SetUVs(0, UV); m.SetTriangles(T, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        /// <summary>Segmento de tronco/rama (cilindro conico) entre dos puntos, con UV a escala de mundo.</summary>
        static void Limb(Buf b, Vector3 p0, Vector3 p1, float r0, float r1, int sides, float mpt)
        {
            Vector3 dir = (p1 - p0).normalized;
            Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.95f ? Vector3.right : Vector3.up).normalized;
            Vector3 up = Vector3.Cross(side, dir);
            float len = (p1 - p0).magnitude;
            int baseIdx = b.V.Count;
            for (int ring = 0; ring < 2; ring++)
            {
                Vector3 c = ring == 0 ? p0 : p1; float r = ring == 0 ? r0 : r1;
                for (int s = 0; s <= sides; s++)
                {
                    float a = s / (float)sides * Mathf.PI * 2f;
                    Vector3 n = side * Mathf.Cos(a) + up * Mathf.Sin(a);
                    b.V.Add(c + n * r); b.N.Add(n);
                    b.UV.Add(new Vector2(s / (float)sides * (2f * Mathf.PI * Mathf.Max(r0, 0.02f)) / mpt, (ring == 0 ? 0f : len) / mpt));
                }
            }
            int row = sides + 1;
            for (int s = 0; s < sides; s++)
            {
                int a = baseIdx + s, c = a + row;
                b.T.Add(a); b.T.Add(c); b.T.Add(a + 1); b.T.Add(a + 1); b.T.Add(c); b.T.Add(c + 1);
            }
        }

        /// <summary>Tarjeta curva de follaje: 3 filas (base, medio, punta) con caida, ancho que se afina y normales redondeadas hacia afuera.</summary>
        static void Card(Buf b, Vector3 root, Vector3 dir, Vector3 side, float length, float width, float droop, Vector3 normalCenter, float normalUp)
        {
            int baseIdx = b.V.Count;
            for (int row = 0; row < 3; row++)
            {
                float k = row * 0.5f;
                Vector3 c = root + dir * (length * k) + Vector3.down * (droop * k * k);
                float w = width * (row == 0 ? 0.30f : (row == 1 ? 1f : 0.42f)) * 0.5f;
                for (int col = 0; col < 2; col++)
                {
                    Vector3 p = c + side * (col == 0 ? -w : w);
                    Vector3 outward = p - normalCenter; outward.y *= 0.4f;
                    Vector3 n = (outward.normalized + Vector3.up * normalUp).normalized;
                    b.V.Add(p); b.N.Add(n); b.UV.Add(new Vector2(col, k));
                }
            }
            for (int row = 0; row < 2; row++)
            {
                int a = baseIdx + row * 2;
                b.T.Add(a); b.T.Add(a + 2); b.T.Add(a + 1); b.T.Add(a + 1); b.T.Add(a + 2); b.T.Add(a + 3);
            }
        }

        /// <param name="detail">1 = modelo completo; &lt;1 = version simple para lejos (menos ramas y lados, tarjetas mas anchas).</param>
        public static TreeVariant Conifer(int seed, float height, float detail = 1f)
        {
            int sides = detail < 1f ? 5 : 9;
            float sizeK = Mathf.Clamp(1f / Mathf.Sqrt(detail), 1f, 1.8f);
            var rng = new System.Random(seed);
            var trunk = new Buf(); var fol = new Buf();
            float lean = ((float)rng.NextDouble() - 0.5f) * 0.05f * height;
            float baseR = height * 0.026f;
            // Tronco recto con leve inclinacion, en 5 tramos que se afinan.
            Vector3 prev = Vector3.zero;
            for (int i = 1; i <= 6; i++)
            {
                float k = i / 6f;
                Vector3 p = new Vector3(lean * k * k, height * 0.99f * k, lean * 0.4f * k * k);
                Limb(trunk, prev, p, Mathf.Lerp(baseR, 0.03f, Mathf.Pow((i - 1) / 6f, 0.8f)) * (i == 1 ? 1.15f : 1f), Mathf.Lerp(baseR, 0.03f, Mathf.Pow(k, 0.8f)), sides, 1.4f);
                prev = p;
            }
            // Raices expuestas (pequenas) para romper la base recta.
            for (int r = 0; r < 4; r++)
            {
                float a = r * Mathf.PI * 0.5f + (float)rng.NextDouble();
                Limb(trunk, new Vector3(Mathf.Cos(a) * baseR * 0.6f, 0.25f, Mathf.Sin(a) * baseR * 0.6f), new Vector3(Mathf.Cos(a) * baseR * 2.1f, -0.1f, Mathf.Sin(a) * baseR * 2.1f), baseR * 0.45f, baseR * 0.15f, 5, 1.4f);
            }

            int whorls = Mathf.Max(6, Mathf.RoundToInt(16 * detail)); float golden = 2.39996f; float yaw0 = (float)rng.NextDouble() * 6.28f;
            Vector3 center = new Vector3(0, height * 0.55f, 0);
            for (int w = 0; w < whorls; w++)
            {
                float t = w / (float)(whorls - 1);
                float y = Mathf.Lerp(height * 0.16f, height * 0.97f, t);
                int nb = Mathf.RoundToInt(Mathf.Lerp(9f, 4f, t));
                float Lb = height * 0.30f * Mathf.Pow(1f - t, 0.85f) + 0.55f;
                float elev = Mathf.Lerp(-0.18f, 0.55f, t);
                float droop = Lb * Mathf.Lerp(0.42f, 0.10f, t);
                for (int i = 0; i < nb; i++)
                {
                    float yaw = yaw0 + w * golden + i * (Mathf.PI * 2f / nb) + ((float)rng.NextDouble() - 0.5f) * 0.4f;
                    Vector3 dir = new Vector3(Mathf.Cos(yaw) * Mathf.Cos(elev), Mathf.Sin(elev), Mathf.Sin(yaw) * Mathf.Cos(elev)).normalized;
                    Vector3 root = new Vector3(lean * (y / height) * (y / height), y, lean * 0.4f * (y / height) * (y / height)) + dir * 0.10f;
                    Vector3 side = Vector3.Cross(Vector3.up, dir).normalized;
                    for (int c = 0; c < 2; c++)
                    {
                        float roll = (c == 0 ? 1f : -1f) * (0.35f + (float)rng.NextDouble() * 0.2f);
                        Vector3 s2 = Quaternion.AngleAxis(roll * Mathf.Rad2Deg, dir) * side;
                        Card(fol, root, dir, s2, Lb, Lb * 0.66f * sizeK, droop, center, 0.45f);
                    }
                }
            }
            // Punta: tarjetas verticales cruzadas.
            Vector3 top = new Vector3(lean, height * 0.96f, lean * 0.4f);
            for (int c = 0; c < 3; c++)
                Card(fol, top, Vector3.up, Quaternion.AngleAxis(c * 60f, Vector3.up) * Vector3.right, height * 0.13f, height * 0.06f, 0f, center, 0.9f);

            return new TreeVariant { Trunk = trunk.ToMesh("ConiferTrunk" + seed), Foliage = fol.ToMesh("ConiferFoliage" + seed), Height = height, TrunkRadius = baseR };
        }

        public static TreeVariant Broadleaf(int seed, float height, float detail = 1f)
        {
            int sides = detail < 1f ? 6 : 10;
            float sizeK = Mathf.Clamp(1f / Mathf.Sqrt(detail), 1f, 1.8f);
            var rng = new System.Random(seed);
            var trunk = new Buf(); var fol = new Buf();
            float baseR = height * 0.032f;
            float stemH = height * 0.46f;
            float lean = ((float)rng.NextDouble() - 0.5f) * 0.08f * height;
            Vector3 prev = Vector3.zero;
            for (int i = 1; i <= 4; i++)
            {
                float k = i / 4f;
                Vector3 p = new Vector3(lean * k * k, stemH * k, 0);
                Limb(trunk, prev, p, Mathf.Lerp(baseR, baseR * 0.7f, (i - 1) / 4f) * (i == 1 ? 1.18f : 1f), Mathf.Lerp(baseR, baseR * 0.7f, k), sides, 1.4f);
                prev = p;
            }
            Vector3 crown = new Vector3(lean, stemH, 0);
            Vector3 center = crown + Vector3.up * height * 0.22f;
            float rx = height * 0.36f, ry = height * 0.30f;
            // Ramas principales hacia el follaje.
            int limbs = 5;
            var tips = new List<Vector3>();
            for (int i = 0; i < limbs; i++)
            {
                float a = i / (float)limbs * Mathf.PI * 2f + (float)rng.NextDouble() * 0.6f;
                Vector3 mid = crown + new Vector3(Mathf.Cos(a) * rx * 0.30f, height * 0.10f, Mathf.Sin(a) * rx * 0.30f);
                Vector3 tip = center + new Vector3(Mathf.Cos(a) * rx * 0.62f, ((float)rng.NextDouble() - 0.3f) * ry * 0.7f, Mathf.Sin(a) * rx * 0.62f);
                Limb(trunk, crown, mid, baseR * 0.55f, baseR * 0.38f, 6, 1.4f);
                Limb(trunk, mid, tip, baseR * 0.38f, baseR * 0.10f, 5, 1.4f);
                tips.Add(tip);
            }
            // Racimos de hojas: pares de tarjetas cruzadas dentro de un elipsoide.
            int clusters = Mathf.Max(18, Mathf.RoundToInt(70 * detail));
            for (int i = 0; i < clusters; i++)
            {
                Vector3 p;
                if (i < tips.Count * 3) p = tips[i % tips.Count] + new Vector3(((float)rng.NextDouble() - 0.5f) * height * 0.22f, ((float)rng.NextDouble() - 0.3f) * height * 0.14f, ((float)rng.NextDouble() - 0.5f) * height * 0.22f);
                else
                {
                    float u = (float)rng.NextDouble() * Mathf.PI * 2f, v = Mathf.Acos(2f * (float)rng.NextDouble() - 1f), r = Mathf.Pow((float)rng.NextDouble(), 0.45f);
                    p = center + new Vector3(Mathf.Sin(v) * Mathf.Cos(u) * rx * r, Mathf.Cos(v) * ry * r, Mathf.Sin(v) * Mathf.Sin(u) * rx * r);
                }
                float size = height * (0.13f + (float)rng.NextDouble() * 0.06f) * sizeK;
                float yaw = (float)rng.NextDouble() * 360f, tilt = ((float)rng.NextDouble() - 0.5f) * 50f;
                for (int c = 0; c < 2; c++)
                {
                    Quaternion q = Quaternion.Euler(tilt, yaw + c * 90f, ((float)rng.NextDouble() - 0.5f) * 30f);
                    Vector3 dir = q * Vector3.up, side = q * Vector3.right;
                    Card(fol, p - dir * size * 0.5f, dir, side, size, size, 0f, center, 0.35f);
                }
            }
            return new TreeVariant { Trunk = trunk.ToMesh("BroadTrunk" + seed), Foliage = fol.ToMesh("BroadFoliage" + seed), Height = height, TrunkRadius = baseR };
        }

        /// <summary>Cactus saguaro: columna con puntas redondeadas y 1 a 3 brazos que salen y suben. Todo en la malla del tronco.</summary>
        public static TreeVariant Cactus(int seed, float height, float detail = 1f)
        {
            var rng = new System.Random(seed);
            var trunk = new Buf();
            int sides = detail < 1f ? 6 : 10;
            float r = height * 0.065f;
            Column(trunk, Vector3.zero, height, r, sides);
            int arms = 1 + rng.Next(3);
            for (int a = 0; a < arms; a++)
            {
                float yaw = (float)rng.NextDouble() * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                float y = height * (0.32f + (float)rng.NextDouble() * 0.3f);
                float reach = height * (0.22f + (float)rng.NextDouble() * 0.12f), rise = height * (0.22f + (float)rng.NextDouble() * 0.2f);
                float ar = r * 0.62f;
                Vector3 p0 = dir * (r * 0.6f) + Vector3.up * y, p1 = dir * (r + reach) + Vector3.up * y;
                Limb(trunk, p0, p1, ar, ar, sides, 1.4f);
                Column(trunk, p1, rise, ar, sides);
            }
            return new TreeVariant { Trunk = trunk.ToMesh("Cactus" + seed), Foliage = new Mesh { name = "Empty" }, Height = height, TrunkRadius = r };
        }

        static void Column(Buf b, Vector3 baseP, float h, float r, int sides)
        {
            const int segs = 5;
            Vector3 prev = baseP; float prevR = r * 0.95f;
            for (int i = 1; i <= segs; i++)
            {
                float k = i / (float)segs;
                float rr = i == segs ? r * 0.45f : r * (1f - 0.1f * k);
                Vector3 p = baseP + Vector3.up * (h * (i == segs ? 1.0f : k * 0.97f));
                Limb(b, prev, p, prevR, rr, sides, 1.4f);
                prev = p; prevR = rr;
            }
            Limb(b, prev, prev + Vector3.up * r * 0.25f, prevR, 0.02f, sides, 1.4f);
        }

        /// <summary>Arbol muerto: tronco retorcido y ramas peladas en tres niveles, sin follaje (pantano, bosque seco).</summary>
        public static TreeVariant DeadTree(int seed, float height, float detail = 1f)
        {
            var rng = new System.Random(seed);
            var trunk = new Buf();
            int sides = detail < 1f ? 5 : 8;
            float baseR = height * 0.04f;
            float lean = ((float)rng.NextDouble() - 0.5f) * 0.18f * height;
            Vector3 prev = Vector3.zero;
            var points = new List<Vector3>();
            const int segs = 6;
            for (int i = 1; i <= segs; i++)
            {
                float k = i / (float)segs;
                var p = new Vector3(lean * k * k + Mathf.Sin(k * 5f + seed) * 0.12f, height * 0.8f * k, Mathf.Cos(k * 4f + seed) * 0.12f);
                Limb(trunk, prev, p, Mathf.Lerp(baseR, baseR * 0.3f, (i - 1) / (float)segs), Mathf.Lerp(baseR, baseR * 0.3f, k), sides, 1.4f);
                points.Add(p); prev = p;
            }
            int branches = Mathf.Max(5, Mathf.RoundToInt(11 * detail));
            for (int i = 0; i < branches; i++)
            {
                int at = 1 + rng.Next(points.Count - 1);
                Vector3 root = points[at];
                float yaw = (float)rng.NextDouble() * Mathf.PI * 2f;
                float len = height * (0.16f + (float)rng.NextDouble() * 0.2f) * (1f - at * 0.08f);
                Vector3 dir = new Vector3(Mathf.Cos(yaw), 0.35f + (float)rng.NextDouble() * 0.5f, Mathf.Sin(yaw)).normalized;
                float br = baseR * 0.38f * (1f - at * 0.1f);
                Vector3 mid = root + dir * len * 0.55f, tip = mid + (dir + new Vector3(((float)rng.NextDouble() - 0.5f) * 0.6f, 0.3f, ((float)rng.NextDouble() - 0.5f) * 0.6f)).normalized * len * 0.5f;
                Limb(trunk, root, mid, br, br * 0.6f, 5, 1.4f);
                Limb(trunk, mid, tip, br * 0.6f, 0.02f, 4, 1.4f);
                if (detail >= 1f && i % 2 == 0)
                {
                    Vector3 twig = mid + new Vector3(((float)rng.NextDouble() - 0.5f), 0.4f, ((float)rng.NextDouble() - 0.5f)).normalized * len * 0.35f;
                    Limb(trunk, mid, twig, br * 0.4f, 0.015f, 4, 1.4f);
                }
            }
            Limb(trunk, prev, prev + Vector3.up * height * 0.12f, baseR * 0.3f, 0.02f, 5, 1.4f);
            return new TreeVariant { Trunk = trunk.ToMesh("DeadTree" + seed), Foliage = new Mesh { name = "Empty" }, Height = height, TrunkRadius = baseR };
        }
    }
}
