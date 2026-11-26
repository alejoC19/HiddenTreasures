using System;
using System.Collections.Generic;
using System.Text;
using HuesoOxido.Collection;
using HuesoOxido.Core;
using HuesoOxido.Economy;
using HuesoOxido.Inventory;
using HuesoOxido.SaveSystem;
using HuesoOxido.Survival;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Comprueba con el serializador REAL de Unity (JsonUtility) que (1) un guardado anterior a los sistemas de expedicion carga y deja
    /// los valores iniciales, y (2) un guardado nuevo hace ida y vuelta sin perder datos. Lo usan un metodo de Editor (lote) y un test de PlayMode.
    /// </summary>
    public static class SaveCompatibilityCheck
    {
        sealed class Lookup : IItemLookup
        {
            readonly Dictionary<string, IItemInfo> _d = new Dictionary<string, IItemInfo>();
            public Lookup() { foreach (var s in DefaultItems.Specs()) _d[s.Id] = s; }
            public bool TryGet(string id, out IItemInfo info) => _d.TryGetValue(id, out info);
        }

        static SaveBundle NewBundle()
        {
            var l = new Lookup();
            return new SaveBundle
            {
                Wallet = new Wallet(0), Backpack = new InventoryModel("Mochila", 8f, l), Trunk = new InventoryModel("Baul", 40f, l),
                Upgrades = new UpgradeService(DefaultUpgrades.Create()), Permits = new PermitService(PermitService.ForBiomes()),
                Collection = new CollectionLog(), Energy = new EnergyModel(100f), Clock = new GameClock(17f, 1), Weather = new WeatherModel(7),
                Fuel = new FuelTank(40f), Quests = new QuestManager(),
                Needs = new NeedsModel(), Loadout = new Loadout(), Workbook = new FindWorkbook(1337), Contracts = new ContractBoard(DefaultContracts.All),
            };
        }

        /// <summary>Devuelve true si todo coincide; el informe lista cada comprobacion (OK / FALLO).</summary>
        public static bool Verify(string legacyJson, out string report)
        {
            var sb = new StringBuilder(); bool ok = true;
            void Check(string name, bool cond) { sb.AppendLine((cond ? "OK    " : "FALLO ") + name); if (!cond) ok = false; }
            try
            {
                // 1) Guardado anterior (sin Needs/Loadout/FindStates/Contracts/BiomeId).
                var old = SaveMigrator.Migrate(JsonUtility.FromJson<SaveData>(legacyJson));
                Check("el guardado anterior se lee y migra", old != null);
                Check("bloques nuevos presentes pero vacios (Has = false)", old.Needs != null && !old.Needs.Has && old.Loadout != null && !old.Loadout.Has && old.Contracts != null && !old.Contracts.Has);
                Check("bioma vacio = bosque", string.IsNullOrEmpty(old.BiomeId) && Biomes.ById(old.BiomeId).IsDefault);
                var b = NewBundle();
                GameSaveCodec.Restore(old, b);
                Check("dinero, mochila y baul restaurados", b.Wallet.Money == 135 && b.Backpack.Items.Count == 2 && b.Trunk.Items.Count == 1);
                Check("permiso, hora y combustible restaurados", b.Permits.Owns("permit_forest_ruins") && Math.Abs(b.Clock.Hour - 14.25f) < 1e-3f && b.Clock.Day == 3 && Math.Abs(b.Fuel.Level - 22f) < 1e-3f);
                Check("mision restaurada", b.Quests.HasQuest && b.Quests.Progress == 1);
                Check("necesidades al inicial", Math.Abs(b.Needs.Hydration - 1f) < 1e-4f && Math.Abs(b.Needs.BodyTemperatureC - 37f) < 1e-3f);
                Check("equipo inicial", b.Loadout.Has(EquipmentKind.Detector) && b.Loadout.Has(EquipmentKind.Shovel) && Math.Abs(b.Loadout.WaterLiters - 1f) < 1e-4f);
                Check("contratos en cero", b.Contracts.Reputation == 0 && b.Contracts.Active.Count == 0);

                // 2) Guardado nuevo: ida y vuelta completa por JSON.
                var a = NewBundle();
                GameSaveCodec.Restore(old, a);
                a.Needs.Tick(2f, new NeedsContext { Moving = true, AmbientC = 28f });
                a.Loadout.Grant("eq_flashlight"); a.Loadout.Equip("eq_flashlight"); a.Loadout.AddWater(2f);
                var item = new ItemInstance("UID-XYZ", "roman_denarius", 0.7f);
                a.Workbook.Clean(item, 0.7f, 0.6f);
                a.Contracts.Restore(150, null, null); a.Contracts.Accept("c_village", 2);
                var data = new SaveData { BiomeId = Biomes.ForestId };
                GameSaveCodec.Capture(data, a);
                string json = JsonUtility.ToJson(data, true);
                var back = SaveMigrator.Migrate(JsonUtility.FromJson<SaveData>(json));
                var c = NewBundle();
                GameSaveCodec.Restore(back, c);
                Check("necesidades ida y vuelta", Math.Abs(a.Needs.Hydration - c.Needs.Hydration) < 1e-5f && Math.Abs(a.Needs.BodyTemperatureC - c.Needs.BodyTemperatureC) < 1e-4f);
                Check("equipo y provisiones ida y vuelta", c.Loadout.Has(EquipmentKind.Flashlight) && Math.Abs(c.Loadout.WaterLiters - a.Loadout.WaterLiters) < 1e-4f);
                Check("estado de hallazgo ida y vuelta", Math.Abs(a.Workbook.State(item).Dirt - c.Workbook.State(item).Dirt) < 1e-5f && a.Workbook.State(item).Authentic == c.Workbook.State(item).Authentic);
                Check("contrato activo ida y vuelta", c.Contracts.IsActive("c_village") && c.Contracts.Reputation == 150);
                Check("el dinero y la mochila siguen iguales", c.Wallet.Money == 135 && c.Backpack.Items.Count == 2);
            }
            catch (Exception e) { sb.AppendLine("FALLO excepcion: " + e); ok = false; }
            report = sb.ToString();
            return ok;
        }
    }
}
