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
}
