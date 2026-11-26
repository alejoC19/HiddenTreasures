using HuesoOxido.Survival;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class NeedsModelTests
    {
        static NeedsContext Walk(float ambient = 15f) => new NeedsContext { Moving = true, AmbientC = ambient, Warmth = 0.4f };

        [Test]
        public void StartsFullAndNeutral()
        {
            var n = new NeedsModel();
            Assert.AreEqual(1f, n.Hydration); Assert.AreEqual(1f, n.Satiety); Assert.AreEqual(37f, n.BodyTemperatureC, 1e-4f);
            var e = n.Evaluate();
            Assert.AreEqual(NeedsWarning.None, e.Warnings); Assert.AreEqual(1f, e.SpeedMultiplier); Assert.IsFalse(e.Collapsing);
        }

        [Test]
        public void HeatAndSprintingIncreaseThirst()
        {
            float cool = NeedsModel.HydrationDrainPerHour(Walk(15f));
            float hot = NeedsModel.HydrationDrainPerHour(Walk(36f));
            var sprint = Walk(15f); sprint.Sprinting = true;
            Assert.Greater(hot, cool * 1.5f);
            Assert.Greater(NeedsModel.HydrationDrainPerHour(sprint), cool * 1.9f);
        }

        [Test]
        public void HeatProtectionAndLoadMatter()
        {
            var hotNaked = Walk(36f); var hotCovered = Walk(36f); hotCovered.HeatProtection = 1f;
            Assert.Less(NeedsModel.HydrationDrainPerHour(hotCovered), NeedsModel.HydrationDrainPerHour(hotNaked));
            Assert.Less(NeedsModel.EquilibriumC(hotCovered), NeedsModel.EquilibriumC(hotNaked));
            var heavy = Walk(); heavy.LoadFraction = 1f;
            Assert.Greater(NeedsModel.HydrationDrainPerHour(heavy), NeedsModel.HydrationDrainPerHour(Walk()));
        }

        [Test]
        public void WarmClothesProtectFromCold_AndShelterHelps()
        {
            var light = Walk(-10f); light.Warmth = 0f;
            var warm = Walk(-10f); warm.Warmth = 1f;
            var shelter = Walk(-10f); shelter.Warmth = 1f; shelter.Sheltered = true;
            Assert.Less(NeedsModel.EquilibriumC(light), NeedsModel.EquilibriumC(warm) - 3f);
            Assert.Greater(NeedsModel.EquilibriumC(shelter), NeedsModel.EquilibriumC(warm));
            var wet = Walk(5f); wet.Wet = true;
            Assert.Less(NeedsModel.EquilibriumC(wet), NeedsModel.EquilibriumC(Walk(5f)));
        }

        [Test]
        public void MildForestDay_ShortExpeditionNeedsNoMicromanagement()
        {
            var n = new NeedsModel();
            for (int i = 0; i < 4; i++) n.Tick(1f, Walk(15f));      // 4 horas de caminata
            Assert.Greater(n.Hydration, 0.5f); Assert.Greater(n.Satiety, 0.6f);
            Assert.AreEqual(NeedsWarning.None, n.Evaluate().Warnings);
            Assert.AreEqual(37f, n.BodyTemperatureC, 0.8f);
        }

        [Test]
        public void ThirstEscalatesAndCollapsesWithoutDrinking()
        {
            var n = new NeedsModel(); int collapses = 0; n.Collapsed += () => collapses++;
            var ctx = Walk(30f); ctx.Working = true;
            for (int i = 0; i < 60 && collapses == 0; i++) n.Tick(0.5f, ctx);
            Assert.AreEqual(1, collapses);
            Assert.IsTrue(n.Evaluate().Collapsing);
            n.Drink(0.6f);
            Assert.IsFalse(n.Evaluate().Collapsing);
            n.Tick(0.01f, ctx);
            Assert.AreEqual(1, collapses, "no debe avisar dos veces el mismo colapso");
        }

        [Test]
        public void FreezingSlowsAndDrainsEnergy_ButWarmingRecovers()
        {
            var n = new NeedsModel();
            var ctx = Walk(-20f); ctx.Warmth = 0f; ctx.Wet = true;
            for (int i = 0; i < 12; i++) n.Tick(0.5f, ctx);
            var e = n.Evaluate();
            Assert.IsTrue((e.Warnings & NeedsWarning.Freezing) != 0);
            Assert.Less(e.SpeedMultiplier, 1f); Assert.Greater(e.EnergyDrainMultiplier, 1.4f);
            n.Warm(8f);
            Assert.Greater(n.BodyTemperatureC, NeedsModel.FreezingBodyC);
        }

        [Test]
        public void BodyHasInertia_AShortSoakingDoesNotFreeze()
        {
            var n = new NeedsModel();
            var ctx = Walk(0f); ctx.Wet = true; ctx.Warmth = 0.3f;
            n.Tick(0.1f, ctx);
            Assert.Greater(n.BodyTemperatureC, 36.5f);
        }

        [Test]
        public void ConsumablesClampAndSnapshotRoundTrips()
        {
            var n = new NeedsModel();
            n.Drink(5f); n.Eat(5f);
            Assert.AreEqual(1f, n.Hydration); Assert.AreEqual(1f, n.Satiety);
            n.Tick(2f, Walk(28f));
            var s = n.Snapshot();
            var m = new NeedsModel(); m.Restore(s.hydration, s.satiety, s.bodyC);
            Assert.AreEqual(n.Hydration, m.Hydration, 1e-5f); Assert.AreEqual(n.BodyTemperatureC, m.BodyTemperatureC, 1e-5f);
            m.Restore(2f, -1f, 0f);
            Assert.AreEqual(1f, m.Hydration); Assert.AreEqual(0f, m.Satiety); Assert.AreEqual(37f, m.BodyTemperatureC, 1e-4f);
        }

        [Test]
        public void NeverNegativeOrNaN()
        {
            var n = new NeedsModel();
            n.Tick(0f, Walk()); n.Tick(-3f, Walk());
            for (int i = 0; i < 400; i++) n.Tick(0.7f, new NeedsContext { Sprinting = true, AmbientC = 45f, LoadFraction = 1f });
            Assert.GreaterOrEqual(n.Hydration, 0f); Assert.GreaterOrEqual(n.Satiety, 0f);
            Assert.IsFalse(float.IsNaN(n.BodyTemperatureC));
        }
    }
}
