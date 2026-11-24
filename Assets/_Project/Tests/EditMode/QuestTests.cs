using NUnit.Framework;
using HuesoOxido.Core;

namespace HuesoOxido.Tests
{
    public sealed class QuestTests
    {
        [Test]
        public void RelicHunt_StartsAtZeroOfThree()
        {
            var q = new QuestManager();
            q.Start(QuestManager.RelicHunt());
            Assert.AreEqual("Reliquias: 0/3", q.ProgressText);
            Assert.IsFalse(q.IsCompleted);
        }

        [Test]
        public void OnlyRelicsAdvanceTheQuest()
        {
            var q = new QuestManager();
            q.Start(QuestManager.RelicHunt());
            Assert.IsFalse(q.ReportFind("flint_arrowhead"));
            Assert.IsFalse(q.ReportFind(null));
            Assert.IsTrue(q.ReportFind("relic_gold_tunjo"));
            Assert.AreEqual("Reliquias: 1/3", q.ProgressText);
        }

        [Test]
        public void ThreeRelicsComplete_AndFireCompletedOnce()
        {
            var q = new QuestManager();
            int completed = 0, changed = 0;
            q.Completed += _ => completed++;
            q.Changed += () => changed++;
            q.Start(QuestManager.RelicHunt());
            q.ReportFind("relic_gold_tunjo"); q.ReportFind("relic_ceramic_vessel"); q.ReportFind("relic_jade_pendant");
            Assert.IsTrue(q.IsCompleted);
            Assert.AreEqual(1, completed);
            Assert.IsFalse(q.ReportFind("relic_gold_tunjo"), "una mision cumplida ya no avanza");
            Assert.AreEqual(3, q.Progress);
            Assert.AreEqual(1, completed);
            Assert.GreaterOrEqual(changed, 4);
        }

        [Test]
        public void SaveRestore_KeepsProgress_AndRejectsUnknownQuest()
        {
            var q = new QuestManager();
            q.Restore(QuestManager.RelicHuntId, 2, false);
            Assert.AreEqual("Reliquias: 2/3", q.ProgressText);
            Assert.IsFalse(q.IsCompleted);
            q.Restore(QuestManager.RelicHuntId, 99, false);
            Assert.AreEqual(3, q.Progress, "el progreso se acota al objetivo");
            Assert.IsTrue(q.IsCompleted);
            q.Restore("otra_mision", 1, false);
            Assert.IsFalse(q.HasQuest);
        }

        [Test]
        public void CatalogHasThreeRelics_WithPrefix()
        {
            int n = 0;
            foreach (var s in DefaultItems.Specs()) if (s.Id.StartsWith(QuestManager.RelicPrefix)) n++;
            Assert.AreEqual(3, n);
        }
    }
}
