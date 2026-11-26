using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class RescueServiceTests
    {
        static EquipmentEffects Fx(params string[] ids)
        {
            var l = new Loadout();
            foreach (var id in ids) { l.Grant(id); l.Equip(id); }
            return EquipmentEffects.From(l);
        }
        static RescueContext Ctx(float dist = 300f, float cov = 0.9f, float weather = 0f, bool night = false, bool collapsed = false)
            => new RescueContext { DistanceToCampMeters = dist, RadioCoverage = cov, WeatherSeverity = weather, Night = night, Collapsed = collapsed };

        [Test]
        public void WithoutRadioThereIsNoCall()
        {
            var q = RescueService.Request(Fx("eq_gps", "eq_firstaid"), Ctx());
            Assert.AreEqual(RescueStatus.NoRadio, q.Status); Assert.IsNotEmpty(q.Reason);
        }

        [Test]
        public void RemoteRegionsMayHaveNoSignal_ForestHasCoverage()
        {
            Assert.AreEqual(RescueStatus.Dispatched, RescueService.Request(Fx("eq_radio"), Ctx(cov: Biomes.Forest.RadioCoverage)).Status);
            Assert.AreEqual(RescueStatus.NoSignal, RescueService.Request(Fx("eq_radio"), Ctx(cov: 0.3f)).Status);
            Assert.AreEqual(RescueStatus.NoSignal, RescueService.Request(Fx("eq_radio"), Ctx(dist: 900f, cov: 0.4f)).Status, "lejos de la base la senal cae");
            Assert.Less(Biomes.Swamp.RadioCoverage, Biomes.Forest.RadioCoverage);
        }

        [Test]
        public void StormBlocksTheRescue_ButLightRainOnlyDelaysIt()
        {
            Assert.AreEqual(RescueStatus.BlockedByWeather, RescueService.Request(Fx("eq_radio"), Ctx(weather: 0.9f)).Status);
            var clear = RescueService.Request(Fx("eq_radio"), Ctx(weather: 0f)); var rain = RescueService.Request(Fx("eq_radio"), Ctx(weather: 0.5f));
            Assert.Greater(rain.EtaHours, clear.EtaHours);
        }

        [Test]
        public void PreciseCoordinatesMeanFasterRescueAndFullRecovery()
        {
            var approx = RescueService.Request(Fx("eq_radio"), Ctx(collapsed: true)); var precise = RescueService.Request(Fx("eq_radio", "eq_gps"), Ctx(collapsed: true));
            Assert.Less(precise.EtaHours, approx.EtaHours); Assert.AreEqual(CoordinatePrecision.Precise, precise.Precision);
            Assert.AreEqual(1f, precise.RecoveredItemsFraction); Assert.AreEqual(0.5f, approx.RecoveredItemsFraction);
        }

        [Test]
        public void CallingBeforeCollapseSavesEverything_AtACost()
        {
            var q = RescueService.Request(Fx("eq_radio"), Ctx(collapsed: false));
            Assert.AreEqual(1f, q.RecoveredItemsFraction); Assert.Greater(q.Fee, 0);
            Assert.Greater(RescueService.Request(Fx("eq_radio"), Ctx(night: true)).Fee, RescueService.Request(Fx("eq_radio"), Ctx(night: false)).Fee);
            Assert.Greater(RescueService.Request(Fx("eq_radio"), Ctx(dist: 1000f)).EtaHours, RescueService.Request(Fx("eq_radio"), Ctx(dist: 100f)).EtaHours);
        }
    }
}
