using System.Collections.Generic;
using System.Linq;
using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class FindMetadataTests
    {
        static IItemInfo Info(string id)
        {
            var spec = DefaultItems.Specs().First(s => s.Id == id);
            return spec;
        }

        [Test]
        public void EveryCatalogItemHasAProfileAndAnUnknownOneGetsAGenericProfile()
        {
            foreach (var s in DefaultItems.Specs())
            {
                var p = FindProfiles.Get(s.Id);
                Assert.AreEqual(s.Id, p.ItemId, s.Id);
                Assert.Greater(p.AgeYears, 0); Assert.IsNotEmpty(p.Story); Assert.IsNotEmpty(p.Origin);
                Assert.That(p.FakeChance, Is.InRange(0f, 1f));
            }
            Assert.AreEqual("Origen desconocido", FindProfiles.Get("nope").Origin);
        }

        [Test]
        public void NewFindsStartDirtyAndUnidentified()
        {
            var wb = new FindWorkbook(1);
            var item = new ItemInstance("u1", "roman_denarius", 0.4f);
            var s = wb.State(item);
            Assert.Greater(s.Dirt, 0.5f); Assert.IsFalse(s.Identified);
            Assert.IsFalse(wb.CanIdentify(item)); Assert.IsNull(wb.Identify(item));
        }

        [Test]
        public void BrushCleansSafely_HandsOnFragilePieceCanDamage()
        {
            var wb = new FindWorkbook(1);
            var a = new ItemInstance("a", "roman_denarius", 0.8f);
            Assert.AreEqual(CleanResult.Cleaned, wb.Clean(a, 0.7f, 0.5f));
            Assert.AreEqual(0.8f, a.Condition, 1e-5f);
            var b = new ItemInstance("b", "roman_denarius", 0.8f);
            Assert.AreEqual(CleanResult.Damaged, wb.Clean(b, 0.05f, 0.5f));
            Assert.Less(b.Condition, 0.8f);
        }

        [Test]
        public void CleanThenIdentify_RevealsHistory_AndAlreadyCleanIsReported()
        {
            var wb = new FindWorkbook(1);
            var item = new ItemInstance("u2", "bronze_axe", 0.6f);
            for (int i = 0; i < 4 && !wb.CanIdentify(item); i++) wb.Clean(item, 0.7f, 0.4f);
            Assert.IsTrue(wb.CanIdentify(item));
            var st = wb.Identify(item);
            Assert.IsNotNull(st); Assert.IsTrue(st.Identified);
            for (int i = 0; i < 6; i++) wb.Clean(item, 0.7f, 0.5f);
            Assert.AreEqual(CleanResult.AlreadyClean, wb.Clean(item, 0.7f, 0.5f));
        }

        [Test]
        public void AuthenticityIsStableAndSeedDependent_AndNeverFakeIfChanceIsZero()
        {
            var wb1 = new FindWorkbook(7); var wb2 = new FindWorkbook(7);
            int fakes = 0, diff = 0; var wb3 = new FindWorkbook(8);
            for (int i = 0; i < 400; i++)
            {
                var it = new ItemInstance("uid" + i, "relic_gold_tunjo", 0.5f);
                bool a = wb1.State(it).Authentic;
                Assert.AreEqual(a, wb2.State(it).Authentic, "misma semilla, misma pieza -> misma autenticidad");
                if (!a) fakes++;
                if (wb3.State(it).Authentic != a) diff++;
                Assert.IsTrue(new FindWorkbook(7).State(new ItemInstance("j" + i, "expedition_journal", 1f)).Authentic);
            }
            Assert.That(fakes, Is.InRange(60, 140), "alrededor del 25 %");
            Assert.Greater(diff, 0);
        }

        [Test]
        public void ValueRisesWithCleaningAndIdentification_FakesAreNearlyWorthless()
        {
            var wb = new FindWorkbook(3);
            var info = Info("roman_denarius");
            // Buscamos una pieza autentica y una falsa
            ItemInstance real = null, fake = null;
            for (int i = 0; i < 300 && (real == null || fake == null); i++)
            {
                var it = new ItemInstance("v" + i, "roman_denarius", 0.7f);
                if (wb.State(it).Authentic) real = real ?? it; else fake = fake ?? it;
            }
            Assert.IsNotNull(real); Assert.IsNotNull(fake);
            int dirtyUnknown = wb.Value(info, real);
            for (int i = 0; i < 4; i++) { wb.Clean(real, 0.7f, 0.4f); wb.Clean(fake, 0.7f, 0.4f); }
            int cleanUnknown = wb.Value(info, real);
            wb.Identify(real); wb.Identify(fake);
            int cleanKnown = wb.Value(info, real);
            Assert.Greater(cleanUnknown, dirtyUnknown); Assert.Greater(cleanKnown, cleanUnknown);
            Assert.Less(wb.Value(info, fake), cleanKnown * 0.1f);
        }

        [Test]
        public void SnapshotRoundTrips()
        {
            var wb = new FindWorkbook(5);
            var it = new ItemInstance("s1", "bronze_fibula", 0.5f);
            wb.Clean(it, 0.7f, 1f); wb.Identify(it);
            var snap = wb.Snapshot();
            var wb2 = new FindWorkbook(5); wb2.Restore(snap);
            var s = wb2.State(it);
            Assert.IsTrue(s.Identified); Assert.AreEqual(wb.State(it).Dirt, s.Dirt, 1e-5f); Assert.AreEqual(wb.State(it).Authentic, s.Authentic);
        }
    }
}
