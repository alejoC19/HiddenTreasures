using System.Collections.Generic;
using HuesoOxido.Collection;
using HuesoOxido.Core;
using HuesoOxido.SaveSystem;
using HuesoOxido.Survival;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class SurvivalTests
    {
        [Test]
        public void Energy_SprintingCostsMoreThanWalking_AndLoadMakesItWorse()
        {
            float walk = EnergyModel.DrainPerSecond(new EnergyContext { Moving = true });
            float sprint = EnergyModel.DrainPerSecond(new EnergyContext { Moving = true, Sprinting = true });
            float loaded = EnergyModel.DrainPerSecond(new EnergyContext { Moving = true, LoadFraction = 1f });
            Assert.Greater(sprint, walk);
            Assert.Greater(loaded, walk);
        }

        [Test]
        public void Energy_ExhaustionFiresOnce()
        {
            var e = new EnergyModel(10f);
            int fired = 0;
            e.Exhausted += () => fired++;
            e.Drain(6f);
            Assert.IsFalse(e.IsExhausted);
            e.Drain(6f);
            e.Drain(1f);
            Assert.IsTrue(e.IsExhausted);
            Assert.AreEqual(1, fired);
            Assert.IsFalse(e.TryConsume(1f));
        }

        [Test]
        public void Clock_WrapsDays()
        {
            var c = new GameClock(23f, 1);
            c.Advance(GameClock.RealSecondsPerDay / 12f); // +2 h
            Assert.AreEqual(1f, c.Hour, 0.01f);
            Assert.AreEqual(2, c.Day);
            Assert.IsTrue(c.IsNight);
        }

        [Test]
        public void Clock_SkipToNextMorningAdvancesDay()
        {
            var c = new GameClock(19f, 3);
            c.SkipTo(7f);
            Assert.AreEqual(4, c.Day);
            Assert.AreEqual(7f, c.Hour, 1e-4f);
        }

        [Test]
        public void Weather_IsDeterministicForSeed()
        {
            var a = new WeatherModel(42);
            var b = new WeatherModel(42);
            for (int i = 0; i < 400; i++) { a.Tick(5f); b.Tick(5f); Assert.AreEqual(a.Current, b.Current); }
        }
    }

    public class CollectionAndSaveTests
    {
        [Test]
        public void Collection_TracksDuplicates()
        {
            var c = new CollectionLog();
            Assert.IsTrue(c.RegisterFind("a").IsNewDiscovery);
            var again = c.RegisterFind("a");
            Assert.IsTrue(again.IsDuplicate);
            Assert.AreEqual(2, again.TimesFound);
            Assert.AreEqual(1, c.DiscoveredCount);
        }

        [Test]
        public void Collection_RestoreReplacesState()
        {
            var c = new CollectionLog();
            c.RegisterFind("x");
            c.Restore(new[] { new CollectionEntry { Id = "y", TimesFound = 3, Displayed = true } });
            Assert.AreEqual(0, c.TimesFound("x"));
            Assert.IsTrue(c.IsDisplayed("y"));
        }

        [Test]
        public void SaveMigrator_FillsMissingListsAndRejectsFutureVersions()
        {
            var old = new SaveData { Backpack = null, Trunk = null, Upgrades = null, Permits = null, Collection = null, ExcavatedSites = null, Player = null, Vehicle = null };
            var m = SaveMigrator.Migrate(old);
            Assert.NotNull(m.Backpack); Assert.NotNull(m.ExcavatedSites); Assert.NotNull(m.Player);
            Assert.AreEqual(SaveData.CurrentVersion, m.Version);
            Assert.IsNull(SaveMigrator.Migrate(new SaveData { Version = SaveData.CurrentVersion + 5 }));
            Assert.IsNull(SaveMigrator.Migrate(null));
        }
    }

    public class LootTests
    {
        [Test]
        public void Loot_FavorsCommonItems()
        {
            var items = new List<IItemInfo>
            {
                new TestItem { Id = "c", Rarity = Rarity.Common },
                new TestItem { Id = "l", Rarity = Rarity.Legendary },
            };
            var rng = new System.Random(1);
            int common = 0;
            for (int i = 0; i < 1000; i++) if (LootTable.Pick(items, rng).Id == "c") common++;
            Assert.Greater(common, 900);
        }
    }
}
