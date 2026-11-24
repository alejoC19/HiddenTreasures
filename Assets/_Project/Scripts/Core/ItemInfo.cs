using System;

namespace HuesoOxido.Core
{
    /// <summary>Datos de solo lectura de un tipo de hallazgo. Lo implementa ItemDefinition (ScriptableObject).</summary>
    public interface IItemInfo
    {
        string Id { get; }
        string DisplayName { get; }
        string Description { get; }
        /// <summary>Nota que distingue lo documentado de lo ficticio.</summary>
        string SourceNote { get; }
        bool IsFiction { get; }
        Rarity Rarity { get; }
        FindCategory Category { get; }
        float WeightKg { get; }
        int BaseValue { get; }
    }

    public interface IItemLookup
    {
        bool TryGet(string id, out IItemInfo info);
    }

    /// <summary>Instancia concreta de un hallazgo (lo que va en mochila, baul o coleccion).</summary>
    [Serializable]
    public sealed class ItemInstance
    {
        public string Uid;
        public string DefinitionId;
        /// <summary>0 = muy deteriorado, 1 = restaurado.</summary>
        public float Condition;

        public ItemInstance(string uid, string definitionId, float condition)
        {
            Uid = uid;
            DefinitionId = definitionId;
            Condition = condition < 0f ? 0f : (condition > 1f ? 1f : condition);
        }

        public static ItemInstance Create(string definitionId, Random rng, float condition = 0.55f)
        {
            var bytes = new byte[8];
            rng.NextBytes(bytes);
            return new ItemInstance(BitConverter.ToString(bytes).Replace("-", ""), definitionId, condition);
        }
    }

    public static class ItemPricing
    {
        /// <summary>Valor de venta: el estado de conservacion mueve entre 50% y 100% del valor base.</summary>
        public static int ValueOf(IItemInfo info, float condition)
        {
            float c = condition < 0f ? 0f : (condition > 1f ? 1f : condition);
            return (int)Math.Round(info.BaseValue * (0.5f + 0.5f * c));
        }
    }
}
