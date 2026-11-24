using System.Collections.Generic;
using System.Linq;
using HuesoOxido.Collection;
using HuesoOxido.Core;
using HuesoOxido.Economy;
using HuesoOxido.Inventory;
using HuesoOxido.Survival;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    /// <summary>Consistencia del contenido por defecto y simulacion del loop central con los modelos puros.</summary>
    public class ContentTests
    {
        static readonly List<ItemSpec> Specs = DefaultItems.Specs();

        [Test]
        public void Catalog_HasUniqueIdsAndSaneValues()
        {
            Assert.AreEqual(Specs.Count, Specs.Select(s => s.Id).Distinct().Count());
            foreach (var s in Specs)
            {
                Assert.IsNotEmpty(s.Id); Assert.IsNotEmpty(s.DisplayName); Assert.IsNotEmpty(s.Description);
                Assert.IsNotEmpty(s.SourceNote, s.Id + ": falta la nota que distingue real de ficcion");
                Assert.Greater(s.WeightKg, 0f, s.Id); Assert.Greater(s.BaseValue, 0, s.Id);
            }
        }

        [Test]
        public void Catalog_CoversAllRarities_AndValueGrowsWithRarity()
        {
            foreach (Rarity r in System.Enum.GetValues(typeof(Rarity)))
                Assert.IsTrue(Specs.Any(s => s.Rarity == r), "sin items de rareza " + r);
            double common = Specs.Where(s => s.Rarity == Rarity.Common).Average(s => s.BaseValue);
            double legendary = Specs.Where(s => s.Rarity == Rarity.Legendary).Average(s => s.BaseValue);
            Assert.Greater(legendary, common * 10);
        }

        [Test]
        public void WorldDesign_FixedFindsExist()
        {
            // WorldBuilder coloca estos dos por id: si cambian en el catalogo, el mundo queda roto.
            Assert.IsTrue(Specs.Any(s => s.Id == "golden_torc"));
            var journal = Specs.Single(s => s.Id == "expedition_journal");
            Assert.IsTrue(journal.IsFiction, "el diario es ficcion y debe estar marcado");
            Assert.IsFalse(Specs.Where(s => s.Id != "expedition_journal").Any(s => s.IsFiction));
        }

        [Test]
        public void EveryUpgradeAndPermitHasPositiveCosts()
        {
            foreach (var u in DefaultUpgrades.Create()) { Assert.Greater(u.MaxLevel, 0); Assert.IsTrue(u.Costs.All(c => c > 0)); }
            Assert.Greater(PermitService.ForestRuins.Cost, 0);
            Assert.Less(PermitService.ForestRuins.Cost, GameStartMoney, "el jugador debe poder pagar el primer permiso");
        }

        const int GameStartMoney = 120;
    }

    public class CentralLoopTests
    {
        /// <summary>Recorre el loop: permiso -> hallazgo -> peso -> baul -> vender -> mejora -> mochila mas grande.</summary>
        [Test]
        public void CentralLoop_RunsEndToEndOnPureModels()
        {
            var lookup = new SpecLookup(DefaultItems.Specs());
            var wallet = new Wallet(120);
            var permits = new PermitService(new[] { PermitService.ForestRuins });
            var collection = new CollectionLog();
            var upgrades = new UpgradeService(DefaultUpgrades.Create());
            var trade = new TradingService(lookup, wallet, collection);
            var backpack = new InventoryModel("Mochila", 8f, lookup);
            var trunk = new InventoryModel("Baul", 40f, lookup);
            var rng = new System.Random(3);

            Assert.IsFalse(permits.CanDig("forest_ruins"), "sin permiso no se excava");
            Assert.IsTrue(permits.TryBuy("permit_forest_ruins", wallet));
            Assert.IsTrue(permits.CanDig("forest_ruins"));

            // Excavar varias piezas: la mochila se llena y obliga a decidir.
            int carried = 0, leftOnGround = 0;
            foreach (var id in new[] { "trilobite_slab", "ammonite", "bronze_axe", "flint_arrowhead" })
            {
                var item = ItemInstance.Create(id, rng, 0.5f);
                collection.RegisterFind(id);
                if (backpack.TryAdd(item)) carried++; else leftOnGround++;
            }
            Assert.Greater(carried, 0);
            Assert.Greater(leftOnGround, 0, "la mochila limitada tiene que dejar piezas afuera");
            Assert.LessOrEqual(backpack.TotalWeight, 8f + 1e-3f);

            // Volver al vehiculo, guardar, vender.
            int moved = backpack.TransferAllTo(trunk);
            Assert.AreEqual(carried, moved);
            Assert.AreEqual(0, backpack.Items.Count);
            int before = wallet.Money;
            int earned = trade.SellAll(trunk);
            Assert.Greater(earned, 0);
            Assert.AreEqual(before + earned, wallet.Money);

            // Progresion: con ese dinero se compra una mejora y entra mas peso.
            wallet.Add(500);
            Assert.IsTrue(upgrades.TryPurchase("backpack", wallet));
            backpack.SetCapacity(8f + upgrades.Bonus(UpgradeKind.BackpackCapacity));
            Assert.AreEqual(12f, backpack.CapacityKg, 1e-4f);
        }

        [Test]
        public void ExhaustionPenalty_LosesPartOfBackpackOnly()
        {
            var lookup = new SpecLookup(DefaultItems.Specs());
            var backpack = new InventoryModel("Mochila", 100f, lookup);
            var trunk = new InventoryModel("Baul", 100f, lookup);
            for (int i = 0; i < 5; i++) { backpack.TryAdd(new ItemInstance("b" + i, "pottery_shard", 1f)); trunk.TryAdd(new ItemInstance("t" + i, "pottery_shard", 1f)); }
            var lost = backpack.RemoveRandomFraction(0.4f, new System.Random(1));
            Assert.AreEqual(2, lost.Count);
            Assert.AreEqual(3, backpack.Items.Count);
            Assert.AreEqual(5, trunk.Items.Count, "lo guardado en el baul esta a salvo");
        }

        sealed class SpecLookup : IItemLookup
        {
            readonly Dictionary<string, IItemInfo> _d;
            public SpecLookup(IEnumerable<ItemSpec> specs) { _d = specs.ToDictionary(s => s.Id, s => (IItemInfo)s); }
            public bool TryGet(string id, out IItemInfo info) => _d.TryGetValue(id, out info);
        }
    }
}
