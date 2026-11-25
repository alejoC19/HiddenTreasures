using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public sealed class QualityTests
    {
        [Test]
        public void Profiles_AreOrdered_LowToHigh()
        {
            var l = QualityProfile.For(QualityTier.Low); var m = QualityProfile.For(QualityTier.Medium); var h = QualityProfile.For(QualityTier.High);
            Assert.Less(l.VegetationDistanceScale, m.VegetationDistanceScale); Assert.Less(m.VegetationDistanceScale, h.VegetationDistanceScale);
            Assert.Less(l.RenderScale, m.RenderScale); Assert.Less(m.RenderScale, h.RenderScale);
            Assert.Less(l.ShadowDistance, m.ShadowDistance); Assert.Less(m.ShadowDistance, h.ShadowDistance);
            Assert.LessOrEqual(l.ShadowCascades, m.ShadowCascades); Assert.LessOrEqual(m.ShadowCascades, h.ShadowCascades);
            Assert.IsFalse(l.Ssao); Assert.IsTrue(h.DepthOfField); Assert.IsFalse(m.DepthOfField);
            Assert.AreEqual(1f, h.RenderScale);
        }

        [Test]
        public void Auto_PicksMediumForAMidRangeGpu()
        {
            Assert.AreEqual(QualityTier.Medium, QualityProfile.Auto(8192));   // RX 580 de 8 GB
            Assert.AreEqual(QualityTier.Low, QualityProfile.Auto(2048));
            Assert.AreEqual(QualityTier.High, QualityProfile.Auto(16384));
            Assert.AreEqual(QualityTier.Medium, QualityProfile.Auto(0), "memoria desconocida: Medio");
        }

        [Test]
        public void Next_CyclesThroughAllTiers()
        {
            Assert.AreEqual(QualityTier.Medium, QualityProfile.Next(QualityTier.Low));
            Assert.AreEqual(QualityTier.High, QualityProfile.Next(QualityTier.Medium));
            Assert.AreEqual(QualityTier.Low, QualityProfile.Next(QualityTier.High));
        }

        static QualityTier? Run(AdaptiveQuality a, float fps, float seconds, QualityTier tier, bool active = true)
        {
            QualityTier? result = null;
            float dt = 1f / fps;
            for (float t = 0; t < seconds; t += dt) { var r = a.Feed(dt, active, tier); if (r.HasValue) result = r; }
            return result;
        }

        [Test]
        public void Adaptive_DropsOneTier_WhenFpsStaysLow()
        {
            var a = new AdaptiveQuality();
            Assert.AreEqual(QualityTier.Low, Run(a, 20f, 30f, QualityTier.Medium));
        }

        [Test]
        public void Adaptive_DoesNothing_WhenFpsIsFine_OrWhileInMenus_OrAtLow()
        {
            Assert.IsNull(Run(new AdaptiveQuality(), 60f, 40f, QualityTier.High));
            Assert.IsNull(Run(new AdaptiveQuality(), 10f, 40f, QualityTier.High, active: false));
            Assert.IsNull(Run(new AdaptiveQuality(), 10f, 40f, QualityTier.Low));
        }

        [Test]
        public void Adaptive_IgnoresTheWarmup_AndWaitsBetweenDrops()
        {
            var a = new AdaptiveQuality();
            Assert.IsNull(Run(a, 15f, 7f, QualityTier.High), "los primeros segundos (carga) no cuentan");
            var b = new AdaptiveQuality();
            var first = Run(b, 20f, 20f, QualityTier.High);
            Assert.AreEqual(QualityTier.Medium, first);
            Assert.IsNull(Run(b, 20f, 6f, QualityTier.Medium), "enfriamiento: no baja dos veces seguidas");
        }
    }
}
