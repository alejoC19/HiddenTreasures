using HuesoOxido.Collection;
using HuesoOxido.Core;
using HuesoOxido.Economy;
using HuesoOxido.Inventory;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class EconomyTests
    {
        [Test]
        public void Wallet_SpendAndAdd()
        {
            var w = new Wallet(50);
            Assert.IsFalse(w.TrySpend(51));
            Assert.IsTrue(w.TrySpend(20));
            w.Add(5);
            Assert.AreEqual(35, w.Money);
        }

        [Test]
        public void Pricing_ConditionScalesBetweenHalfAndFull()
        {
            var info = new TestItem { Id = "x", BaseValue = 100 };
            Assert.AreEqual(50, ItemPricing.ValueOf(info, 0f));
            Assert.AreEqual(75, ItemPricing.ValueOf(info, 0.5f));
            Assert.AreEqual(100, ItemPricing.ValueOf(info, 1f));
        }

        [Test]
        public void Sell_RemovesItemAndPays()
        {
            var lookup = new TestLookup().Add("a", 1f, 100);
            var wallet = new Wallet(0);
            var trade = new TradingService(lookup, wallet, new CollectionLog());
            var inv = new InventoryModel("m", 10f, lookup);
            inv.TryAdd(new ItemInstance("1", "a", 1f));
            Assert.AreEqual(100, trade.Sell(inv, "1"));
            Assert.AreEqual(100, wallet.Money);
            Assert.AreEqual(0, inv.Items.Count);
            Assert.AreEqual(-1, trade.Sell(inv, "1"));
        }

        [Test]
        public void Restore_CostsMoneyAndRaisesValue()
        {
            var lookup = new TestLookup().Add("a", 1f, 200);
            var wallet = new Wallet(500);
            var trade = new TradingService(lookup, wallet, new CollectionLog());
            var inv = new InventoryModel("m", 10f, lookup);
            inv.TryAdd(new ItemInstance("1", "a", 0.5f));
            int before = trade.PriceOf(inv.Find("1"));
            Assert.IsTrue(trade.Restore(inv, "1"));
            Assert.Greater(trade.PriceOf(inv.Find("1")), before);
            Assert.Less(wallet.Money, 500);
            Assert.IsFalse(trade.Restore(inv, "1"), "ya restaurado");
        }

        [Test]
        public void Display_OnlyFirstCopyAndItLeavesInventory()
        {
            var lookup = new TestLookup().Add("a", 1f, 50);
            var col = new CollectionLog();
            var trade = new TradingService(lookup, new Wallet(0), col);
            var inv = new InventoryModel("m", 10f, lookup);
            inv.TryAdd(new ItemInstance("1", "a", 1f));
            inv.TryAdd(new ItemInstance("2", "a", 1f));
            col.RegisterFind("a"); col.RegisterFind("a");
            Assert.IsTrue(trade.Display(inv, "1"));
            Assert.IsFalse(trade.Display(inv, "2"), "ya hay una exhibida: la otra es duplicado");
            Assert.AreEqual(1, inv.Items.Count);
            Assert.IsTrue(col.IsDisplayed("a"));
        }

        [Test]
        public void Upgrades_PurchaseLevelsAndBonus()
        {
            var up = new UpgradeService(DefaultUpgrades.Create());
            var wallet = new Wallet(1000);
            Assert.AreEqual(0f, up.Bonus(UpgradeKind.BackpackCapacity));
            Assert.IsTrue(up.TryPurchase("backpack", wallet));
            Assert.IsTrue(up.TryPurchase("backpack", wallet));
            Assert.AreEqual(8f, up.Bonus(UpgradeKind.BackpackCapacity), 1e-4f);
            Assert.AreEqual(1000 - 120 - 300, wallet.Money);
        }

        [Test]
        public void Upgrades_CannotExceedMaxOrAffordNothing()
        {
            var up = new UpgradeService(DefaultUpgrades.Create());
            Assert.IsFalse(up.TryPurchase("backpack", new Wallet(10)));
            var rich = new Wallet(100000);
            for (int i = 0; i < 10; i++) up.TryPurchase("tank", rich);
            Assert.AreEqual(2, up.Level("tank"));
            Assert.AreEqual(-1, up.NextCost("tank"));
        }

        [Test]
        public void Permit_GatesDigging()
        {
            var permits = new PermitService(new[] { PermitService.ForestRuins });
            Assert.IsFalse(permits.CanDig("forest_ruins"));
            Assert.IsFalse(permits.TryBuy("permit_forest_ruins", new Wallet(10)));
            Assert.IsTrue(permits.TryBuy("permit_forest_ruins", new Wallet(100)));
            Assert.IsTrue(permits.CanDig("forest_ruins"));
            Assert.IsFalse(permits.CanDig("otra_zona"));
        }
    }
}
