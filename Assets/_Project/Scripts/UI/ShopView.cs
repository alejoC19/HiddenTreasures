using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HuesoOxido.Collection;
using HuesoOxido.Core;
using HuesoOxido.Economy;
using HuesoOxido.Inventory;
using HuesoOxido.SaveSystem;

namespace HuesoOxido.UI
{
    /// <summary>Mostrador del campamento: vender, restaurar, mejoras, permisos, museo y partida. Solo llama a los servicios.</summary>
    public sealed class ShopView : MonoBehaviour
    {
        enum Page { Sell, Restore, Upgrades, Permits, Museum, Game }

        public sealed class Deps
        {
            public TradingService Trade; public UpgradeService Upgrades; public PermitService Permits;
            public CollectionLog Collection; public Wallet Wallet; public IItemLookup Lookup;
            public IReadOnlyList<IItemInfo> AllItems;
            public InventoryModel Backpack; public InventoryModel Trunk;
            public ISessionActions Session;
            public Func<float> GetSensitivity;
            public Action<float> SetSensitivity;
        }

        Deps _d;
        GameObject _root;
        RectTransform _content;
        Text _money;
        Page _page = Page.Sell;
        bool _open, _dirty, _confirmNew;
        readonly Dictionary<Page, Button> _tabs = new Dictionary<Page, Button>();

        public bool IsOpen => _open;

        public void Initialize(Transform canvas, Deps deps)
        {
            _d = deps;
            var dim = UiFactory.Panel(canvas, "ShopView", new Color(0f, 0f, 0f, 0.55f));
            _root = dim.gameObject;
            UiFactory.Stretch(dim.rectTransform);
            var panel = UiFactory.Panel(dim.transform, "Panel", UiFactory.PanelColor);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 780f));

            var title = UiFactory.Label(panel.transform, "CAMPAMENTO", 30, UiFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Region(title.rectTransform, 0.03f, 0.92f, 0.4f, 0.99f);
            _money = UiFactory.Label(panel.transform, "", 28, UiFactory.TextColor, TextAnchor.MiddleRight, FontStyle.Bold);
            UiFactory.Region(_money.rectTransform, 0.5f, 0.92f, 0.97f, 0.99f);

            var tabRow = UiFactory.Rect("Tabs", panel.transform);
            UiFactory.Region(tabRow, 0.03f, 0.84f, 0.97f, 0.915f);
            var h = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            AddTab(tabRow, Page.Sell, "Vender");
            AddTab(tabRow, Page.Restore, "Restaurar");
            AddTab(tabRow, Page.Upgrades, "Mejoras");
            AddTab(tabRow, Page.Permits, "Permisos");
            AddTab(tabRow, Page.Museum, "Museo");
            AddTab(tabRow, Page.Game, "Partida");

            var list = UiFactory.ScrollList(panel.transform, "List", out _content);
            UiFactory.Region(list, 0.03f, 0.04f, 0.97f, 0.82f);

            var hint = UiFactory.Label(panel.transform, "Esc para cerrar", 16, UiFactory.Muted, TextAnchor.LowerRight);
            UiFactory.Region(hint.rectTransform, 0.6f, 0.0f, 0.97f, 0.04f);

            _d.Wallet.Changed += _ => MarkDirty();
            _d.Upgrades.Changed += MarkDirty;
            _d.Permits.Changed += MarkDirty;
            _d.Collection.Changed += MarkDirty;
            _d.Backpack.Changed += MarkDirty;
            _d.Trunk.Changed += MarkDirty;
            _root.SetActive(false);
        }

        void AddTab(Transform parent, Page page, string label)
        {
            var b = UiFactory.MakeButton(parent, label, () => { _page = page; _confirmNew = false; MarkDirty(); }, 100f, 40f, 20);
            _tabs[page] = b;
        }

        void MarkDirty() { _dirty = true; }

        public void Open() { _open = true; _root.SetActive(true); _dirty = true; }
        public void OpenMuseum() { _page = Page.Museum; Open(); }
        public void Close() { _open = false; _confirmNew = false; _root.SetActive(false); }

        void Update() { if (_open && _dirty) { _dirty = false; Rebuild(); } }

        void Rebuild()
        {
            UiFactory.Clear(_content);
            _money.text = "<color=#" + UiFactory.Hex(UiFactory.Accent) + ">$ " + _d.Wallet.Money + "</color>";
            foreach (var kv in _tabs)
                kv.Value.GetComponent<Image>().color = kv.Key == _page ? new Color(0.5f, 0.36f, 0.14f, 1f) : UiFactory.ButtonColor;

            switch (_page)
            {
                case Page.Sell: BuildSell(); break;
                case Page.Restore: BuildRestore(); break;
                case Page.Upgrades: BuildUpgrades(); break;
                case Page.Permits: BuildPermits(); break;
                case Page.Museum: BuildMuseum(); break;
                default: BuildGame(); break;
            }
        }

