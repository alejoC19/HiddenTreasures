using System.Linq;
using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class CleaningSessionTests
    {
        static CleaningSession Make(string uid, float condition = 0.8f, float tool = 0.7f, bool authentic = true)
            => new CleaningSession(new ItemInstance(uid, "roman_denarius", condition), FindProfiles.Get("roman_denarius"), authentic, tool);

        static void CleanAll(CleaningSession s, float pressure)
        {
            for (int sec = 0; sec < CleaningSession.Sectors; sec++) for (int i = 0; i < 80; i++) s.Brush(sec, pressure, 0.1f);
        }

        [Test]
        public void SafePressureDependsOnToolAndFragility()
        {
            Assert.Greater(Make("a", 0.8f, 0.7f).SafePressure, Make("a", 0.8f, 0.2f).SafePressure);
            Assert.Greater(Make("a", 0.9f).SafePressure, Make("a", 0.1f).SafePressure);
        }

        [Test]
        public void GentlePressureCleansWithoutDamage_HardPressureDamages()
        {
            var gentle = Make("g"); CleanAll(gentle, gentle.SafePressure * 0.9f);
            Assert.Greater(gentle.CleanFraction, 0.95f); Assert.AreEqual(0f, gentle.Damage);
            var hard = Make("h"); CleanAll(hard, 1f);
            Assert.Greater(hard.Damage, 0f);
            var item = new ItemInstance("h", "roman_denarius", 0.8f);
            var s = new CleaningSession(item, FindProfiles.Get("roman_denarius"), true, 0.7f); CleanAll(s, 1f);
            var outcome = s.Finish();
            Assert.Greater(outcome.ConditionLost, 0f); Assert.AreEqual(0.8f - outcome.ConditionLost, item.Condition, 1e-5f);
        }

        [Test]
        public void HardPressureIsFasterButCostly()
        {
            var a = Make("sp"); var b = Make("sp");
            a.Brush(0, a.SafePressure * 0.5f, 1f); b.Brush(0, 1f, 1f);
            Assert.Greater(b.SoilAt(0) < a.SoilAt(0) ? 1 : 0, 0);
            Assert.Greater(b.Damage, a.Damage);
        }

        [Test]
        public void CluesAppearOnlyWhenTheirSectorIsClean_AndFourExist()
        {
            var s = Make("c1"); int revealedEvents = 0; s.ClueRevealed += _ => revealedEvents++;
            Assert.IsEmpty(s.RevealedClues());
            s.Brush(0, 0.1f, 0.1f);
            Assert.IsEmpty(s.RevealedClues());
            CleanAll(s, s.SafePressure * 0.9f);
            Assert.AreEqual(4, s.RevealedClues().Count); Assert.AreEqual(4, revealedEvents);
        }

        [Test]
        public void CluesAreDeterministicPerUid()
        {
            var a = Make("same"); var b = Make("same"); CleanAll(a, 0.3f); CleanAll(b, 0.3f);
            CollectionAssert.AreEqual(a.RevealedClues().Select(c => c.Text).ToList(), b.RevealedClues().Select(c => c.Text).ToList());
        }

        [Test]
        public void FakesBetrayThemselves_AuthenticPiecesUsuallyDoNot()
        {
            int fakeWithSuspicious = 0, authWithSuspicious = 0;
            for (int i = 0; i < 300; i++)
            {
                var f = Make("f" + i, authentic: false); CleanAll(f, 0.3f);
                var a = Make("a" + i, authentic: true); CleanAll(a, 0.3f);
                if (f.RevealedClues().Any(c => c.Tone == ClueTone.Suspicious)) fakeWithSuspicious++;
                if (a.RevealedClues().Any(c => c.Tone == ClueTone.Suspicious)) authWithSuspicious++;
            }
            Assert.AreEqual(300, fakeWithSuspicious); Assert.Less(authWithSuspicious, 70);
        }

        [Test]
        public void AppraisalImprovesWithMoreCluesButIsNotCertainty()
        {
            CleaningSession s = null;
            for (int i = 0; i < 200; i++)   // una falsificacion que se delata con 2 pistas sospechosas
            {
                var t = Make("ap" + i, authentic: false); CleanAll(t, 0.3f);
                if (t.RevealedClues().Count(c => c.Tone == ClueTone.Suspicious) >= 2) { s = t; break; }
            }
            Assert.IsNotNull(s);
            var all = s.RevealedClues();
            var one = Appraisal.From(all.Take(1).ToList()); var four = Appraisal.From(all);
            Assert.Greater(four.Confidence, one.Confidence); Assert.LessOrEqual(four.Confidence, 0.95f);
            Assert.Greater(Appraisal.From(all, 0.3f).Confidence, four.Confidence);
            Assert.IsFalse(four.BelievesAuthentic, "una falsificacion con todas sus pistas se detecta");
        }

        [Test]
        public void FullInspectionIsRightMostOfTheTime_ButNotAlways()
        {
            int right = 0, total = 0;
            for (int i = 0; i < 400; i++)
            {
                bool authentic = i % 2 == 0;
                var s = Make("k" + i, authentic: authentic); CleanAll(s, 0.3f);
                if (Appraisal.From(s.RevealedClues()).BelievesAuthentic == authentic) right++;
                total++;
            }
            float acc = right / (float)total;
            Assert.That(acc, Is.InRange(0.8f, 0.995f), "informa mucho, pero con falsas alarmas el jugador sigue decidiendo bajo incertidumbre");
        }

        [Test]
        public void OutOfRangeSectorsAreIgnored()
        {
            var s = Make("o");
            Assert.AreEqual(0f, s.Brush(-1, 1f, 1f).Removed); Assert.AreEqual(0f, s.Brush(99, 1f, 1f).Removed); Assert.AreEqual(0f, s.Brush(0, 1f, 0f).Removed);
        }
    }
}
