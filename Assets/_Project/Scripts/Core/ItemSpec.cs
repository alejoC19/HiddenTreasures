namespace HuesoOxido.Core
{
    /// <summary>Datos puros de un hallazgo. Es la fuente del catalogo por defecto y de los tests.</summary>
    public sealed class ItemSpec : IItemInfo
    {
        public ItemSpec(string id, string name, string description, string sourceNote, bool fiction,
                        Rarity rarity, FindCategory category, float weightKg, int baseValue)
        {
            Id = id; DisplayName = name; Description = description; SourceNote = sourceNote; IsFiction = fiction;
            Rarity = rarity; Category = category; WeightKg = weightKg; BaseValue = baseValue;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string SourceNote { get; }
        public bool IsFiction { get; }
        public Rarity Rarity { get; }
        public FindCategory Category { get; }
        public float WeightKg { get; }
        public int BaseValue { get; }
    }
}
