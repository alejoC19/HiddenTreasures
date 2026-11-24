using System;

namespace HuesoOxido.Core
{
    /// <summary>Ruido de gradiente 2D determinista (misma semilla = mismos valores) y sin dependencias del motor.</summary>
    public sealed class PerlinNoise
    {
        readonly int[] _p = new int[512];

        public PerlinNoise(int seed)
        {
            var perm = new int[256];
            for (int i = 0; i < 256; i++) perm[i] = i;
            var rng = new Random(seed);
            for (int i = 255; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = perm[i]; perm[i] = perm[j]; perm[j] = t;
            }
            for (int i = 0; i < 512; i++) _p[i] = perm[i & 255];
        }

        static float Fade(float t) => t * t * t * (t * (t * 6f - 15f) + 10f);
        static float Lerp(float a, float b, float t) => a + (b - a) * t;

        static float Grad(int h, float x, float y)
        {
            switch (h & 7)
            {
                case 0: return x + y;
                case 1: return -x + y;
                case 2: return x - y;
                case 3: return -x - y;
                case 4: return x;
                case 5: return -x;
                case 6: return y;
                default: return -y;
            }
        }

        /// <summary>Valor en [0, 1].</summary>
        public float Noise(float x, float y)
        {
            float fx = (float)Math.Floor(x), fy = (float)Math.Floor(y);
            int xi = (int)fx & 255, yi = (int)fy & 255;
            float xf = x - fx, yf = y - fy;
            float u = Fade(xf), v = Fade(yf);
            int aa = _p[_p[xi] + yi], ab = _p[_p[xi] + yi + 1];
            int ba = _p[_p[xi + 1] + yi], bb = _p[_p[xi + 1] + yi + 1];
            float x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1f, yf), u);
            float x2 = Lerp(Grad(ab, xf, yf - 1f), Grad(bb, xf - 1f, yf - 1f), u);
            float val = Lerp(x1, x2, v) * 0.85f;
            return Math.Max(0f, Math.Min(1f, 0.5f + 0.5f * val));
        }

        /// <summary>Ruido fractal normalizado a [0, 1].</summary>
        public float Fbm(float x, float y, int octaves = 3, float lacunarity = 2.1f, float gain = 0.5f)
        {
            float sum = 0f, amp = 1f, norm = 0f, f = 1f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Noise(x * f + i * 17.3f, y * f + i * 9.1f) * amp;
                norm += amp; amp *= gain; f *= lacunarity;
            }
            return sum / norm;
        }
    }
}
