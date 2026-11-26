using System;
using System.Collections.Generic;
using HuesoOxido.Core;

namespace HuesoOxido.Economy
{
    public sealed class PermitDefinition
    {
        public string Id; public string Name; public string Description; public string ZoneId; public int Cost;
    }

    /// <summary>Permisos de excavacion por terreno. Para el MVP existe uno solo; el diseño admite mas.</summary>
    public sealed class PermitService : IDigPermission
    {
        readonly List<PermitDefinition> _defs;
        readonly HashSet<string> _owned = new HashSet<string>();
        public event Action Changed;

        public PermitService(IEnumerable<PermitDefinition> defs) { _defs = new List<PermitDefinition>(defs); }

        public static PermitDefinition ForestRuins => new PermitDefinition
        {
            Id = "permit_forest_ruins", ZoneId = "forest_ruins", Name = "Permiso: Bosque de las Ruinas", Cost = 40,
            Description = "Autoriza excavar en el bosque y las ruinas durante esta expedición."
        };

        /// <summary>Un permiso por ecosistema, en el orden del registro de biomas.</summary>
        public static List<PermitDefinition> ForBiomes()
        {
            var list = new List<PermitDefinition>();
            foreach (var b in Biomes.All)
                list.Add(b.IsDefault ? ForestRuins : new PermitDefinition { Id = b.PermitId, ZoneId = b.ZoneId, Name = "Permiso: " + b.Name, Cost = b.PermitCost, Description = b.Description });
            return list;
        }

        public IReadOnlyList<PermitDefinition> Definitions => _defs;
        public bool Owns(string permitId) => _owned.Contains(permitId);

        public bool CanDig(string zoneId)
        {
            foreach (var d in _defs) if (d.ZoneId == zoneId && _owned.Contains(d.Id)) return true;
            return false;
        }

        public bool TryBuy(string permitId, Wallet wallet)
        {
            var d = _defs.Find(x => x.Id == permitId);
            if (d == null || _owned.Contains(permitId) || !wallet.TrySpend(d.Cost)) return false;
            _owned.Add(permitId);
            Changed?.Invoke();
            return true;
        }

        public List<string> Snapshot() => new List<string>(_owned);

        public void Restore(IEnumerable<string> owned)
        {
            _owned.Clear();
            if (owned != null) foreach (var o in owned) _owned.Add(o);
            Changed?.Invoke();
        }
    }
}
