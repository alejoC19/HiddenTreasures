using System;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Mapa de alturas del bosque: colinas suaves, mesetas planas para el campamento y las ruinas, un cinturon de cerros en el
    /// borde (oculta el fin del mundo) y el camino tallado en el terreno. Logica pura: el mismo mundo para la misma semilla.
    /// </summary>
    public sealed class HeightField
    {
        public const float Size = 320f;
        public const int Cells = 160;
        public const int Nodes = Cells + 1;
        public const float Step = Size / Cells;
        public const float Half = Size * 0.5f;

        public readonly float[,] H = new float[Nodes, Nodes]; // [z, x]
        public RoadPath Road { get; private set; }

        static float Smooth(float d, float a, float b)
        {
            float t = Math.Max(0f, Math.Min(1f, (d - a) / (b - a)));
            return t * t * (3f - 2f * t);
        }

        public static HeightField Generate(int seed, float campX, float campZ, float ruinsX, float ruinsZ, RoadPath road)
        {
            var f = new HeightField { Road = road };
            var noise = new PerlinNoise(seed);
            float ox = seed * 0.37f, oz = seed * 0.61f;

            float Base(float x, float z)
            {
                float u = x / Size + 0.5f, v = z / Size + 0.5f;
                float n = noise.Fbm(u * 3.1f + ox, v * 3.1f + oz, 3, 2.2f, 0.5f);
                return 4f + n * 11f;
            }

            float campH = Base(campX, campZ), ruinsH = Base(ruinsX, ruinsZ);
            for (int zi = 0; zi < Nodes; zi++)
            {
                for (int xi = 0; xi < Nodes; xi++)
                {
                    float x = -Half + xi * Step, z = -Half + zi * Step;
                    float h = Base(x, z);
                    float dc = (float)Math.Sqrt((x - campX) * (x - campX) + (z - campZ) * (z - campZ));
                    float dr = (float)Math.Sqrt((x - ruinsX) * (x - ruinsX) + (z - ruinsZ) * (z - ruinsZ));
                    h += (campH - h) * (1f - Smooth(dc, 20f, 48f));
                    h += (ruinsH - h) * (1f - Smooth(dr, 26f, 52f));
                    float edge = Math.Max(Math.Abs(x), Math.Abs(z)) / Half;
                    h += Smooth(edge, 0.80f, 0.98f) * 20f;
                    f.H[zi, xi] = h;
                }
            }
            if (road != null) f.CarveRoad(road);
            return f;
        }

        void CarveRoad(RoadPath road)
        {
            int n = road.Count;
            var center = new float[n];
            for (int i = 0; i < n; i++) center[i] = HeightAt(road.X(i), road.Z(i));
            for (int pass = 0; pass < 3; pass++) // suaviza el perfil para que el camino sea transitable
            {
                var tmp = new float[n];
                for (int i = 0; i < n; i++)
                {
                    float s = 0f; int c = 0;
                    for (int k = -4; k <= 4; k++) { int j = i + k; if (j >= 0 && j < n) { s += center[j]; c++; } }
                    tmp[i] = s / c;
                }
                center = tmp;
            }
            for (int zi = 0; zi < Nodes; zi++)
            {
                for (int xi = 0; xi < Nodes; xi++)
                {
                    float x = -Half + xi * Step, z = -Half + zi * Step;
                    float d = road.DistanceTo(x, z, out int seg, out float t);
                    if (d > 7f) continue;
                    float target = center[seg] + (center[Math.Min(seg + 1, n - 1)] - center[seg]) * t;
                    float w = 1f - Smooth(d, 2.8f, 7f);
                    H[zi, xi] += (target - H[zi, xi]) * w;
                }
            }
        }

        public bool InBounds(float x, float z) => x >= -Half && x <= Half && z >= -Half && z <= Half;

        public float HeightAt(float x, float z)
        {
            float fx = (x + Half) / Step, fz = (z + Half) / Step;
            fx = Math.Max(0f, Math.Min(Cells - 0.0001f, fx));
            fz = Math.Max(0f, Math.Min(Cells - 0.0001f, fz));
            int ix = (int)fx, iz = (int)fz;
            float tx = fx - ix, tz = fz - iz;
            float h00 = H[iz, ix], h10 = H[iz, ix + 1], h01 = H[iz + 1, ix], h11 = H[iz + 1, ix + 1];
            return (h00 * (1f - tx) + h10 * tx) * (1f - tz) + (h01 * (1f - tx) + h11 * tx) * tz;
        }

        /// <summary>Pendiente en grados (diferencias centrales a 1 m).</summary>
        public float SlopeDegrees(float x, float z)
        {
            float dx = (HeightAt(x + 1f, z) - HeightAt(x - 1f, z)) * 0.5f;
            float dz = (HeightAt(x, z + 1f) - HeightAt(x, z - 1f)) * 0.5f;
            return (float)(Math.Atan(Math.Sqrt(dx * dx + dz * dz)) * 180.0 / Math.PI);
        }
    }

    /// <summary>Ubicaciones fijas del diseño del nivel.</summary>
    public static class WorldLayout
    {
        public const float CampX = -70f, CampZ = -70f;
        public const float RuinsX = 55f, RuinsZ = 45f;
        public const int Seed = 1337;

        public static RoadPath CreateRoad(PerlinNoise noise)
            => RoadPath.Create(noise, CampX + 17f, CampZ + 9f, RuinsX - 3f, RuinsZ - 15f);
    }
}
