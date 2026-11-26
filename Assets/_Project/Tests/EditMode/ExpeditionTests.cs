using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class ExpeditionTests
    {
        [Test]
        public void Starter_HasBasicsAndExclusiveSlots()
        {
            var l = Loadout.Starter();
            Assert.IsTrue(l.Has(EquipmentKind.Detector)); Assert.IsTrue(l.Has(EquipmentKind.Shovel)); Assert.IsTrue(l.Has(EquipmentKind.LightClothing));
            l.Grant("eq_pick"); Assert.IsTrue(l.Equip("eq_pick"));
            Assert.IsTrue(l.Has(EquipmentKind.Pick)); Assert.IsFalse(l.Has(EquipmentKind.Shovel), "pala y pico son excluyentes");
            l.Grant("eq_clothes_warm"); l.Equip("eq_clothes_warm");
            Assert.IsTrue(l.Has(EquipmentKind.WarmClothing)); Assert.IsFalse(l.Has(EquipmentKind.LightClothing));
            Assert.IsFalse(l.Equip("eq_gps"), "no se equipa lo que no se posee");
        }

        [Test]
        public void ItemsKeepIndependentSlots_AndWeightCounts()
        {
            var l = Loadout.Starter();
            float w0 = l.EquipmentWeightKg();
            l.Grant("eq_flashlight"); l.Grant("eq_firstaid"); l.Equip("eq_flashlight"); l.Equip("eq_firstaid");
            Assert.Greater(l.EquipmentWeightKg(), w0 + 1f - 1e-3f);
            l.AddWater(2f); l.AddRations(2);
            Assert.AreEqual(3f, l.WaterLiters, 1e-4f);
            Assert.IsTrue(l.TakeWater(1f)); Assert.IsFalse(l.TakeWater(5f)); Assert.IsTrue(l.TakeRation()); Assert.AreEqual(2, l.Rations);
        }

        [Test]
        public void Forest_StarterCanDepartInDaylight_ButNotWithoutWater()
        {
            var forest = Biomes.Forest;
            var l = Loadout.Starter();
            var ok = ExpeditionPlanner.Evaluate(forest, l, 0.8f, 10f);
            Assert.IsTrue(ok.CanDepart);
            Assert.Greater(ok.Missing(RequirementLevel.Recommended), 0, "faltan linterna y navegacion (recomendados)");
            l.TakeWater(1f);
            Assert.IsFalse(ExpeditionPlanner.Evaluate(forest, l, 0.8f, 10f).CanDepart);
        }

        [Test]
        public void Snow_RequiresWarmClothing_AndNightRequiresFlashlight()
        {
            var l = Loadout.Starter();
            var snow = ExpeditionPlanner.Evaluate(Biomes.Snow, l, 0.8f, 10f);
            Assert.IsFalse(snow.CanDepart);
            l.Grant("eq_clothes_warm"); l.Equip("eq_clothes_warm");
            Assert.IsTrue(ExpeditionPlanner.Evaluate(Biomes.Snow, l, 0.8f, 10f).CanDepart);
            Assert.IsFalse(ExpeditionPlanner.Evaluate(Biomes.Forest, l, 0.8f, 22f).CanDepart, "de noche sin linterna no se sale");
            l.Grant("eq_flashlight"); l.Equip("eq_flashlight");
            Assert.IsTrue(ExpeditionPlanner.Evaluate(Biomes.Forest, l, 0.8f, 22f).CanDepart);
        }

        [Test]
        public void Desert_AsksForMoreWaterAndRiskDropsWithPreparation()
        {
            var l = Loadout.Starter();
            Assert.IsFalse(ExpeditionPlanner.Evaluate(Biomes.Desert, l, 0.9f, 9f).CanDepart);
            float riskBefore = ExpeditionPlanner.Evaluate(Biomes.Forest, l, 0.3f, 9f).Risk;
            l.AddWater(2f); l.Grant("eq_compass"); l.Equip("eq_compass"); l.Grant("eq_flashlight"); l.Equip("eq_flashlight");
            float riskAfter = ExpeditionPlanner.Evaluate(Biomes.Forest, l, 0.9f, 9f).Risk;
            Assert.Less(riskAfter, riskBefore);
            Assert.IsTrue(ExpeditionPlanner.Evaluate(Biomes.Desert, l, 0.9f, 9f).CanDepart);
        }

        [Test]
        public void NoFuelNoDeparture()
        {
            Assert.IsFalse(ExpeditionPlanner.Evaluate(Biomes.Forest, Loadout.Starter(), 0.1f, 10f).CanDepart);
        }

        [Test]
        public void ExpeditionLifecycle()
        {
            var log = new ExpeditionLog();
            log.BeginPreparing(Biomes.ForestId);
            var bad = ExpeditionPlanner.Evaluate(Biomes.Forest, Loadout.Starter(), 0.05f, 10f);
            Assert.IsFalse(log.Depart(bad, 10f)); Assert.AreEqual(ExpeditionPhase.Preparing, log.Phase);
            var good = ExpeditionPlanner.Evaluate(Biomes.Forest, Loadout.Starter(), 0.9f, 10f);
            Assert.IsTrue(log.Depart(good, 10f));
            log.RecordFind(120); log.RecordFind(40);
            var s = log.Return(13.5f);
            Assert.AreEqual(2, s.Finds); Assert.AreEqual(160, s.TotalValue); Assert.AreEqual(3.5f, s.Hours, 1e-4f); Assert.IsTrue(s.Success);
            Assert.IsNull(log.Return(14f), "no se vuelve dos veces");
            log.RecordFind(999); Assert.AreEqual(160, log.TotalValue, "fuera de expedicion no se registran hallazgos");
        }

        [Test]
        public void LoadoutSnapshotRoundTrips_AndIgnoresUnknownIds()
        {
            var l = Loadout.Starter(); l.Grant("eq_gps"); l.Equip("eq_gps");
            var s = l.Snapshot();
            var m = new Loadout(); m.Restore(s.owned, s.equipped, s.water, s.rations);
            Assert.IsTrue(m.Has(EquipmentKind.Gps)); Assert.AreEqual(l.EquipmentWeightKg(), m.EquipmentWeightKg(), 1e-4f);
            var n = new Loadout(); n.Restore(new[] { "fake", "eq_shovel" }, new[] { "eq_shovel", "eq_gps" }, -3f, -2);
            Assert.IsTrue(n.Has(EquipmentKind.Shovel)); Assert.IsFalse(n.Has(EquipmentKind.Gps)); Assert.AreEqual(0f, n.WaterLiters);
        }

        [Test]
        public void RemoteRegionsRecommendARadio_ForestDoesNot()
        {
            var l = Loadout.Starter();
            Assert.IsFalse(ExpeditionPlanner.Evaluate(Biomes.Forest, l, 0.9f, 10f).Items.Exists(i => i.Id == "radio"));
            var swamp = ExpeditionPlanner.Evaluate(Biomes.Swamp, l, 0.9f, 10f);
            var item = swamp.Items.Find(i => i.Id == "radio");
            Assert.IsNotNull(item); Assert.AreEqual(RequirementLevel.Recommended, item.Level); Assert.IsFalse(item.Met);
            l.Grant("eq_radio"); l.Equip("eq_radio");
            Assert.IsTrue(ExpeditionPlanner.Evaluate(Biomes.Swamp, l, 0.9f, 10f).Items.Find(i => i.Id == "radio").Met);
        }

        [Test]
        public void LongExplorationIsRecommendedNotRequired()
        {
            var l = Loadout.Starter();
            var r = ExpeditionPlanner.Evaluate(Biomes.Forest, l, 0.9f, 10f);
            var item = r.Items.Find(i => i.Id == "water_long");
            Assert.AreEqual(RequirementLevel.Recommended, item.Level); Assert.IsFalse(item.Met); Assert.IsTrue(r.CanDepart);
            l.AddWater(2f);
            Assert.IsTrue(ExpeditionPlanner.Evaluate(Biomes.Forest, l, 0.9f, 10f).Items.Find(i => i.Id == "water_long").Met);
        }
    }
}
