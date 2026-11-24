using System;
using System.Collections.Generic;

namespace HuesoOxido.Core
{
    /// <summary>Camino de tierra como polilinea densa (puntos cada ~2 m) entre dos puntos, con curvas suaves. Logica pura.</summary>
    public sealed class RoadPath
    {
        public const float Step = 2f;
        readonly List<float> _x = new List<float>();
        readonly List<float> _z = new List<float>();

        public int Count => _x.Count;
        public float X(int i) => _x[i];
        public float Z(int i) => _z[i];

        public static RoadPath Create(PerlinNoise noise, float sx, float sz, float ex, float ez, int waypoints = 5, float lateral = 22f)
        {
            var ctrlX = new List<float> { sx };
            var ctrlZ = new List<float> { sz };
            float dx = ex - sx, dz = ez - sz, len = (float)Math.Sqrt(dx * dx + dz * dz);
            float px = -dz / len, pz = dx / len; // perpendicular
            for (int k = 1; k <= waypoints; k++)
            {
                float t = k / (float)(waypoints + 1);
                float off = (noise.Noise(t * 3.7f + 11f, 5.5f) * 2f - 1f) * lateral * (float)Math.Sin(Math.PI * t);
                ctrlX.Add(sx + dx * t + px * off);
                ctrlZ.Add(sz + dz * t + pz * off);
            }
            ctrlX.Add(ex); ctrlZ.Add(ez);

            var road = new RoadPath();
            int n = ctrlX.Count;
            for (int i = 0; i < n - 1; i++)
            {
                int i0 = Math.Max(0, i - 1), i1 = i, i2 = i + 1, i3 = Math.Min(n - 1, i + 2);
                float segLen = (float)Math.Sqrt((ctrlX[i2] - ctrlX[i1]) * (ctrlX[i2] - ctrlX[i1]) + (ctrlZ[i2] - ctrlZ[i1]) * (ctrlZ[i2] - ctrlZ[i1]));
                int steps = Math.Max(2, (int)Math.Ceiling(segLen / Step));
                for (int s = 0; s < steps; s++)
                {
                    float t = s / (float)steps;
                    road._x.Add(CatmullRom(ctrlX[i0], ctrlX[i1], ctrlX[i2], ctrlX[i3], t));
                    road._z.Add(CatmullRom(ctrlZ[i0], ctrlZ[i1], ctrlZ[i2], ctrlZ[i3], t));
                }
            }
            road._x.Add(ex); road._z.Add(ez);
            return road;
        }

        static float CatmullRom(float p0, float p1, float p2, float p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>Distancia al camino; devuelve ademas el segmento mas cercano y la posicion t (0..1) sobre el.</summary>
        public float DistanceTo(float x, float z, out int segment, out float t)
        {
            float best = float.MaxValue; segment = 0; t = 0f;
            for (int i = 0; i < _x.Count - 1; i++)
            {
                float ax = _x[i], az = _z[i], bx = _x[i + 1], bz = _z[i + 1];
                float vx = bx - ax, vz = bz - az;
                float l2 = vx * vx + vz * vz;
                float u = l2 < 1e-6f ? 0f : ((x - ax) * vx + (z - az) * vz) / l2;
                u = u < 0f ? 0f : (u > 1f ? 1f : u);
                float cx = ax + vx * u - x, cz = az + vz * u - z;
                float d2 = cx * cx + cz * cz;
                if (d2 < best) { best = d2; segment = i; t = u; }
            }
            return (float)Math.Sqrt(best);
        }

        public float DistanceTo(float x, float z) => DistanceTo(x, z, out _, out _);
    }
}
