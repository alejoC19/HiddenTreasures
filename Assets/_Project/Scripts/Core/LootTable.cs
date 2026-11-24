using System;
using System.Collections.Generic;

namespace HuesoOxido.Core
{
    /// <summary>Eleccion ponderada de hallazgos por rareza (determinista con un Random con semilla).</summary>
    public static class LootTable
    {
        public static float Weight(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return 50f;
                case Rarity.Uncommon: return 28f;
                case Rarity.Rare: return 13f;
                case Rarity.Epic: return 6f;
                default: return 2.5f;
            }
        }

        public static IItemInfo Pick(IReadOnlyList<IItemInfo> items, Random rng)
        {
            if (items == null || items.Count == 0) throw new ArgumentException("Sin items");
            float total = 0f;
            for (int i = 0; i < items.Count; i++) total += Weight(items[i].Rarity);
            double roll = rng.NextDouble() * total;
            for (int i = 0; i < items.Count; i++)
            {
                roll -= Weight(items[i].Rarity);
                if (roll <= 0) return items[i];
            }
            return items[items.Count - 1];
        }
    }
}
