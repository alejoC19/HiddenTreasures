using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>Datos configurables de un hallazgo. Se pueden crear como assets o por codigo.</summary>
    [CreateAssetMenu(menuName = "Hidden Treasures/Item Definition", fileName = "Item_")]
    public sealed class ItemDefinition : ScriptableObject, IItemInfo
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField, TextArea] string description;
        [SerializeField, TextArea] string sourceNote;
        [SerializeField] bool isFiction;
        [SerializeField] Rarity rarity;
        [SerializeField] FindCategory category;
        [SerializeField] float weightKg = 0.5f;
        [SerializeField] int baseValue = 10;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public string SourceNote => sourceNote;
        public bool IsFiction => isFiction;
        public Rarity Rarity => rarity;
        public FindCategory Category => category;
        public float WeightKg => weightKg;
        public int BaseValue => baseValue;

        public static ItemDefinition Create(string id, string name, string description, string sourceNote, bool fiction,
            Rarity rarity, FindCategory category, float weightKg, int baseValue)
        {
            var d = CreateInstance<ItemDefinition>();
            d.name = id;
            d.id = id; d.displayName = name; d.description = description; d.sourceNote = sourceNote;
            d.isFiction = fiction; d.rarity = rarity; d.category = category;
            d.weightKg = weightKg; d.baseValue = baseValue;
            return d;
        }
    }

    /// <summary>Catalogo de hallazgos. Si no hay assets asignados, se usa el catalogo por defecto del MVP.</summary>
    [CreateAssetMenu(menuName = "Hidden Treasures/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalog : ScriptableObject, IItemLookup
    {
        [SerializeField] List<ItemDefinition> items = new List<ItemDefinition>();
        Dictionary<string, ItemDefinition> _byId;

        public IReadOnlyList<ItemDefinition> Items => items;

        public static ItemCatalog CreateFrom(IEnumerable<ItemDefinition> defs)
        {
            var c = CreateInstance<ItemCatalog>();
            c.items = new List<ItemDefinition>(defs);
            return c;
        }

        public bool TryGet(string id, out IItemInfo info)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, ItemDefinition>();
                foreach (var i in items) if (i != null && !string.IsNullOrEmpty(i.Id)) _byId[i.Id] = i;
            }
            if (id != null && _byId.TryGetValue(id, out var d)) { info = d; return true; }
            info = null;
            return false;
        }
    }
}