        string Name(ItemInstance it)
        {
            if (!_d.Lookup.TryGet(it.DefinitionId, out var info)) return it.DefinitionId;
            string dup = _d.Collection.IsDisplayed(info.Id) ? "  <color=#" + UiFactory.Hex(UiFactory.Muted) + ">(duplicado)</color>" : "";
            return "<color=#" + UiFactory.Hex(RarityStyle.ColorOf(info.Rarity)) + ">" + info.DisplayName + "</color>  <color=#"
                   + UiFactory.Hex(UiFactory.Muted) + ">" + Mathf.RoundToInt(it.Condition * 100f) + "%</color>" + dup;
        }

        void Row(string text, string button, Action onClick, bool enabled = true)
        {
            var row = UiFactory.Row(_content, 50f);
            UiFactory.RowText(row, text);
            if (button != null)
            {
                var b = UiFactory.MakeButton(row, button, onClick, 150f, 38f, 18);
                b.interactable = enabled;
            }
        }

        void Header(string text) => UiFactory.Label(_content, "<b>" + text + "</b>", 22, UiFactory.Accent);

        IEnumerable<(string source, InventoryModel inv)> Sources()
        {
            yield return ("Mochila", _d.Backpack);
            yield return ("Baúl", _d.Trunk);
        }

        void BuildSell()
        {
            // Resumen: todo lo vendible (mochila + baul) y cuanto vale en total.
            int grandTotal = 0, grandCount = 0;
            foreach (var (_, inv0) in Sources())
                foreach (var it0 in inv0.Items) { int pr = _d.Trade.PriceOf(it0); if (pr > 0) { grandTotal += pr; grandCount++; } }
            UiFactory.Label(_content, "Tus hallazgos valen <b><color=#" + UiFactory.Hex(UiFactory.Accent) + ">$" + grandTotal + "</color></b>  (" + grandCount + " objetos)  ·  Dinero: $" + _d.Wallet.Money, 20, UiFactory.TextColor);

            bool any = false;
            foreach (var (source, inv) in Sources())
            {
                if (inv.Items.Count == 0) continue;
                any = true;
                int total = 0;
                foreach (var it in inv.Items) total += _d.Trade.PriceOf(it);
                Header(source + "  ·  " + inv.TotalWeight.ToString("0.0") + " kg");
                var inv2 = inv;
                Row("Vender todo de " + source.ToLower() + " (" + inv.Items.Count + " objetos)", "Todo  $" + total, () => _d.Trade.SellAll(inv2));
                var sorted = new List<ItemInstance>(inv.Items);
                sorted.Sort((x, y) => _d.Trade.PriceOf(y).CompareTo(_d.Trade.PriceOf(x)));   // lo mas valioso primero
                foreach (var it in sorted)
                {
                    var item = it;
                    Row(Name(item), "Vender  $" + _d.Trade.PriceOf(item), () => _d.Trade.Sell(inv2, item.Uid));
                }
            }
            if (!any) UiFactory.Label(_content, "No tenés hallazgos para vender. Explorá el bosque con el detector.", 20, UiFactory.Muted);
        }

        void BuildRestore()
        {
            bool any = false;
            foreach (var (source, inv) in Sources())
            {
                foreach (var it in new List<ItemInstance>(inv.Items))
                {
                    int cost = _d.Trade.RestoreCost(it);
                    if (cost < 0) continue;
                    any = true;
                    var item = it; var inv2 = inv;
                    Row(Name(item) + "  <color=#" + UiFactory.Hex(UiFactory.Muted) + ">(" + source.ToLower() + ")</color>", "Restaurar  $" + cost,
                        () => _d.Trade.Restore(inv2, item.Uid), _d.Wallet.Money >= cost);
                }
            }
            if (!any) UiFactory.Label(_content, "Nada para restaurar. Restaurar sube el valor de venta de una pieza hasta el máximo.", 20, UiFactory.Muted);
        }

