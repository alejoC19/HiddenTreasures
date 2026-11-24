using System;
using HuesoOxido.Collection;
using HuesoOxido.Core;
using HuesoOxido.Inventory;

namespace HuesoOxido.Economy
{
    /// <summary>Venta, restauracion y exhibicion de hallazgos en el campamento.</summary>
    public sealed class TradingService
    {
        readonly IItemLookup _lookup;
        readonly Wallet _wallet;
        readonly CollectionLog _collection;

        public TradingService(IItemLookup lookup, Wallet wallet, CollectionLog collection)
        {
            _lookup = lookup; _wallet = wallet; _collection = collection;
        }

        public int PriceOf(ItemInstance item)
            => _lookup.TryGet(item.DefinitionId, out var info) ? ItemPricing.ValueOf(info, item.Condition) : 0;

        public int RestoreCost(ItemInstance item)
        {
            if (!_lookup.TryGet(item.DefinitionId, out var info) || item.Condition >= 0.999f) return -1;
            return Math.Max(5, (int)Math.Round(info.BaseValue * 0.25f * (1f - item.Condition)));
        }

        /// <summary>Vende un objeto de 'from'. Devuelve lo cobrado, o -1 si no existe.</summary>
        public int Sell(InventoryModel from, string uid)
        {
            if (!from.Remove(uid, out var item)) return -1;
            int price = PriceOf(item);
            _wallet.Add(price);
            return price;
        }

        public int SellAll(InventoryModel from)
        {
            int total = 0;
            foreach (var it in new System.Collections.Generic.List<ItemInstance>(from.Items))
            {
                int p = Sell(from, it.Uid);
                if (p > 0) total += p;
            }
            return total;
        }

        public bool Restore(InventoryModel inv, string uid)
        {
            var item = inv.Find(uid);
            if (item == null) return false;
            int cost = RestoreCost(item);
            if (cost < 0 || !_wallet.TrySpend(cost)) return false;
            item.Condition = 1f;
            return true;
        }

        /// <summary>Entrega la pieza al museo si todavia no hay una exhibida de ese tipo.</summary>
        public bool Display(InventoryModel from, string uid)
        {
            var item = from.Find(uid);
            if (item == null || _collection.IsDisplayed(item.DefinitionId)) return false;
            if (!from.Remove(uid, out _)) return false;
            return _collection.TryDisplay(item.DefinitionId);
        }
    }
}
