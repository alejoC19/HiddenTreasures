using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class EquipmentEffectsTests
    {
        static Loadout With(params string[] ids)
        {
            var l = new Loadout();
            foreach (var id in ids) { l.Grant(id); l.Equip(id); }
            return l;
        }

        [Test]
        public void BaseExplorerIsLimitedAtNightAndOnSlopes()
        {
            var e = EquipmentEffects.From(Loadout.Starter());
            Assert.AreEqual(7f, e.NightVisionMeters); Assert.AreEqual(32f, e.MaxClimbSlopeDegrees);
            Assert.IsFalse(e.CanNavigate); Assert.IsFalse(e.CanTreatInjury); Assert.IsFalse(e.CanDocument);
            Assert.AreEqual(0.4f, e.CollapseLossFraction); Assert.AreEqual(0.2f, e.CleanQuality);
        }

        [Test]
        public void EachPieceChangesSomethingConcrete()
        {
            Assert.Greater(EquipmentEffects.From(With("eq_flashlight")).NightVisionMeters, 20f);
            Assert.Greater(EquipmentEffects.From(With("eq_rope")).MaxClimbSlopeDegrees, 40f);
            Assert.IsTrue(EquipmentEffects.From(With("eq_compass")).CanNavigate); Assert.IsFalse(EquipmentEffects.From(With("eq_compass")).HasGps);
            Assert.IsTrue(EquipmentEffects.From(With("eq_gps")).HasGps);
            Assert.IsTrue(EquipmentEffects.From(With("eq_firstaid")).CanTreatInjury);
            Assert.Less(EquipmentEffects.From(With("eq_radio")).CollapseLossFraction, 0.4f);
            Assert.IsTrue(EquipmentEffects.From(With("eq_machete")).CanClearVegetation);
            Assert.IsTrue(EquipmentEffects.From(With("eq_repellent")).InsectProtection);
            Assert.AreEqual(0.7f, EquipmentEffects.From(With("eq_brush")).CleanQuality);
            Assert.IsTrue(EquipmentEffects.From(With("eq_camera")).CanDocument);
        }

        [Test]
        public void ClothingTradeOff_WarmVsLight()
        {
            var warm = EquipmentEffects.From(With("eq_clothes_warm")); var light = EquipmentEffects.From(With("eq_clothes_light"));
            Assert.Greater(warm.Warmth, light.Warmth); Assert.Greater(light.HeatProtection, warm.HeatProtection);
            // Y se nota en el modelo de necesidades: la ropa termica en el calor es peor; la ligera en el frio, tambien.
            float hotWarm = Survival.NeedsModel.EquilibriumC(new Survival.NeedsContext { Moving = true, AmbientC = 34f, Warmth = warm.Warmth, HeatProtection = warm.HeatProtection });
            float hotLight = Survival.NeedsModel.EquilibriumC(new Survival.NeedsContext { Moving = true, AmbientC = 34f, Warmth = light.Warmth, HeatProtection = light.HeatProtection });
            float coldWarm = Survival.NeedsModel.EquilibriumC(new Survival.NeedsContext { Moving = true, AmbientC = -8f, Warmth = warm.Warmth });
            float coldLight = Survival.NeedsModel.EquilibriumC(new Survival.NeedsContext { Moving = true, AmbientC = -8f, Warmth = light.Warmth });
            Assert.Greater(hotWarm, hotLight); Assert.Greater(coldWarm, coldLight + 2.5f);
        }

        [Test]
        public void ShovelVsPickDependsOnTheSoil()
        {
            var shovel = With("eq_shovel"); var pick = With("eq_pick");
            Assert.Greater(EquipmentEffects.DigMultiplier(shovel, SoilKind.Soft), EquipmentEffects.DigMultiplier(pick, SoilKind.Soft));
            foreach (var soil in new[] { SoilKind.Hard, SoilKind.Rooty, SoilKind.Rocky })
                Assert.Greater(EquipmentEffects.DigMultiplier(pick, soil), EquipmentEffects.DigMultiplier(shovel, soil), soil.ToString());
            Assert.Less(EquipmentEffects.DigMultiplier(new Loadout(), SoilKind.Soft), 0.5f);
        }

        [Test]
        public void VisibilityBlendsDayAndLightAndNeverBelowOwnLight()
        {
            var dark = EquipmentEffects.From(Loadout.Starter()); var lamp = EquipmentEffects.From(With("eq_flashlight"));
            Assert.AreEqual(7f, dark.VisibilityMeters(0f, 150f)); Assert.AreEqual(150f, dark.VisibilityMeters(1f, 150f));
            Assert.Greater(lamp.VisibilityMeters(0f, 150f), dark.VisibilityMeters(0f, 150f));
            Assert.GreaterOrEqual(lamp.VisibilityMeters(0.5f, 10f), lamp.NightVisionMeters);
        }

        [Test]
        public void NullLoadoutIsSafe() => Assert.DoesNotThrow(() => EquipmentEffects.From(null));
    }
}
