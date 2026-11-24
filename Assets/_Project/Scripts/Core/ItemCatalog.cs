using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>Catalogo de hallazgos. Si no hay assets asignados, se usa el catalogo por defecto del MVP.</summary>
    [CreateAssetMenu(menuName = "Hidden Treasures/Item Catalog", fileName = "ItemCatalog")]
    public sealed class ItemCatalog : ScriptableObject, IItemLookup
    {
        [SerializeField] List<ItemDefinition> items = new List<ItemDefinition>();
        Dictionary<string, ItemDefinition> _byId;

        public IReadOnlyList<ItemDefinition> Items => items;

        public static ItemCatalog CreateFrom(IEnumerable<ItemDefinition> defs)
        {
            var c = CreateInstance<ItemCatalog>();
            c.items = new List<ItemDefinition>(defs);
            return c;
        }

        public bool TryGet(string id, out IItemInfo info)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<string, ItemDefinition>();
                foreach (var i in items) if (i != null && !string.IsNullOrEmpty(i.Id)) _byId[i.Id] = i;
            }
            if (id != null && _byId.TryGetValue(id, out var d)) { info = d; return true; }
            info = null;
            return false;
        }
    }
}
