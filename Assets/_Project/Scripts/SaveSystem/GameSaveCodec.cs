using System;
using System.Collections.Generic;
using HuesoOxido.Collection;
using HuesoOxido.Core;
using HuesoOxido.Economy;
using HuesoOxido.Inventory;
using HuesoOxido.Survival;

namespace HuesoOxido.SaveSystem
{
    /// <summary>Los modelos puros del juego que forman la partida (sin motor). Lo arma GameServices.</summary>
    public sealed class SaveBundle
    {
        public Wallet Wallet;
        public InventoryModel Backpack, Trunk;
        public UpgradeService Upgrades;
        public PermitService Permits;
        public CollectionLog Collection;
        public EnergyModel Energy;
        public GameClock Clock;
        public WeatherModel Weather;
        public FuelTank Fuel;
        public QuestManager Quests;
        // Sistemas de expedicion: opcionales (null = no se guardan ni se restauran)
        public NeedsModel Needs;
        public Loadout Loadout;
        public FindWorkbook Workbook;
        public ContractBoard Contracts;
    }

    /// <summary>
    /// Pasa el estado de los modelos puros a SaveData y viceversa. Lo que depende de la escena (posiciones, sitios excavados,
    /// objetos en el suelo) lo agrega el compositor. Al estar aqui, todo el viaje guardar->cargar se prueba sin Unity.
    /// </summary>
    public static class GameSaveCodec
    {
        public static ItemDto ToDto(ItemInstance i) => new ItemDto { Uid = i.Uid, DefinitionId = i.DefinitionId, Condition = i.Condition };
        public static ItemInstance FromDto(ItemDto d) => new ItemInstance(d.Uid, d.DefinitionId, d.Condition);

        public static void Capture(SaveData data, SaveBundle s)
        {
            data.Money = s.Wallet.Money;
            data.Backpack.Clear(); data.Trunk.Clear(); data.Upgrades.Clear(); data.Permits.Clear(); data.Collection.Clear();
            foreach (var i in s.Backpack.Items) data.Backpack.Add(ToDto(i));
            foreach (var i in s.Trunk.Items) data.Trunk.Add(ToDto(i));
            foreach (var kv in s.Upgrades.Snapshot()) data.Upgrades.Add(new UpgradeLevelDto { Id = kv.Key, Level = kv.Value });
            data.Permits.AddRange(s.Permits.Snapshot());
            foreach (var e in s.Collection.Entries) data.Collection.Add(new CollectionEntryDto { Id = e.Id, TimesFound = e.TimesFound, Displayed = e.Displayed });
            data.Energy = s.Energy.Current;
            data.Hour = s.Clock.Hour; data.Day = s.Clock.Day;
            data.Weather = (int)s.Weather.Current;
            data.Fuel = s.Fuel.Level;
            if (s.Quests != null && s.Quests.HasQuest)
            {
                data.QuestId = s.Quests.Current.Id; data.QuestProgress = s.Quests.Progress; data.QuestCompleted = s.Quests.IsCompleted;
            }
            else { data.QuestId = null; data.QuestProgress = 0; data.QuestCompleted = false; }

            if (s.Needs != null)
            {
                var n = s.Needs.Snapshot();
                data.Needs = new NeedsDto { Has = true, Hydration = n.hydration, Satiety = n.satiety, BodyC = n.bodyC };
            }
            if (s.Loadout != null)
            {
                var l = s.Loadout.Snapshot();
                data.Loadout = new LoadoutDto { Has = true, Owned = l.owned, Equipped = l.equipped, Water = l.water, Rations = l.rations };
            }
            data.FindStates.Clear();
            if (s.Workbook != null)
                foreach (var f in s.Workbook.Snapshot()) data.FindStates.Add(new FindStateDto { Uid = f.uid, ItemId = f.itemId, Dirt = f.dirt, Identified = f.identified, Authentic = f.authentic });
            if (s.Contracts != null)
            {
                var c = s.Contracts.Snapshot();
                var dto = new ContractsDto { Has = true, Reputation = c.reputation, Completed = c.completed };
                foreach (var a in c.active) dto.Active.Add(new ActiveContractDto { Id = a.id, Delivered = a.delivered, Day = a.day, DeliveredIds = a.deliveredIds });
                data.Contracts = dto;
            }
        }

        /// <param name="afterUpgrades">Se llama tras restaurar las mejoras y antes de los objetos (para recalcular capacidades).</param>
        public static void Restore(SaveData data, SaveBundle s, Action afterUpgrades = null)
        {
            var levels = new Dictionary<string, int>();
            foreach (var u in data.Upgrades) levels[u.Id] = u.Level;
            s.Upgrades.Restore(levels);
            afterUpgrades?.Invoke();

            s.Wallet.Set(data.Money);
            var pack = new List<ItemInstance>(); foreach (var i in data.Backpack) pack.Add(FromDto(i));
            var trunk = new List<ItemInstance>(); foreach (var i in data.Trunk) trunk.Add(FromDto(i));
            s.Backpack.ReplaceAll(pack);
            s.Trunk.ReplaceAll(trunk);
            s.Permits.Restore(data.Permits);
            var col = new List<CollectionEntry>();
            foreach (var e in data.Collection) col.Add(new CollectionEntry { Id = e.Id, TimesFound = e.TimesFound, Displayed = e.Displayed });
            s.Collection.Restore(col);

            if (data.Energy >= 0f) s.Energy.Set(data.Energy); else s.Energy.RestoreFull();
            s.Clock.Set(data.Hour, data.Day);
            s.Weather.Set((WeatherKind)Math.Max(0, Math.Min(3, data.Weather)));
            if (data.Fuel >= 0f) s.Fuel.SetLevel(data.Fuel);

            if (s.Quests != null)
            {
                if (!string.IsNullOrEmpty(data.QuestId)) s.Quests.Restore(data.QuestId, data.QuestProgress, data.QuestCompleted);
                else s.Quests.Start(QuestManager.RelicHunt());      // guardado viejo sin mision
            }

            // Sistemas de expedicion: un guardado viejo (Has = false) deja los valores iniciales.
            if (s.Needs != null) { if (data.Needs != null && data.Needs.Has) s.Needs.Restore(data.Needs.Hydration, data.Needs.Satiety, data.Needs.BodyC); else s.Needs.RestoreAll(); }
            if (s.Loadout != null)
            {
                if (data.Loadout != null && data.Loadout.Has) s.Loadout.Restore(data.Loadout.Owned, data.Loadout.Equipped, data.Loadout.Water, data.Loadout.Rations);
                else { var st = Loadout.Starter(); var snap = st.Snapshot(); s.Loadout.Restore(snap.owned, snap.equipped, snap.water, snap.rations); }
            }
            if (s.Workbook != null)
            {
                var states = new List<(string, string, float, bool, bool)>();
                if (data.FindStates != null) foreach (var f in data.FindStates) states.Add((f.Uid, f.ItemId, f.Dirt, f.Identified, f.Authentic));
                s.Workbook.Restore(states);
            }
            if (s.Contracts != null)
            {
                if (data.Contracts != null && data.Contracts.Has)
                {
                    var act = new List<(string, int, int, string)>();
                    foreach (var a in data.Contracts.Active) act.Add((a.Id, a.Delivered, a.Day, a.DeliveredIds));
                    s.Contracts.Restore(data.Contracts.Reputation, data.Contracts.Completed, act);
                }
                else s.Contracts.Restore(0, null, null);
            }
        }
    }
}
