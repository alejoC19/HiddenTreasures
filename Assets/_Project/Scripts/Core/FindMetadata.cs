using System;
using System.Collections.Generic;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Contexto de un tipo de hallazgo: cuanto tiene de antiguo, de donde viene, su historia y que tan probable es que sea falso.
    /// Datos puros; el contenido final vendra de las ruinas y los misterios de cada region.
    /// </summary>
    public sealed class FindProfile
    {
        public string ItemId, Origin, Story;
        public int AgeYears;
        /// <summary>0..1: probabilidad de que una pieza de este tipo sea falsa (se decide una vez, de forma estable por pieza).</summary>
        public float FakeChance;
        /// <summary>0..1: suciedad con la que sale de la tierra.</summary>
        public float InitialGrime = 0.7f;
    }

    public static class FindProfiles
    {
        static readonly Dictionary<string, FindProfile> Map = new Dictionary<string, FindProfile>
        {
            { "flint_arrowhead", new FindProfile { ItemId = "flint_arrowhead", AgeYears = 6000, Origin = "Cazadores del valle", Story = "Tallada para una caza que nadie recuerda.", FakeChance = 0.10f, InitialGrime = 0.5f } },
            { "pottery_shard", new FindProfile { ItemId = "pottery_shard", AgeYears = 1800, Origin = "Aldea junto al río", Story = "Parte de una vasija de uso diario.", FakeChance = 0.02f, InitialGrime = 0.8f } },
            { "iron_nail", new FindProfile { ItemId = "iron_nail", AgeYears = 700, Origin = "Carpintería medieval", Story = "Sostuvo una viga que ya no existe.", FakeChance = 0.01f, InitialGrime = 0.9f } },
            { "bronze_fibula", new FindProfile { ItemId = "bronze_fibula", AgeYears = 2300, Origin = "Taller de bronce", Story = "Un broche que prendió una capa.", FakeChance = 0.12f, InitialGrime = 0.75f } },
            { "medieval_key", new FindProfile { ItemId = "medieval_key", AgeYears = 800, Origin = "Casa fuerte", Story = "Abría algo que alguien quiso proteger.", FakeChance = 0.08f, InitialGrime = 0.8f } },
            { "roman_denarius", new FindProfile { ItemId = "roman_denarius", AgeYears = 1900, Origin = "Ruta comercial romana", Story = "Moneda de plata perdida en un camino.", FakeChance = 0.20f, InitialGrime = 0.7f } },
            { "bronze_axe", new FindProfile { ItemId = "bronze_axe", AgeYears = 3200, Origin = "Fundición antigua", Story = "Herramienta y símbolo de rango.", FakeChance = 0.10f, InitialGrime = 0.7f } },
            { "expedition_journal", new FindProfile { ItemId = "expedition_journal", AgeYears = 90, Origin = "Expedición Valdés", Story = "Las últimas páginas cuentan cómo terminó.", FakeChance = 0f, InitialGrime = 0.6f } },
            { "relic_gold_tunjo", new FindProfile { ItemId = "relic_gold_tunjo", AgeYears = 1100, Origin = "Ofrenda prehispánica", Story = "Figura votiva de oro.", FakeChance = 0.25f, InitialGrime = 0.6f } },
            { "relic_ceramic_vessel", new FindProfile { ItemId = "relic_ceramic_vessel", AgeYears = 1000, Origin = "Ritual prehispánico", Story = "Contuvo algo sagrado.", FakeChance = 0.15f, InitialGrime = 0.8f } },
            { "relic_jade_pendant", new FindProfile { ItemId = "relic_jade_pendant", AgeYears = 1300, Origin = "Taller ceremonial", Story = "Jade tallado por manos expertas.", FakeChance = 0.25f, InitialGrime = 0.6f } },
            { "golden_torc", new FindProfile { ItemId = "golden_torc", AgeYears = 2500, Origin = "Tumba de un jefe", Story = "Marca de poder, enterrada con su dueño.", FakeChance = 0.15f, InitialGrime = 0.55f } },
        };

        /// <summary>Perfil del tipo; si no tiene uno propio devuelve uno generico (siempre hay un perfil).</summary>
        public static FindProfile Get(string itemId)
        {
            if (itemId != null && Map.TryGetValue(itemId, out var p)) return p;
            return new FindProfile { ItemId = itemId, AgeYears = 500, Origin = "Origen desconocido", Story = "Nadie sabe su historia.", FakeChance = 0.05f, InitialGrime = 0.7f };
        }
    }

    /// <summary>Estado de una pieza concreta (por uid): cuanta suciedad conserva y si ya fue identificada.</summary>
    public sealed class FindState
    {
        public string Uid, ItemId;
        public float Dirt;
        public bool Identified;
        public bool Authentic;
    }

    public enum CleanResult { Cleaned, AlreadyClean, Damaged }

    /// <summary>
    /// Limpieza, identificacion y valoracion de hallazgos. Estable y sin azar oculto: la autenticidad de una pieza se decide
    /// una sola vez a partir de su uid y de la semilla, y no cambia al guardar/cargar ni al repetir la identificacion.
    /// </summary>
    public sealed class FindWorkbook
    {
        public const float CleanEnoughToIdentify = 0.35f;
        readonly Dictionary<string, FindState> _states = new Dictionary<string, FindState>();
        readonly int _seed;

        public FindWorkbook(int seed) { _seed = seed; }
        public event Action Changed;

        public FindState State(ItemInstance item)
        {
            if (_states.TryGetValue(item.Uid, out var s)) return s;
            var profile = FindProfiles.Get(item.DefinitionId);
            s = new FindState { Uid = item.Uid, ItemId = item.DefinitionId, Dirt = profile.InitialGrime, Identified = false, Authentic = !IsFake(item.Uid, profile) };
            _states[item.Uid] = s;
            return s;
        }

        bool IsFake(string uid, FindProfile p)
        {
            if (p.FakeChance <= 0f) return false;
            unchecked
            {
                int h = _seed * 486187739;
                foreach (char c in uid ?? "") h = h * 31 + c;
                h ^= h >> 15; h *= 0x2c1b3c6d; h ^= h >> 12;
                return ((uint)h % 10000u) / 10000f < p.FakeChance;
            }
        }

        /// <summary>
        /// Limpia durante unas horas de juego (o su equivalente). quality: 0.2 manos, 0.7 cepillo de arqueologo.
        /// Herramientas toscas sobre piezas fragiles pueden dejar la pieza peor: se informa en el resultado.
        /// </summary>
        public CleanResult Clean(ItemInstance item, float quality, float amount)
        {
            var s = State(item);
            if (s.Dirt <= 0.001f) return CleanResult.AlreadyClean;
            s.Dirt = Math.Max(0f, s.Dirt - Math.Max(0f, amount) * (0.5f + quality));
            var result = CleanResult.Cleaned;
            float risk = Math.Max(0f, 0.5f - quality) * Math.Max(0f, amount);
            if (risk > 0.04f)
            {
                item.Condition = Math.Max(0f, item.Condition - risk * 0.25f);
                result = CleanResult.Damaged;
            }
            Changed?.Invoke();
            return result;
        }

        public bool CanIdentify(ItemInstance item) => State(item).Dirt <= CleanEnoughToIdentify;

        /// <summary>Identifica la pieza (requiere estar razonablemente limpia). Devuelve null si todavia no se puede.</summary>
        public FindState Identify(ItemInstance item)
        {
            var s = State(item);
            if (s.Dirt > CleanEnoughToIdentify) return null;
            if (!s.Identified) { s.Identified = true; Changed?.Invoke(); }
            return s;
        }

        /// <summary>Valor de venta: sucia vale menos, sin identificar se descuenta, identificada y antigua suma, falsa casi no vale.</summary>
        public int Value(IItemInfo info, ItemInstance item)
        {
            var s = State(item);
            var p = FindProfiles.Get(item.DefinitionId);
            float v = ItemPricing.ValueOf(info, item.Condition);
            v *= 1f - 0.3f * s.Dirt;
            if (!s.Identified) v *= 0.6f;
            else if (!s.Authentic) v *= 0.05f;
            else v *= 1f + Math.Min(0.6f, p.AgeYears / 1500f);
            return (int)Math.Round(v);
        }

        public (string uid, string itemId, float dirt, bool identified, bool authentic)[] Snapshot()
        {
            var list = new List<(string, string, float, bool, bool)>();
            foreach (var s in _states.Values) list.Add((s.Uid, s.ItemId, s.Dirt, s.Identified, s.Authentic));
            return list.ToArray();
        }

        public void Restore(IEnumerable<(string uid, string itemId, float dirt, bool identified, bool authentic)> states)
        {
            _states.Clear();
            if (states != null)
                foreach (var t in states) _states[t.uid] = new FindState { Uid = t.uid, ItemId = t.itemId, Dirt = Math.Max(0f, Math.Min(1f, t.dirt)), Identified = t.identified, Authentic = t.authentic };
            Changed?.Invoke();
        }
    }
}
