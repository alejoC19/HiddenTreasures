using System.Collections.Generic;
using HuesoOxido.Core;

namespace HuesoOxido.Tests
{
    /// <summary>Catalogo de prueba sin ScriptableObjects (no requiere el motor).</summary>
    public sealed class TestItem : IItemInfo
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; } = "";
        public string SourceNote { get; set; } = "";
        public bool IsFiction { get; set; }
        public Rarity Rarity { get; set; }
        public FindCategory Category { get; set; }
        public float WeightKg { get; set; }
        public int BaseValue { get; set; }
    }

    public sealed class TestLookup : IItemLookup
    {
        readonly Dictionary<string, IItemInfo> _d = new Dictionary<string, IItemInfo>();
        public TestLookup Add(string id, float kg, int value, Rarity r = Rarity.Common)
        {
            _d[id] = new TestItem { Id = id, DisplayName = id, WeightKg = kg, BaseValue = value, Rarity = r };
            return this;
        }
        public bool TryGet(string id, out IItemInfo info) => _d.TryGetValue(id, out info);
    }
}
