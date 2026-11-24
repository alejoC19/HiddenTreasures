using System;
using System.Collections.Generic;

namespace HuesoOxido.SaveSystem
{
    /// <summary>DTOs serializables. Solo ids y valores simples; nunca referencias a ScriptableObjects.</summary>
    [Serializable]
    public sealed class ItemDto
    {
        public string Uid;
        public string DefinitionId;
        public float Condition;
    }

    [Serializable]
    public sealed class UpgradeLevelDto { public string Id; public int Level; }

    [Serializable]
    public sealed class CollectionEntryDto { public string Id; public int TimesFound; public bool Displayed; }

    [Serializable]
    public sealed class Vec3Dto { public float X, Y, Z; public float Yaw; }

    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string SavedAtUtc;
        public int Money;
        public List<ItemDto> Backpack = new List<ItemDto>();
        public List<ItemDto> Trunk = new List<ItemDto>();
        public List<UpgradeLevelDto> Upgrades = new List<UpgradeLevelDto>();
        public List<string> Permits = new List<string>();
        public List<CollectionEntryDto> Collection = new List<CollectionEntryDto>();
        // Estado de la partida
        public float Energy = -1f;
        public float Hour = 17f;
        public int Day = 1;
        public int Weather;
        public float Fuel = -1f;
        public List<string> ExcavatedSites = new List<string>();
        public Vec3Dto Player = new Vec3Dto();
        public Vec3Dto Vehicle = new Vec3Dto();
        public bool HasPositions;
    }

    public static class SaveMigrator
    {
        /// <summary>Lleva datos de versiones viejas a la actual. Hoy solo hay v1; aqui van las migraciones futuras.</summary>
        public static SaveData Migrate(SaveData data)
        {
            if (data == null) return null;
            if (data.Version > SaveData.CurrentVersion) return null; // guardado de una version futura
            if (data.Backpack == null) data.Backpack = new List<ItemDto>();
            if (data.Trunk == null) data.Trunk = new List<ItemDto>();
            if (data.Upgrades == null) data.Upgrades = new List<UpgradeLevelDto>();
            if (data.Permits == null) data.Permits = new List<string>();
            if (data.Collection == null) data.Collection = new List<CollectionEntryDto>();
            if (data.ExcavatedSites == null) data.ExcavatedSites = new List<string>();
            if (data.Player == null) data.Player = new Vec3Dto();
            if (data.Vehicle == null) data.Vehicle = new Vec3Dto();
            data.Version = SaveData.CurrentVersion;
            return data;
        }
    }
}
