using UnityEngine;

namespace HuesoOxido.Core
{
    public static class RarityStyle
    {
        public static Color ColorOf(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return new Color(0.80f, 0.80f, 0.78f);
                case Rarity.Uncommon: return new Color(0.45f, 0.85f, 0.45f);
                case Rarity.Rare: return new Color(0.35f, 0.65f, 1.00f);
                case Rarity.Epic: return new Color(0.75f, 0.45f, 1.00f);
                default: return new Color(1.00f, 0.78f, 0.25f);
            }
        }

        public static string NameOf(Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return "Común";
                case Rarity.Uncommon: return "Poco común";
                case Rarity.Rare: return "Rara";
                case Rarity.Epic: return "Épica";
                default: return "Legendaria";
            }
        }

        public static string NameOf(FindCategory c)
        {
            switch (c)
            {
                case FindCategory.Fossil: return "Paleontología";
                case FindCategory.Archaeology: return "Arqueología";
                case FindCategory.History: return "Historia";
                default: return "Geología";
            }
        }
    }
}
