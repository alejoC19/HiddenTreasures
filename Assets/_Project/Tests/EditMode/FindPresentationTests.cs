using System;
using System.Linq;
using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public sealed class FindPresentationTests
    {
        static ItemSpec Spec(string id) => DefaultItems.Specs().First(s => s.Id == id);

        [Test]
        public void Card_CarriesNameRarityValueAndNewFlag()
        {
            var c = FindPresentation.BuildCard(Spec("flint_arrowhead"), 18, true);
            Assert.AreEqual("Punta de flecha de sílex", c.Title);
            Assert.AreEqual(RarityStyle.NameOf(Rarity.Common), c.RarityName);
            Assert.AreEqual(18, c.Value);
            Assert.IsTrue(c.IsNew);
            Assert.IsFalse(c.IsRelic);
            Assert.IsFalse(string.IsNullOrEmpty(c.Description));
        }

        [Test]
        public void Relics_AreFlaggedAndShownLonger()
        {
            var relic = FindPresentation.BuildCard(Spec("relic_gold_tunjo"), 900, false);
            var epic = FindPresentation.BuildCard(Spec("trilobite_slab"), 640, false);
            Assert.IsTrue(relic.IsRelic);
            Assert.IsFalse(relic.IsNew);
            Assert.Greater(relic.Seconds, epic.Seconds - 0.001f);
        }

        [Test]
        public void DisplayTime_GrowsWithRarity()
        {
            float last = 0f;
            foreach (Rarity r in Enum.GetValues(typeof(Rarity)))
            {
                float s = FindPresentation.DisplaySeconds(r);
                Assert.Greater(s, last);
                last = s;
            }
            Assert.AreEqual(3.5f, FindPresentation.DisplaySeconds(Rarity.Common), 1e-4f);
        }

        [Test]
        public void Tone_FollowsRarity_AndRelicsAreSpecial()
        {
            Assert.AreEqual(DetectorTone.Low, FindPresentation.ToneFor(Spec("iron_nail")));
            Assert.AreEqual(DetectorTone.Mid, FindPresentation.ToneFor(Spec("bronze_fibula")));
            Assert.AreEqual(DetectorTone.High, FindPresentation.ToneFor(Spec("roman_denarius")));
            Assert.AreEqual(DetectorTone.High, FindPresentation.ToneFor(Spec("golden_torc")));
            Assert.AreEqual(DetectorTone.Relic, FindPresentation.ToneFor(Spec("relic_jade_pendant")));
            Assert.AreEqual(DetectorTone.Low, FindPresentation.ToneFor(null));
        }

        static float Brightness(float[] d)
        {
            float diff = 0f, e = 0f;
            for (int i = 1; i < d.Length; i++) { diff += (d[i] - d[i - 1]) * (d[i] - d[i - 1]); e += d[i] * d[i]; }
            return diff / e;
        }

        [Test]
        public void BeepTimbre_GetsBrighterForValuableTargets_KeepsPitch()
        {
            float f = 880f;
            float low = Brightness(ProceduralSounds.Beep(f, DetectorTone.Low, 0.25f));
            float mid = Brightness(ProceduralSounds.Beep(f, DetectorTone.Mid, 0.25f));
            float high = Brightness(ProceduralSounds.Beep(f, DetectorTone.High, 0.25f));
            float relic = Brightness(ProceduralSounds.Beep(f, DetectorTone.Relic, 0.25f));
            Assert.Less(low, mid); Assert.Less(mid, high); Assert.Less(high, relic + 0.0001f);
            foreach (DetectorTone t in Enum.GetValues(typeof(DetectorTone)))
            {
                var d = ProceduralSounds.Beep(f, t, 0.25f);
                Assert.LessOrEqual(d.Max(Math.Abs), 1.0f);
                Assert.IsTrue(d.All(x => !float.IsNaN(x)));
            }
            // la fundamental no cambia: Low coincide con el bip simple
            CollectionAssert.AreEqual(ProceduralSounds.Beep(f, 0.25f), ProceduralSounds.Beep(f, DetectorTone.Low, 0.25f));
        }
    }
}
