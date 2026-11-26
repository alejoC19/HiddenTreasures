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
    /// <summary>Guardado de los sistemas de expedicion: ida y vuelta, y compatibilidad con guardados anteriores.</summary>
    public sealed class ExpeditionSaveTests
    {
        static SaveBundle Bundle(bool withExpedition)
        {
            var l = new TestLookup().Add("a", 1f, 100);
            var b = new SaveBundle
            {
                Wallet = new Wallet(50), Backpack = new InventoryModel("Mochila", 8f, l), Trunk = new InventoryModel("Baul", 40f, l),
                Upgrades = new UpgradeService(DefaultUpgrades.Create()), Permits = new PermitService(new[] { PermitService.ForestRuins }),
                Collection = new CollectionLog(), Energy = new EnergyModel(100f), Clock = new GameClock(17f, 1), Weather = new WeatherModel(7),
                Fuel = new FuelTank(40f), Quests = new QuestManager(),
            };
            if (withExpedition)
            {
                b.Needs = new NeedsModel(); b.Loadout = Loadout.Starter(); b.Workbook = new FindWorkbook(9); b.Contracts = new ContractBoard(DefaultContracts.All);
            }
            return b;
        }

        [Test]
        public void ExpeditionSystemsSurviveSaveAndLoad()
        {
            var a = Bundle(true);
            a.Needs.Tick(3f, new NeedsContext { Moving = true, AmbientC = 30f });
            a.Loadout.Grant("eq_flashlight"); a.Loadout.Equip("eq_flashlight"); a.Loadout.TakeWater(0.5f);
            var item = new ItemInstance("z1", "roman_denarius", 0.7f);
            a.Workbook.Clean(item, 0.7f, 0.6f);
            a.Contracts.Accept("c_pottery", 2);

            var data = new SaveData();
            GameSaveCodec.Capture(data, a);

            var b = Bundle(true);
            GameSaveCodec.Restore(data, b);
            Assert.AreEqual(a.Needs.Hydration, b.Needs.Hydration, 1e-5f);
            Assert.IsTrue(b.Loadout.Has(EquipmentKind.Flashlight)); Assert.AreEqual(0.5f, b.Loadout.WaterLiters, 1e-4f);
            Assert.AreEqual(a.Workbook.State(item).Dirt, b.Workbook.State(item).Dirt, 1e-5f);
            Assert.IsTrue(b.Contracts.IsActive("c_pottery"));
        }

        [Test]
        public void OldSaveWithoutExpeditionDataRestoresStarterValues()
        {
            var data = new SaveData();                    // un guardado anterior: los bloques opcionales llegan vacios (Has = false)
            data.Needs = null; data.Loadout = null; data.FindStates = null; data.Contracts = null;
            SaveMigrator.Migrate(data);
            var b = Bundle(true);
            b.Needs.Tick(5f, new NeedsContext { Sprinting = true, AmbientC = 35f });
            b.Loadout.Restore(null, null, 0f, 0);
            GameSaveCodec.Restore(data, b);
            Assert.AreEqual(1f, b.Needs.Hydration);
            Assert.IsTrue(b.Loadout.Has(EquipmentKind.Detector)); Assert.AreEqual(1f, b.Loadout.WaterLiters);
            Assert.AreEqual(0, b.Contracts.Reputation);
        }

        [Test]
        public void BundlesWithoutExpeditionSystemsStillWork()
        {
            var a = Bundle(false); var data = new SaveData();
            GameSaveCodec.Capture(data, a);
            Assert.IsFalse(data.Needs.Has); Assert.IsFalse(data.Contracts.Has);
            Assert.DoesNotThrow(() => GameSaveCodec.Restore(data, Bundle(false)));
        }
    }
}