        void BuildUpgrades()
        {
            foreach (var def in _d.Upgrades.Definitions)
            {
                string id = def.Id;
                int lvl = _d.Upgrades.Level(id);
                int cost = _d.Upgrades.NextCost(id);
                string text = "<b>" + def.Name + "</b>  Nivel " + lvl + "/" + def.MaxLevel + "\n<color=#" + UiFactory.Hex(UiFactory.Muted) + ">" + def.Description + "</color>";
                var row = UiFactory.Row(_content, 66f);
                UiFactory.RowText(row, text, 20);
                var b = UiFactory.MakeButton(row, cost < 0 ? "MÁXIMO" : "Comprar  $" + cost, () => _d.Upgrades.TryPurchase(id, _d.Wallet), 170f, 42f, 18);
                b.interactable = cost >= 0 && _d.Wallet.Money >= cost;
            }
        }

        void BuildPermits()
        {
            UiFactory.Label(_content, "Sin permiso no se puede excavar. Cada terreno tiene el suyo.", 19, UiFactory.Muted);
            foreach (var p in _d.Permits.Definitions)
            {
                string id = p.Id;
                bool owned = _d.Permits.Owns(id);
                var row = UiFactory.Row(_content, 66f);
                UiFactory.RowText(row, "<b>" + p.Name + "</b>\n<color=#" + UiFactory.Hex(UiFactory.Muted) + ">" + p.Description + "</color>", 20);
                var b = UiFactory.MakeButton(row, owned ? "ACTIVO" : "Comprar  $" + p.Cost, () => _d.Permits.TryBuy(id, _d.Wallet), 170f, 42f, 18);
                b.interactable = !owned && _d.Wallet.Money >= p.Cost;
            }
        }

        void BuildMuseum()
        {
            int total = _d.AllItems.Count;
            UiFactory.Label(_content, "Descubiertas: " + _d.Collection.DiscoveredCount + " / " + total + "     Exhibidas: " + _d.Collection.DisplayedCount + " / " + total, 22, UiFactory.TextColor);

            Header("Exhibir piezas");
            bool any = false;
            foreach (var (source, inv) in Sources())
            {
                foreach (var it in new List<ItemInstance>(inv.Items))
                {
                    if (_d.Collection.IsDisplayed(it.DefinitionId)) continue;
                    any = true;
                    var item = it; var inv2 = inv;
                    Row(Name(item) + "  <color=#" + UiFactory.Hex(UiFactory.Muted) + ">(" + source.ToLower() + ")</color>", "Exhibir", () => _d.Trade.Display(inv2, item.Uid));
                }
            }
            if (!any) UiFactory.Label(_content, "No tenés piezas nuevas para exhibir. Solo se exhibe una por tipo; las demás son duplicados para vender.", 19, UiFactory.Muted);

            Header("Catálogo");
            foreach (var info in _d.AllItems)
            {
                int times = _d.Collection.TimesFound(info.Id);
                string text;
                if (times == 0) text = "<color=#" + UiFactory.Hex(UiFactory.Muted) + ">???  (sin descubrir)</color>";
                else text = "<color=#" + UiFactory.Hex(RarityStyle.ColorOf(info.Rarity)) + ">" + info.DisplayName + "</color>  x" + times
                            + (_d.Collection.IsDisplayed(info.Id) ? "   <color=#" + UiFactory.Hex(UiFactory.Good) + ">EXHIBIDA</color>" : "")
                            + (info.IsFiction ? "   <color=#" + UiFactory.Hex(UiFactory.Accent) + ">ficción</color>" : "");
                Row(text, null, null);
            }
        }

        void BuildGame()
        {
            UiFactory.Label(_content, "El juego guarda al dormir en la carpa. También podés guardar con F5.", 20, UiFactory.Muted);
            var sens = UiFactory.Row(_content, 50f);
            UiFactory.RowText(sens, "Sensibilidad de cámara: <b>" + _d.GetSensitivity().ToString("0.0") + "</b>");
            UiFactory.MakeButton(sens, "−", () => { _d.SetSensitivity(_d.GetSensitivity() - 0.1f); MarkDirty(); }, 60f, 38f, 24);
            UiFactory.MakeButton(sens, "+", () => { _d.SetSensitivity(_d.GetSensitivity() + 0.1f); MarkDirty(); }, 60f, 38f, 24);
            Row("Guardar partida", "Guardar", () => { _d.Session.SaveGame(); MarkDirty(); });
            Row("Cargar la última partida guardada", "Cargar", () => _d.Session.LoadGame(), _d.Session.HasSave);
            Row(_confirmNew ? "<color=#" + UiFactory.Hex(UiFactory.Bad) + ">¿Seguro? Se borra todo el progreso.</color>" : "Empezar de cero",
                _confirmNew ? "Confirmar" : "Nueva partida",
                () => { if (_confirmNew) { _confirmNew = false; _d.Session.NewGame(); } else { _confirmNew = true; MarkDirty(); } });
        }
    }
}
