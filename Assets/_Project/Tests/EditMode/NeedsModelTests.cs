using HuesoOxido.Core;
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

        static float Seconds(float s) => NeedsModel.GameHoursFromSeconds(s);

        [Test]
        public void ClockConversion_MatchesTheGameClock() => Assert.AreEqual(1f, NeedsModel.GameHoursFromSeconds(50f), 1e-4f);

        [Test]
        public void MildForestWalk_DesignTargets_NoMicromanagement()
        {
            // Caminando sin parar en un dia templado: sin avisos en los primeros 12 min, sed hacia los 15-20, colapso no antes de 22 min.
            var n = new NeedsModel(); float thirstyAt = -1f, collapseAt = -1f;
            for (int s = 0; s < 3600; s += 5)
            {
                n.Tick(Seconds(5f), Walk(15f));
                if (thirstyAt < 0 && (n.Evaluate().Warnings & NeedsWarning.Thirsty) != 0) thirstyAt = s / 60f;
                if (collapseAt < 0 && n.Evaluate().Collapsing) collapseAt = s / 60f;
            }
            Assert.That(thirstyAt, Is.InRange(17f, 27f)); Assert.That(collapseAt, Is.InRange(28f, 38f));
            Assert.AreEqual(37f, new NeedsModel().BodyTemperatureC, 1e-4f);
        }

        [Test]
        public void StarterWaterAndRationCoverAFortyFiveMinuteExpedition()
        {
            var n = new NeedsModel();
            for (int s = 0; s < 24 * 60; s += 5) n.Tick(Seconds(5f), Walk(15f));   // 24 min: ya llego el aviso suave
            Assert.AreNotEqual(NeedsWarning.None, n.Evaluate().Warnings);
            n.DrinkLiters(1f); n.EatRation();                                       // lo que trae el equipo inicial
            for (int s = 0; s < 21 * 60; s += 5) n.Tick(Seconds(5f), Walk(15f));   // 45 min en total
            Assert.IsFalse(n.Evaluate().Collapsing); Assert.Greater(n.Hydration, 0.2f); Assert.Greater(n.Satiety, 0.3f);
        }

        [Test]
        public void HungerIsSlowerThanThirst_AndNeverTheFirstProblem()
        {
            var n = new NeedsModel();
            for (int s = 0; s < 1500; s += 5) n.Tick(Seconds(5f), Walk(15f));   // 25 min
            Assert.Greater(n.Satiety, n.Hydration + 0.25f);
            Assert.AreEqual(0, (int)(n.Evaluate().Warnings & (NeedsWarning.Hungry | NeedsWarning.Starving)));
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

        [Test]
        public void ThirstWarningIsOnlyAReminder_PenaltiesStartLater()
        {
            var n = new NeedsModel(); n.Restore(0.3f, 1f, 37f);
            var e = n.Evaluate();
            Assert.IsTrue((e.Warnings & NeedsWarning.Thirsty) != 0);
            Assert.AreEqual(1f, e.SpeedMultiplier); Assert.AreEqual(1f, e.DigMultiplier); Assert.AreEqual(1f, e.EnergyDrainMultiplier);
            n.Restore(0.1f, 1f, 37f);
            Assert.Greater(n.Evaluate().EnergyDrainMultiplier, 1.2f);
        }

        [Test]
        public void ARightlyPreparedExplorerExploresForOverAnHour_NoCountdown()
        {
            // 3 L de agua y 2 raciones, auto-consumo: caminando 75 min seguidos (mas del doble de la expedicion "sin preparar").
            var n = new NeedsModel(); var l = Loadout.Starter(); l.AddWater(2f); l.AddRations(1);
            bool everCritical = false;
            for (int s = 0; s < 75 * 60; s += 5)
            {
                n.Tick(Seconds(5f), Walk(15f));
                Provisions.AutoConsume(n, l);
                var w = n.Evaluate().Warnings;
                if ((w & (NeedsWarning.Dehydrated | NeedsWarning.Starving)) != 0 || n.Evaluate().Collapsing) everCritical = true;
            }
            Assert.IsFalse(everCritical);
            Assert.Greater(Provisions.Estimate(Biomes.Forest, l, n).LimitingMinutes, 0f);
        }

        [Test]
        public void MoreWaterMeansMoreAutonomy_AtTheCostOfWeight()
        {
            var a = Loadout.Starter(); var b = Loadout.Starter(); b.AddWater(2f);
            float ma = Provisions.Estimate(Biomes.Forest, a).WaterMinutes, mb = Provisions.Estimate(Biomes.Forest, b).WaterMinutes;
            Assert.That(ma, Is.InRange(45f, 60f)); Assert.That(mb, Is.InRange(90f, 110f));
            Assert.AreEqual(2f, b.EquipmentWeightKg() - a.EquipmentWeightKg(), 1e-3f, "cada litro pesa 1 kg: la autonomia se paga con peso");
            Assert.LessOrEqual(b.WaterLiters, Loadout.WaterCapacityLiters);
            var four = Loadout.Starter(); four.AddWater(10f); Assert.AreEqual(Loadout.WaterCapacityLiters, four.WaterLiters, "capacidad maxima");
        }

        [Test]
        public void HeatShortensAutonomy_ProtectionAndRestHelp()
        {
            var l = Loadout.Starter(); l.AddWater(2f);
            float forest = Provisions.Estimate(Biomes.Forest, l).WaterMinutes, desert = Provisions.Estimate(Biomes.Desert, l).WaterMinutes;
            Assert.Less(desert, forest * 0.7f);
            Assert.Less(NeedsModel.HydrationDrainPerHour(new NeedsContext { AmbientC = 15f }), NeedsModel.HydrationDrainPerHour(Walk(15f)), "descansar gasta menos");
        }

        [Test]
        public void AutoConsumeUsesProvisionsOnlyWhenNeeded_AndRefillIsFree()
        {
            var n = new NeedsModel(); var l = Loadout.Starter();
            Assert.AreEqual((false, false), Provisions.AutoConsume(n, l), "recien salido no consume");
            n.Restore(0.3f, 0.2f, 37f);
            var r = Provisions.AutoConsume(n, l);
            Assert.IsTrue(r.drank); Assert.IsTrue(r.ate); Assert.AreEqual(0.5f, l.WaterLiters, 1e-4f); Assert.AreEqual(0, l.Rations);
            n.Restore(0.2f, 1f, 37f); l.TakeWater(0.5f);
            Assert.IsFalse(Provisions.AutoConsume(n, l).drank, "sin agua no hay que consumir: queda el aviso suave");
            l.RefillWater();
            Assert.AreEqual(Loadout.WaterCapacityLiters, l.WaterLiters);
        }
    }
}
