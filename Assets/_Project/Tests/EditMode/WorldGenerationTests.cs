using System;
using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class WorldGenerationTests
    {
        static HeightField Make(int seed = WorldLayout.Seed)
        {
            var road = WorldLayout.CreateRoad(new PerlinNoise(seed));
            return HeightField.Generate(seed, WorldLayout.CampX, WorldLayout.CampZ, WorldLayout.RuinsX, WorldLayout.RuinsZ, road);
        }

        [Test]
        public void Perlin_IsDeterministicAndInRange()
        {
            var a = new PerlinNoise(5); var b = new PerlinNoise(5); var c = new PerlinNoise(6);
            bool differs = false;
            for (int i = 0; i < 200; i++)
            {
                float x = i * 0.37f, y = i * 0.61f;
                float va = a.Fbm(x, y);
                Assert.AreEqual(va, b.Fbm(x, y));
                Assert.That(va, Is.InRange(0f, 1f));
                if (Math.Abs(va - c.Fbm(x, y)) > 1e-4f) differs = true;
            }
            Assert.IsTrue(differs, "semillas distintas deben dar mundos distintos");
        }

        [Test]
        public void HeightField_IsDeterministicFiniteAndBounded()
        {
            var a = Make(); var b = Make();
            for (int z = 0; z < HeightField.Nodes; z += 7)
                for (int x = 0; x < HeightField.Nodes; x += 7)
                {
                    Assert.AreEqual(a.H[z, x], b.H[z, x]);
                    Assert.IsFalse(float.IsNaN(a.H[z, x]) || float.IsInfinity(a.H[z, x]));
                    Assert.That(a.H[z, x], Is.InRange(0f, 45f));
                }
        }

        [Test]
        public void CampAndRuinsAreFlatPlateaus()
        {
            var f = Make();
            foreach (var (cx, cz) in new[] { (WorldLayout.CampX, WorldLayout.CampZ), (WorldLayout.RuinsX, WorldLayout.RuinsZ) })
            {
                float min = float.MaxValue, max = float.MinValue;
                for (float a = 0; a < 6.28f; a += 0.4f)
                    for (float r = 0; r <= 14f; r += 3.5f)
                    {
                        float h = f.HeightAt(cx + (float)Math.Cos(a) * r, cz + (float)Math.Sin(a) * r);
                        min = Math.Min(min, h); max = Math.Max(max, h);
                    }
                Assert.Less(max - min, 0.6f, "la meseta debe ser casi plana para construir encima");
            }
        }

        [Test]
        public void Rim_HidesWorldEdge()
        {
            var f = Make();
            float inner = f.HeightAt(0f, 0f);
            foreach (var (x, z) in new[] { (150f, 0f), (-150f, 20f), (10f, 150f), (-30f, -150f) })
                Assert.Greater(f.HeightAt(x, z), inner + 6f, "el borde debe tener cerros que tapen el fin del mundo");
        }

        [Test]
        public void Road_ConnectsCampAndRuins_WithDrivableSlopes()
        {
            var f = Make();
            var r = f.Road;
            Assert.Greater(r.Count, 40);
            Assert.Less(r.DistanceTo(WorldLayout.CampX + 17f, WorldLayout.CampZ + 9f), 0.5f);
            Assert.Less(r.DistanceTo(WorldLayout.RuinsX - 3f, WorldLayout.RuinsZ - 15f), 0.5f);
            float maxSlope = 0f;
            for (int i = 0; i < r.Count; i++)
            {
                if (i > 0)
                {
                    float dx = r.X(i) - r.X(i - 1), dz = r.Z(i) - r.Z(i - 1);
                    Assert.Less(Math.Sqrt(dx * dx + dz * dz), 3.2, "puntos del camino demasiado separados");
                }
                maxSlope = Math.Max(maxSlope, f.SlopeDegrees(r.X(i), r.Z(i)));
                Assert.IsTrue(f.InBounds(r.X(i), r.Z(i)));
            }
            Assert.Less(maxSlope, 22f, "el camino debe poder recorrerse en vehiculo");
        }

        [Test]
        public void RoadDistance_IsZeroOnPathAndGrowsAway()
        {
            var r = Make().Road;
            int mid = r.Count / 2;
            Assert.Less(r.DistanceTo(r.X(mid), r.Z(mid)), 0.01f);
            Assert.Greater(r.DistanceTo(r.X(mid) + 40f, r.Z(mid) - 40f), 5f);
        }
    }
}
