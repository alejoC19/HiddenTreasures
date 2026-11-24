using System;
using System.Collections.Generic;

namespace HuesoOxido.Economy
{
    public enum UpgradeKind { BackpackCapacity, TrunkCapacity, DetectorRange, DigSpeed, FuelTank, EnergyMax }

    public sealed class UpgradeDefinition
    {
        public string Id; public string Name; public string Description;
        public UpgradeKind Kind;
        /// <summary>Costo para pasar al nivel i+1.</summary>
        public int[] Costs;
        /// <summary>Bonus aditivo por nivel (kg, metros, %, litros, puntos segun el tipo).</summary>
        public float BonusPerLevel;
        public int MaxLevel => Costs.Length;
    }

    public static class DefaultUpgrades
    {
        public static List<UpgradeDefinition> Create() => new List<UpgradeDefinition>
        {
            new UpgradeDefinition { Id = "backpack", Name = "Mochila reforzada", Kind = UpgradeKind.BackpackCapacity,
                Description = "+4 kg de capacidad por nivel.", Costs = new[] { 120, 300, 700 }, BonusPerLevel = 4f },
            new UpgradeDefinition { Id = "trunk", Name = "Baúl ampliado", Kind = UpgradeKind.TrunkCapacity,
                Description = "+15 kg de capacidad por nivel.", Costs = new[] { 200, 450, 900 }, BonusPerLevel = 15f },
            new UpgradeDefinition { Id = "detector", Name = "Bobina del detector", Kind = UpgradeKind.DetectorRange,
                Description = "+1 m de alcance por nivel.", Costs = new[] { 150, 380, 800 }, BonusPerLevel = 1f },
            new UpgradeDefinition { Id = "shovel", Name = "Pala de acero", Kind = UpgradeKind.DigSpeed,
                Description = "Excavás 15% más rápido por nivel.", Costs = new[] { 100, 260, 600 }, BonusPerLevel = 0.15f },
            new UpgradeDefinition { Id = "tank", Name = "Tanque auxiliar", Kind = UpgradeKind.FuelTank,
                Description = "+20 L de combustible por nivel.", Costs = new[] { 180, 420 }, BonusPerLevel = 20f },
            new UpgradeDefinition { Id = "stamina", Name = "Entrenamiento de campo", Kind = UpgradeKind.EnergyMax,
                Description = "+15 de energía máxima por nivel.", Costs = new[] { 160, 400, 850 }, BonusPerLevel = 15f },
        };
    }

    public sealed class UpgradeService
    {
        readonly Dictionary<string, UpgradeDefinition> _defs = new Dictionary<string, UpgradeDefinition>();
        readonly Dictionary<string, int> _levels = new Dictionary<string, int>();
        readonly List<UpgradeDefinition> _ordered = new List<UpgradeDefinition>();

        public event Action Changed;

        public UpgradeService(IEnumerable<UpgradeDefinition> defs)
        {
            foreach (var d in defs) { _defs[d.Id] = d; _ordered.Add(d); }
        }

        public IReadOnlyList<UpgradeDefinition> Definitions => _ordered;

        public int Level(string id) => _levels.TryGetValue(id, out var l) ? l : 0;

        public bool IsMaxed(string id) => _defs.TryGetValue(id, out var d) && Level(id) >= d.MaxLevel;

        /// <summary>Costo del siguiente nivel, o -1 si ya esta al maximo.</summary>
        public int NextCost(string id)
        {
            if (!_defs.TryGetValue(id, out var d) || IsMaxed(id)) return -1;
            return d.Costs[Level(id)];
        }

        public bool TryPurchase(string id, Wallet wallet)
        {
            int cost = NextCost(id);
            if (cost < 0 || !wallet.TrySpend(cost)) return false;
            _levels[id] = Level(id) + 1;
            Changed?.Invoke();
            return true;
        }

        public float Bonus(UpgradeKind kind)
        {
            float total = 0f;
            foreach (var d in _ordered) if (d.Kind == kind) total += d.BonusPerLevel * Level(d.Id);
            return total;
        }

        public Dictionary<string, int> Snapshot() => new Dictionary<string, int>(_levels);

        public void Restore(IEnumerable<KeyValuePair<string, int>> levels)
        {
            _levels.Clear();
            if (levels != null)
                foreach (var kv in levels)
                    if (_defs.TryGetValue(kv.Key, out var d)) _levels[kv.Key] = Math.Min(kv.Value, d.MaxLevel);
            Changed?.Invoke();
        }
    }
}
