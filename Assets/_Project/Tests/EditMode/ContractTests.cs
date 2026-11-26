using System.Linq;
using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class ContractTests
    {
        static IItemInfo Info(string id) => DefaultItems.Specs().First(s => s.Id == id);

        // Devuelve una pieza autentica y ya limpia e identificada (o sin identificar si se pide).
        static ItemInstance Ready(FindWorkbook wb, string def, float cond, bool identify = true, bool wantAuthentic = true)
        {
            for (int i = 0; i < 2000; i++)
            {
                var it = new ItemInstance("t" + def + i, def, cond);
                if (wb.State(it).Authentic != wantAuthentic) continue;
                for (int k = 0; k < 8; k++) wb.Clean(it, 0.9f, 0.5f);
                if (identify) wb.Identify(it);
                return it;
            }
            Assert.Fail("sin pieza candidata"); return null;
        }

        [Test]
        public void OffersRespectRegionReputationAndExclusions()
        {
            var b = new ContractBoard(DefaultContracts.All);
            var forest = b.Offers(Biomes.ForestId).Select(d => d.Id).ToList();
            CollectionAssert.Contains(forest, "c_arrowhead"); CollectionAssert.DoesNotContain(forest, "c_axe");
            Assert.IsEmpty(b.Offers(Biomes.DesertId));
            Assert.IsTrue(b.Accept("c_arrowhead", 1));
            CollectionAssert.DoesNotContain(b.Offers(Biomes.ForestId).Select(d => d.Id).ToList(), "c_arrowhead");
            Assert.IsFalse(b.Accept("c_axe", 1), "falta reputacion");
        }

        [Test]
        public void ActiveLimitIsThree()
        {
            var b = new ContractBoard(DefaultContracts.All);
            Assert.IsTrue(b.Accept("c_arrowhead", 1)); Assert.IsTrue(b.Accept("c_pottery", 1)); Assert.IsTrue(b.Accept("c_denarii", 1));
            Assert.AreEqual(3, b.Active.Count);
            b.Restore(0, null, null);
            var b2 = new ContractBoard(DefaultContracts.All.Concat(new[] { new ContractDefinition { Id = "x", Title = "x", RegionId = Biomes.ForestId } }));
            b2.Accept("c_arrowhead", 1); b2.Accept("c_pottery", 1); b2.Accept("c_denarii", 1);
            Assert.IsFalse(b2.Accept("x", 1));
        }

        [Test]
        public void DeliveryNeedsIdentifiedAuthenticAndConditionedPieces()
        {
            var wb = new FindWorkbook(11); var b = new ContractBoard(DefaultContracts.All);
            b.Accept("c_denarii", 1);
            var dirty = new ItemInstance("d1", "roman_denarius", 0.9f);
            Assert.AreEqual(DeliveryResult.NotIdentified, b.Deliver(dirty, Info("roman_denarius"), wb));
            var fake = Ready(wb, "roman_denarius", 0.9f, true, false);
            Assert.AreEqual(DeliveryResult.Counterfeit, b.Deliver(fake, Info("roman_denarius"), wb));
            var worn = Ready(wb, "roman_denarius", 0.2f);
            Assert.AreEqual(DeliveryResult.ConditionTooLow, b.Deliver(worn, Info("roman_denarius"), wb));
            Assert.AreEqual(DeliveryResult.NoMatchingContract, b.Deliver(Ready(wb, "iron_nail", 0.9f), Info("iron_nail"), wb));
        }

        [Test]
        public void CompletingAContractPaysReputationAndUnlocksTier()
        {
            var wb = new FindWorkbook(12); var b = new ContractBoard(DefaultContracts.All);
            int completedEvents = 0; b.Completed += _ => completedEvents++;
            b.Accept("c_denarii", 1);
            Assert.AreEqual(DeliveryResult.Accepted, b.Deliver(Ready(wb, "roman_denarius", 0.8f), Info("roman_denarius"), wb));
            Assert.AreEqual(1, b.Active[0].Delivered); Assert.AreEqual(0, completedEvents);
            Assert.AreEqual(DeliveryResult.Accepted, b.Deliver(Ready(wb, "roman_denarius", 0.8f), Info("roman_denarius"), wb));
            Assert.AreEqual(1, completedEvents); Assert.IsTrue(b.IsCompleted("c_denarii")); Assert.AreEqual(25, b.Reputation);
            Assert.AreEqual("Aprendiz", b.LevelName);
            b.Restore(120, null, null);
            Assert.AreEqual("Explorador", b.LevelName);
            Assert.IsTrue(b.Accept("c_axe", 5));
        }

        [Test]
        public void AnyRareItemContractMatchesByRarity()
        {
            var wb = new FindWorkbook(13); var b = new ContractBoard(DefaultContracts.All);
            b.Restore(400, null, null);
            Assert.IsTrue(b.Accept("c_relic", 1));
            Assert.AreEqual(DeliveryResult.NoMatchingContract, b.Deliver(Ready(wb, "iron_nail", 0.9f), Info("iron_nail"), wb));
            var epic = DefaultItems.Specs().First(s => s.Rarity >= Rarity.Epic);
            var item = Ready(wb, epic.Id, 0.9f);
            Assert.AreEqual(DeliveryResult.Accepted, b.Deliver(item, epic, wb));
        }

        [Test]
        public void OverdueContractsCostReputationNotMoney_AndSnapshotRoundTrips()
        {
            var b = new ContractBoard(new[] { new ContractDefinition { Id = "t", Title = "t", RegionId = Biomes.ForestId, ItemId = "iron_nail", DeadlineDays = 2, RewardMoney = 100 } });
            b.Restore(30, null, null);
            b.Accept("t", 1);
            Assert.AreEqual(0, b.ExpireOverdue(3));
            Assert.AreEqual(1, b.ExpireOverdue(4));
            Assert.AreEqual(20, b.Reputation); Assert.IsEmpty(b.Active);
            b.Accept("t", 4);
            var s = b.Snapshot();
            var c = new ContractBoard(new[] { new ContractDefinition { Id = "t", Title = "t", RegionId = Biomes.ForestId, ItemId = "iron_nail", DeadlineDays = 2 } });
            c.Restore(s.reputation, s.completed, s.active);
            Assert.AreEqual(20, c.Reputation); Assert.IsTrue(c.IsActive("t"));
        }

        [Test]
        public void CatalogIsConsistent()
        {
            var ids = DefaultContracts.All.Select(d => d.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count());
            foreach (var d in DefaultContracts.All)
            {
                Assert.Greater(d.RewardMoney, 0); Assert.Greater(d.Count, 0); Assert.IsNotEmpty(d.Story);
                if (d.ItemId != null) Assert.IsTrue(DefaultItems.Specs().Any(s => s.Id == d.ItemId), d.ItemId);
                if (d.Kind == ContractKind.Collection) { Assert.AreEqual(d.ItemIds.Length, d.Count); foreach (var id in d.ItemIds) Assert.IsTrue(DefaultItems.Specs().Any(s => s.Id == id), id); }
                if (d.Kind == ContractKind.Document || d.Kind == ContractKind.Survey) Assert.IsNotEmpty(d.SiteId);
                if (d.PrerequisiteId != null) Assert.IsTrue(DefaultContracts.All.Any(x => x.Id == d.PrerequisiteId), d.PrerequisiteId);
            }
        }

        [Test]
        public void SurveyNeedsNavigation_DocumentNeedsCamera()
        {
            var b = new ContractBoard(DefaultContracts.All);
            b.Accept("c_survey_ruins", 1); b.Accept("c_doc_arch", 1);
            Assert.IsFalse(b.ReportSite("ruins", documented: true, navigating: false), "llegar sin orientarse no cuenta");
            Assert.IsFalse(b.ReportSite("ruins_arch", documented: false, navigating: true), "sin camara no se documenta");
            Assert.IsTrue(b.ReportSite("ruins", false, true)); Assert.IsTrue(b.IsCompleted("c_survey_ruins"));
            Assert.IsTrue(b.ReportSite("ruins_arch", true, false)); Assert.IsTrue(b.IsCompleted("c_doc_arch"));
            Assert.AreEqual(35, b.Reputation);
        }

        [Test]
        public void CollectionNeedsDistinctPieces_AndChainsUnlockInOrder()
        {
            var wb = new FindWorkbook(21); var b = new ContractBoard(DefaultContracts.All);
            b.Restore(150, null, null);
            Assert.IsTrue(b.Accept("c_village", 1));
            Assert.AreEqual(DeliveryResult.Accepted, b.Deliver(Ready(wb, "pottery_shard", 0.8f), Info("pottery_shard"), wb));
            Assert.AreEqual(DeliveryResult.NoMatchingContract, b.Deliver(Ready(wb, "pottery_shard", 0.8f), Info("pottery_shard"), wb), "la segunda ceramica ya no sirve");
            Assert.AreEqual(DeliveryResult.Accepted, b.Deliver(Ready(wb, "iron_nail", 0.8f), Info("iron_nail"), wb));
            Assert.AreEqual(DeliveryResult.Accepted, b.Deliver(Ready(wb, "flint_arrowhead", 0.8f), Info("flint_arrowhead"), wb));
            Assert.IsTrue(b.IsCompleted("c_village"));
            // cadena: el diario solo despues del reconocimiento
            Assert.IsFalse(b.Accept("c_journal", 2));
            CollectionAssert.DoesNotContain(b.Offers(Biomes.ForestId).Select(d => d.Id).ToList(), "c_journal");
            b.Accept("c_survey_ruins", 2); b.ReportSite("ruins", false, true);
            CollectionAssert.Contains(b.Offers(Biomes.ForestId).Select(d => d.Id).ToList(), "c_journal");
        }

        [Test]
        public void CollectionProgressSurvivesSaveAndLoad()
        {
            var wb = new FindWorkbook(22); var b = new ContractBoard(DefaultContracts.All);
            b.Restore(150, null, null); b.Accept("c_village", 3);
            b.Deliver(Ready(wb, "iron_nail", 0.8f), Info("iron_nail"), wb);
            var s = b.Snapshot();
            var c = new ContractBoard(DefaultContracts.All); c.Restore(s.reputation, s.completed, s.active);
            Assert.AreEqual(DeliveryResult.NoMatchingContract, c.Deliver(Ready(wb, "iron_nail", 0.8f), Info("iron_nail"), wb), "el clavo ya se entrego");
            Assert.AreEqual(DeliveryResult.Accepted, c.Deliver(Ready(wb, "pottery_shard", 0.8f), Info("pottery_shard"), wb));
        }

        [Test]
        public void ProgressionIsCoherent_RewardsGrowWithLevelAndTypesAreVaried()
        {
            var forest = DefaultContracts.All.Where(d => d.RegionId == Biomes.ForestId).ToList();
            Assert.GreaterOrEqual(forest.Select(d => d.Kind).Distinct().Count(), 4, "cuatro tipos de objetivo distintos");
            float avgLow = (float)forest.Where(d => d.MinReputation == 0).Average(d => d.RewardMoney);
            float avgHigh = (float)forest.Where(d => d.MinReputation >= 100).Average(d => d.RewardMoney);
            Assert.Greater(avgHigh, avgLow);
        }
    }
}
