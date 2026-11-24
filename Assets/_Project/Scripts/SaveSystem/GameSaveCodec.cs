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
        }
    }
}
