using System;
using System.Collections.Generic;
using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class BiomeTests
    {
        static HeightField Make(BiomeProfile b, int seed = WorldLayout.Seed)
        {
            var road = WorldLayout.CreateRoad(new PerlinNoise(seed));
            return HeightField.Generate(seed, WorldLayout.CampX, WorldLayout.CampZ, WorldLayout.RuinsX, WorldLayout.RuinsZ, road, b);
        }

        [Test]
        public void Forest_ProfileMatchesLegacyGeneration()
        {
            var road = WorldLayout.CreateRoad(new PerlinNoise(WorldLayout.Seed));
            var legacy = HeightField.Generate(WorldLayout.Seed, WorldLayout.CampX, WorldLayout.CampZ, WorldLayout.RuinsX, WorldLayout.RuinsZ, road);
            var withProfile = Make(Biomes.Forest);
            for (int z = 0; z < HeightField.Nodes; z += 7)
                for (int x = 0; x < HeightField.Nodes; x += 7)
                    Assert.AreEqual(legacy.H[z, x], withProfile.H[z, x], 1e-5f);
        }

        [Test]
        public void Biomes_HaveUniqueIdsZonesAndPermits()
        {
            var ids = new HashSet<string>(); var zones = new HashSet<string>(); var permits = new HashSet<string>(); var prefixes = new HashSet<string>();
            foreach (var b in Biomes.All)
            {
                Assert.IsTrue(ids.Add(b.Id), b.Id); Assert.IsTrue(zones.Add(b.ZoneId), b.ZoneId);
                Assert.IsTrue(permits.Add(b.PermitId), b.PermitId); Assert.IsTrue(prefixes.Add(b.SitePrefix), b.SitePrefix);
                Assert.Greater(b.PermitCost, 0); Assert.IsNotEmpty(b.Name);
            }
            Assert.AreEqual(4, Biomes.All.Count);
        }

        [Test]
        public void ById_FindsByBiomeOrZoneAndFallsBackToForest()
        {
            Assert.AreSame(Biomes.Desert, Biomes.ById("desert_canyon"));
            Assert.AreSame(Biomes.Forest, Biomes.ById("???"));
            Assert.AreSame(Biomes.Forest, Biomes.ById(null));
            Assert.AreSame(Biomes.Snow, Biomes.ByPermit("permit_snow_pines"));
            Assert.IsNull(Biomes.ByPermit("nope"));
        }

        [Test]
        public void EveryBiome_TerrainIsDeterministicFiniteAndCampIsFlatEnough()
        {
            foreach (var b in Biomes.All)
            {
                var a = Make(b); var c = Make(b);
                float min = float.MaxValue, max = float.MinValue;
                for (int z = 0; z < HeightField.Nodes; z++)
                    for (int x = 0; x < HeightField.Nodes; x++)
                    {
                        float h = a.H[z, x];
                        Assert.IsFalse(float.IsNaN(h) || float.IsInfinity(h), b.Id);
                        Assert.AreEqual(h, c.H[z, x], b.Id);
                        min = Math.Min(min, h); max = Math.Max(max, h);
                    }
                Assert.Greater(max - min, 6f, b.Id + " relieve demasiado plano");
                Assert.Less(a.SlopeDegrees(WorldLayout.CampX, WorldLayout.CampZ), 12f, b.Id + " campamento en pendiente");
            }
        }

        [Test]
        public void Desert_ReliefDiffersFromForestAndHasTerraces()
        {
            var f = Make(Biomes.Forest); var d = Make(Biomes.Desert);
            float diff = 0f;
            for (int z = 0; z < HeightField.Nodes; z += 5)
                for (int x = 0; x < HeightField.Nodes; x += 5) diff += Math.Abs(f.H[z, x] - d.H[z, x]);
            Assert.Greater(diff, 100f);
            // Mesetas: hay muchos nodos cerca de multiplos del escalon (el terreno se aplana en los escalones).
            int flat = 0, total = 0;
            for (int z = 4; z < HeightField.Nodes - 4; z += 3)
                for (int x = 4; x < HeightField.Nodes - 4; x += 3)
                {
                    total++;
                    float dx = Math.Abs(d.H[z, x + 1] - d.H[z, x - 1]);
                    if (dx < 0.35f) flat++;
                }
            Assert.Greater(flat / (float)total, 0.3f);
        }
    }
}
