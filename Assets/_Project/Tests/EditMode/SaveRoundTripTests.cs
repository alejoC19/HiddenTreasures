using System.Linq;
using HuesoOxido.Collection;
using HuesoOxido.Core;
using HuesoOxido.Economy;
using HuesoOxido.Inventory;
using HuesoOxido.SaveSystem;
using HuesoOxido.Survival;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    /// <summary>Viaje completo guardar -> (JSON) -> cargar de los modelos puros, y venta con su evento.</summary>
    public sealed class SaveRoundTripTests
    {
        static TestLookup Lookup() => new TestLookup().Add("a", 1f, 100).Add("b", 2f, 40).Add("relic_gold_tunjo", 0.4f, 900, Rarity.Epic);

        static SaveBundle NewBundle(TestLookup l) => new SaveBundle
        {
            Wallet = new Wallet(120),
            Backpack = new InventoryModel("Mochila", 8f, l),
            Trunk = new InventoryModel("Baul", 40f, l),
            Upgrades = new UpgradeService(DefaultUpgrades.Create()),
            Permits = new PermitService(new[] { PermitService.ForestRuins }),
            Collection = new CollectionLog(),
            Energy = new EnergyModel(100f),
            Clock = new GameClock(17f, 1),
            Weather = new WeatherModel(7),
            Fuel = new FuelTank(40f),
            Quests = new QuestManager(),
        };

        [Test]
        public void FullState_SurvivesSaveAndLoad()
        {
            var l = Lookup();
            var a = NewBundle(l);
            a.Wallet.Set(5000);
            Assert.IsTrue(a.Permits.TryBuy(PermitService.ForestRuins.Id, a.Wallet));
            a.Wallet.Set(777);
            a.Backpack.TryAdd(new ItemInstance("u1", "a", 0.6f));
            a.Backpack.TryAdd(new ItemInstance("u2", "relic_gold_tunjo", 0.9f));
            a.Trunk.TryAdd(new ItemInstance("u3", "b", 0.2f));
            a.Energy.Set(42f);
            a.Clock.Set(21.5f, 3);
            a.Weather.Set(WeatherKind.Rain);
            a.Fuel.SetLevel(12.5f);
            a.Collection.RegisterFind("a");
            a.Quests.Start(QuestManager.RelicHunt());
            a.Quests.ReportFind("relic_gold_tunjo");

            var data = new SaveData();
            GameSaveCodec.Capture(data, a);
            data.WorldSeed = 1337;
            var loaded = SaveMigrator.Migrate(data);     // el paso a JSON lo hace JsonUtility en SaveManager (solo existe en Unity)

            var b = NewBundle(l);
            GameSaveCodec.Restore(loaded, b);

            Assert.AreEqual(777, b.Wallet.Money);
            CollectionAssert.AreEquivalent(new[] { "u1", "u2" }, b.Backpack.Items.Select(i => i.Uid));
            Assert.AreEqual(0.6f, b.Backpack.Find("u1").Condition, 1e-4f);
            CollectionAssert.AreEqual(new[] { "u3" }, b.Trunk.Items.Select(i => i.Uid));
            Assert.AreEqual(42f, b.Energy.Current, 1e-3f);
            Assert.AreEqual(21.5f, b.Clock.Hour, 1e-3f);
            Assert.AreEqual(3, b.Clock.Day);
            Assert.AreEqual(WeatherKind.Rain, b.Weather.Current);
            Assert.AreEqual(12.5f, b.Fuel.Level, 1e-3f);
            Assert.AreEqual(1, b.Collection.Entries.Count(e => e.Id == "a"));
            Assert.IsTrue(b.Permits.CanDig("forest_ruins"));
            Assert.AreEqual("Reliquias: 1/3", b.Quests.ProgressText);
            Assert.AreEqual(1337, loaded.WorldSeed);
        }

        [Test]
        public void CompletedQuest_StaysCompleted_AndDoesNotPayAgain()
        {
            var l = Lookup();
            var a = NewBundle(l);
            a.Quests.Start(QuestManager.RelicHunt());
            for (int i = 0; i < 3; i++) a.Quests.ReportFind("relic_gold_tunjo");
            Assert.IsTrue(a.Quests.IsCompleted);

            var data = new SaveData();
            GameSaveCodec.Capture(data, a);
            var b = NewBundle(l);
            int completedEvents = 0;
            b.Quests.Completed += _ => completedEvents++;
            GameSaveCodec.Restore(data, b);
            Assert.IsTrue(b.Quests.IsCompleted);
            Assert.AreEqual(0, completedEvents, "cargar no vuelve a disparar la recompensa");
        }

        [Test]
        public void OldSaveWithoutQuest_StartsTheRelicHunt()
        {
            var b = NewBundle(Lookup());
            var data = SaveMigrator.Migrate(new SaveData { QuestId = null });
            GameSaveCodec.Restore(data, b);
            Assert.IsTrue(b.Quests.HasQuest);
            Assert.AreEqual("Reliquias: 0/3", b.Quests.ProgressText);
        }

        [Test]
        public void Sell_RaisesEvent_AndEmptiesInventory()
        {
            var l = Lookup();
            var wallet = new Wallet(0);
            var trade = new TradingService(l, wallet, new CollectionLog());
            var inv = new InventoryModel("m", 20f, l);
            inv.TryAdd(new ItemInstance("1", "a", 1f));
            inv.TryAdd(new ItemInstance("2", "b", 1f));
            int events = 0, lastTotal = 0, lastCount = 0;
            trade.Sold += (t, c) => { events++; lastTotal = t; lastCount = c; };

            Assert.AreEqual(100, trade.Sell(inv, "1"));
            Assert.AreEqual(1, events);
            Assert.AreEqual(100, lastTotal);

            Assert.AreEqual(40, trade.SellAll(inv));
            Assert.AreEqual(2, events, "vender todo es un solo evento");
            Assert.AreEqual(1, lastCount);
            Assert.AreEqual(0, inv.Items.Count);
            Assert.AreEqual(140, wallet.Money);

            Assert.AreEqual(0, trade.SellAll(inv));
            Assert.AreEqual(2, events, "no hay evento si no se vendio nada");
        }
    }
}
