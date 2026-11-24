using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Pinta por codigo las texturas de follaje (agujas de conifera, racimos de hojas, pasto) con transparencia.
    /// Se usan en tarjetas con recorte alfa: la misma tecnica que los arboles de los juegos, sin descargar modelos pesados.
    /// </summary>
    public static class FoliageTextures
    {
        const int S = 256;

        sealed class Canvas
        {
            public readonly int W, H;
            public readonly Color32[] P;
            public Canvas(int w, int h, Color32 bleed) { W = w; H = h; P = new Color32[w * h]; for (int i = 0; i < P.Length; i++) P[i] = new Color32(bleed.r, bleed.g, bleed.b, 0); }

            public void Plot(int x, int y, float cover, Color32 c)
            {
                if (x < 0 || y < 0 || x >= W || y >= H || cover <= 0f) return;
                ref Color32 d = ref P[y * W + x];
                float a = Mathf.Clamp01(cover);
                if (a * 255f < d.a) { return; }
                d = new Color32(c.r, c.g, c.b, (byte)(a * 255f));
            }

            public void Dot(float cx, float cy, float r, Color32 c)
            {
                int x0 = (int)(cx - r - 1), x1 = (int)(cx + r + 1), y0 = (int)(cy - r - 1), y1 = (int)(cy + r + 1);
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                        Plot(x, y, r + 0.5f - d, c);
                    }
            }

            public void Line(float x0, float y0, float x1, float y1, float w0, float w1, Color32 c0, Color32 c1)
            {
                float len = Mathf.Max(1f, Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0)));
                int steps = (int)(len * 1.5f) + 1;
                for (int i = 0; i <= steps; i++)
                {
                    float t = i / (float)steps;
                    Color32 c = Color32.Lerp(c0, c1, t);
                    Dot(Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), Mathf.Lerp(w0, w1, t) * 0.5f, c);
                }
            }

            public Texture2D ToTexture(string name)
            {
                var t = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
                t.SetPixels32(P); t.Apply(true, false);
                return t;
            }
        }

        static Color32 Rand(System.Random r, Color32 a, Color32 b)
        {
            float t = (float)r.NextDouble();
            return Color32.Lerp(a, b, t);
        }

        /// <summary>Ramita de conifera: tallo central y agujas a ambos lados, mas largas en la base y finas en la punta.</summary>
        public static Texture2D Conifer(int seed)
        {
            var rng = new System.Random(seed);
            var cv = new Canvas(S, S, new Color32(40, 85, 40, 0));
            var stem0 = new Color32(70, 50, 30, 255); var stem1 = new Color32(60, 80, 40, 255);
            cv.Line(S * 0.5f, 2, S * 0.5f, S - 6, 4.2f, 1.6f, stem0, stem1);
            var dark = new Color32(22, 56, 28, 255); var mid = new Color32(40, 92, 40, 255); var light = new Color32(86, 140, 64, 255);
            int count = 170;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float y = 6f + t * (S - 20f);
                for (int side = -1; side <= 1; side += 2)
                {
                    float ang = Mathf.Deg2Rad * Mathf.Lerp(62f, 28f, t) + (float)(rng.NextDouble() - 0.5) * 0.35f;
                    float len = (1f - t * 0.80f) * 104f * (0.75f + (float)rng.NextDouble() * 0.5f);
                    float dx = side * Mathf.Sin(ang), dy = Mathf.Cos(ang);
                    float x1 = S * 0.5f + dx * len, y1 = y + dy * len * 0.55f;
                    Color32 c1 = Rand(rng, mid, light);
                    cv.Line(S * 0.5f, y, x1, y1, 3.0f, 1.0f, Rand(rng, dark, mid), c1);
                    // agujas secundarias (volumen)
                    if (i % 2 == 0)
                    {
                        float mx = Mathf.Lerp(S * 0.5f, x1, 0.55f), my = Mathf.Lerp(y, y1, 0.55f);
                        cv.Line(mx, my, mx + dx * len * 0.32f, my + 16f, 2.4f, 0.8f, mid, c1);
                    }
                }
            }
            return cv.ToTexture("Conifer_" + seed);
        }

        /// <summary>Racimo de hojas ovaladas con nervadura central, sobre ramitas finas.</summary>
        public static Texture2D LeafCluster(int seed)
        {
            var rng = new System.Random(seed);
            var cv = new Canvas(S, S, new Color32(70, 110, 45, 0));
            var twig = new Color32(80, 60, 40, 255);
            for (int i = 0; i < 6; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                cv.Line(S * 0.5f, S * 0.5f, S * 0.5f + Mathf.Cos(a) * 100f, S * 0.5f + Mathf.Sin(a) * 100f, 3f, 1f, twig, twig);
            }
            for (int i = 0; i < 150; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Sqrt((float)rng.NextDouble()) * 106f;
                float cx = S * 0.5f + Mathf.Cos(a) * r, cy = S * 0.5f + Mathf.Sin(a) * r;
                float rot = (float)rng.NextDouble() * Mathf.PI * 2f;
                float la = 12f + (float)rng.NextDouble() * 8f, lb = 5f + (float)rng.NextDouble() * 4f;
                Color32 baseC = Rand(rng, new Color32(58, 100, 36, 255), new Color32(104, 150, 54, 255));
                Color32 veinC = new Color32((byte)Mathf.Min(255, baseC.r + 40), (byte)Mathf.Min(255, baseC.g + 36), (byte)Mathf.Min(255, baseC.b + 20), 255);
                float cr = Mathf.Cos(rot), sr = Mathf.Sin(rot);
                int ext = (int)(la + 2);
                for (int y = (int)cy - ext; y <= (int)cy + ext; y++)
                    for (int x = (int)cx - ext; x <= (int)cx + ext; x++)
                    {
                        float px = x + 0.5f - cx, py = y + 0.5f - cy;
                        float lx = px * cr + py * sr, ly = -px * sr + py * cr;
                        // forma de hoja: ancho maximo al 40% del largo
                        float u = lx / la;
                        if (u < -1f || u > 1f) continue;
                        float prof = lb * Mathf.Sqrt(Mathf.Max(0f, 1f - u * u)) * (1f - 0.25f * u);
                        float cover = prof + 0.5f - Mathf.Abs(ly);
                        if (cover <= 0f) continue;
                        bool vein = Mathf.Abs(ly) < 0.9f;
                        cv.Plot(x, y, cover, vein ? veinC : Color32.Lerp(baseC, veinC, ly > 0 ? 0.0f : 0.25f));
                    }
            }
            return cv.ToTexture("LeafCluster_" + seed);
        }

        /// <summary>Mata de pasto: hojas finas y curvas, oscuras en la base y claras en la punta.</summary>
        public static Texture2D Grass(int seed)
        {
            var rng = new System.Random(seed);
            const int W = 128, H = 256;
            var cv = new Canvas(W, H, new Color32(70, 110, 40, 0));
            for (int i = 0; i < 26; i++)
            {
                float x0 = 14f + (float)rng.NextDouble() * (W - 28f);
                float h = 110f + (float)rng.NextDouble() * 140f;
                float bend = ((float)rng.NextDouble() - 0.5f) * 70f;
                Color32 b0 = Rand(rng, new Color32(30, 60, 20, 255), new Color32(50, 84, 28, 255));
                Color32 b1 = Rand(rng, new Color32(110, 150, 60, 255), new Color32(170, 175, 80, 255));
                int seg = 14; float px = x0, py = 1;
                for (int s = 1; s <= seg; s++)
                {
                    float t = s / (float)seg;
                    float nx = x0 + bend * t * t, ny = h * t;
                    cv.Line(px, py, nx, ny, Mathf.Lerp(5f, 0.8f, t), Mathf.Lerp(5f, 0.8f, t + 0.07f), Color32.Lerp(b0, b1, t - 0.07f), Color32.Lerp(b0, b1, t));
                    px = nx; py = ny;
                }
            }
            return cv.ToTexture("Grass_" + seed);
        }

        /// <summary>Helecho: tallos curvos con foliolos a ambos lados.</summary>
        public static Texture2D Fern(int seed)
        {
            var rng = new System.Random(seed);
            var cv = new Canvas(S, S, new Color32(60, 110, 40, 0));
            for (int f = 0; f < 5; f++)
            {
                float ang = Mathf.Lerp(-55f, 55f, f / 4f) * Mathf.Deg2Rad + (float)(rng.NextDouble() - 0.5) * 0.2f;
                float len = 190f + (float)rng.NextDouble() * 50f;
                float sx = S * 0.5f, sy = 4f; float px = sx, py = sy;
                int seg = 22;
                for (int s = 1; s <= seg; s++)
                {
                    float t = s / (float)seg;
                    float a = ang * (0.45f + t * 0.9f);
                    float nx = px + Mathf.Sin(a) * len / seg, ny = py + Mathf.Cos(a) * len / seg * 0.95f;
                    cv.Line(px, py, nx, ny, 2.6f * (1f - t * 0.6f), 2f * (1f - t * 0.6f), new Color32(60, 90, 40, 255), new Color32(80, 120, 50, 255));
                    float leaf = (1f - t) * 46f * (0.6f + t * 0.5f) + 8f;
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float la = a + side * 1.15f;
                        Color32 c1 = Rand(rng, new Color32(54, 104, 40, 255), new Color32(110, 160, 66, 255));
                        cv.Line(nx, ny, nx + Mathf.Sin(la) * leaf, ny + Mathf.Cos(la) * leaf * 0.7f, 3.4f, 0.8f, new Color32(40, 84, 34, 255), c1);
                    }
                    px = nx; py = ny;
                }
            }
            return cv.ToTexture("Fern_" + seed);
        }

        /// <summary>Flor silvestre blanca (se tine con el color del material): tallo y cabeza de petalos con centro amarillo.</summary>
        public static Texture2D Flower()
        {
            var cv = new Canvas(64, 128, new Color32(120, 140, 70, 0));
            cv.Line(32, 2, 33, 84, 3.2f, 2f, new Color32(50, 90, 36, 255), new Color32(70, 110, 44, 255));
            cv.Line(33, 30, 46, 50, 2.4f, 1.2f, new Color32(60, 100, 40, 255), new Color32(80, 120, 50, 255));
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                cv.Dot(33 + Mathf.Cos(a) * 11f, 96 + Mathf.Sin(a) * 11f, 7.5f, new Color32(255, 255, 255, 255));
            }
            cv.Dot(33, 96, 5.5f, new Color32(255, 214, 70, 255));
            return cv.ToTexture("Flower");
        }
    }
}
