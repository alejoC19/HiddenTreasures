using System;

namespace HuesoOxido.Core
{
    /// <summary>Timbre del bip del detector segun lo que hay enterrado (solo se distingue cerca de la señal).</summary>
    public enum DetectorTone { Low, Mid, High, Relic }

    /// <summary>Datos de la tarjeta que aparece al terminar de limpiar una pieza.</summary>
    public sealed class FindCardData
    {
        public string Title;
        public string RarityName;
        public string CategoryName;
        public string Description;
        public Rarity Rarity;
        public int Value;
        public bool IsNew;
        public bool IsRelic;
        public float Seconds;
    }

    /// <summary>Presentacion de hallazgos y timbres del detector. Logica pura (sin motor), testeable.</summary>
    public static class FindPresentation
    {
        public static FindCardData BuildCard(IItemInfo info, int value, bool isNew)
        {
            bool relic = info.Id != null && info.Id.StartsWith(QuestManager.RelicPrefix, StringComparison.Ordinal);
            return new FindCardData
            {
                Title = info.DisplayName,
                RarityName = RarityStyle.NameOf(info.Rarity),
                CategoryName = RarityStyle.NameOf(info.Category),
                Description = info.Description,
                Rarity = info.Rarity,
                Value = value,
                IsNew = isNew,
                IsRelic = relic,
                Seconds = DisplaySeconds(info.Rarity, relic),
            };
        }

        /// <summary>Lo mas raro se muestra mas tiempo (3,5 s comun ... 7,3 s leyendaria; las reliquias, un poco mas).</summary>
        public static float DisplaySeconds(Rarity rarity, bool relic = false) => 3.5f + 0.95f * (int)rarity + (relic ? 0.6f : 0f);

        public static DetectorTone ToneFor(IItemInfo info)
        {
            if (info == null) return DetectorTone.Low;
            if (info.Id != null && info.Id.StartsWith(QuestManager.RelicPrefix, StringComparison.Ordinal)) return DetectorTone.Relic;
            switch (info.Rarity)
            {
                case Rarity.Common: return DetectorTone.Low;
                case Rarity.Uncommon: return DetectorTone.Mid;
                default: return DetectorTone.High;
            }
        }
    }
}
