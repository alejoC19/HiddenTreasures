using System;
using System.Collections.Generic;
using HuesoOxido.Core;

namespace HuesoOxido.Inventory
{
    /// <summary>Contenedor con limite de peso (mochila o baul). Logica pura, sin MonoBehaviour.</summary>
    public sealed class InventoryModel
    {
        readonly List<ItemInstance> _items = new List<ItemInstance>();
        readonly IItemLookup _lookup;

        public InventoryModel(string label, float capacityKg, IItemLookup lookup)
        {
            Label = label;
            CapacityKg = capacityKg;
            _lookup = lookup ?? throw new ArgumentNullException(nameof(lookup));
        }

        public string Label { get; }
        public float CapacityKg { get; private set; }
        public IReadOnlyList<ItemInstance> Items => _items;
        public event Action Changed;

        public float TotalWeight
        {
            get
            {
                float w = 0f;
                for (int i = 0; i < _items.Count; i++) w += WeightOf(_items[i]);
                return w;
            }
        }

        public float FreeWeight => Math.Max(0f, CapacityKg - TotalWeight);
        public float LoadFraction => CapacityKg <= 0f ? 1f : Math.Min(1f, TotalWeight / CapacityKg);

        public float WeightOf(ItemInstance item)
            => _lookup.TryGet(item.DefinitionId, out var info) ? info.WeightKg : 0f;

        public bool CanAdd(ItemInstance item) => item != null && TotalWeight + WeightOf(item) <= CapacityKg + 1e-4f;

        public bool TryAdd(ItemInstance item)
        {
            if (!CanAdd(item) || Contains(item.Uid)) return false;
            _items.Add(item);
            Changed?.Invoke();
            return true;
        }

        public bool Contains(string uid) => Find(uid) != null;

        public ItemInstance Find(string uid)
        {
            for (int i = 0; i < _items.Count; i++) if (_items[i].Uid == uid) return _items[i];
            return null;
        }

        public bool Remove(string uid, out ItemInstance removed)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Uid != uid) continue;
                removed = _items[i];
                _items.RemoveAt(i);
                Changed?.Invoke();
                return true;
            }
            removed = null;
            return false;
        }

        /// <summary>Cambia el limite. Puede quedar por debajo del peso actual (no se podra agregar mas).</summary>
        public void SetCapacity(float kg)
        {
            if (Math.Abs(kg - CapacityKg) < 1e-5f) return;
            CapacityKg = Math.Max(0f, kg);
            Changed?.Invoke();
        }

        /// <summary>Mueve todo lo que entre en 'target'. Devuelve cuantos objetos se movieron.</summary>
        public int TransferAllTo(InventoryModel target)
        {
            int moved = 0;
            var snapshot = new List<ItemInstance>(_items);
            foreach (var it in snapshot)
            {
                if (!target.CanAdd(it)) continue;
                _items.Remove(it);
                target._items.Add(it);
                moved++;
            }
            if (moved > 0) { Changed?.Invoke(); target.Changed?.Invoke(); }
            return moved;
        }

        public bool TransferTo(InventoryModel target, string uid)
        {
            var it = Find(uid);
            if (it == null || !target.CanAdd(it)) return false;
            _items.Remove(it);
            target._items.Add(it);
            Changed?.Invoke();
            target.Changed?.Invoke();
            return true;
        }

        /// <summary>Quita 'fraction' (0..1) de los objetos al azar. Devuelve los removidos.</summary>
        public List<ItemInstance> RemoveRandomFraction(float fraction, Random rng)
        {
            int n = (int)Math.Ceiling(_items.Count * Math.Max(0f, Math.Min(1f, fraction)));
            var removed = new List<ItemInstance>();
            for (int i = 0; i < n && _items.Count > 0; i++)
            {
                int idx = rng.Next(_items.Count);
                removed.Add(_items[idx]);
                _items.RemoveAt(idx);
            }
            if (removed.Count > 0) Changed?.Invoke();
            return removed;
        }

        public void ReplaceAll(IEnumerable<ItemInstance> items)
        {
            _items.Clear();
            if (items != null) _items.AddRange(items);
            Changed?.Invoke();
        }
    }
}
