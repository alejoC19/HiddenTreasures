using System.Text;
using UnityEngine;
using UnityEngine.UI;
using HuesoOxido.Collection;
using HuesoOxido.Core;
using HuesoOxido.Economy;
using HuesoOxido.Inventory;

namespace HuesoOxido.UI
{
    /// <summary>Mochila, baul cercano y detalle del objeto seleccionado.</summary>
    public sealed class InventoryView : MonoBehaviour
    {
        InventoryModel _backpack;
        IInventoryActions _actions;
        IItemLookup _lookup;
        CollectionLog _collection;
        TradingService _trade;
        GameObject _root;
        RectTransform _packContent, _trunkContent;
        Text _packTitle, _trunkTitle, _detail;
        string _selected;
        bool _dirty, _open;

        public bool IsOpen => _open;

        public void Initialize(Transform canvas, InventoryModel backpack, IInventoryActions actions, IItemLookup lookup,
                               CollectionLog collection, TradingService trade)
        {
            _backpack = backpack; _actions = actions; _lookup = lookup; _collection = collection; _trade = trade;

            var dim = UiFactory.Panel(canvas, "InventoryView", new Color(0f, 0f, 0f, 0.55f));
            _root = dim.gameObject;
            UiFactory.Stretch(dim.rectTransform);
            var panel = UiFactory.Panel(dim.transform, "Panel", UiFactory.PanelColor);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 760f));

            var title = UiFactory.Label(panel.transform, "INVENTARIO", 30, UiFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Region(title.rectTransform, 0.03f, 0.92f, 0.6f, 0.99f);
            var hint = UiFactory.Label(panel.transform, "Tab / Esc para cerrar", 16, UiFactory.Muted, TextAnchor.MiddleRight);
            UiFactory.Region(hint.rectTransform, 0.6f, 0.92f, 0.97f, 0.99f);

            _packTitle = UiFactory.Label(panel.transform, "", 22, UiFactory.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Region(_packTitle.rectTransform, 0.03f, 0.85f, 0.35f, 0.92f);
            var packList = UiFactory.ScrollList(panel.transform, "PackList", out _packContent);
            UiFactory.Region(packList, 0.03f, 0.04f, 0.35f, 0.85f);

            _trunkTitle = UiFactory.Label(panel.transform, "", 22, UiFactory.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Region(_trunkTitle.rectTransform, 0.37f, 0.85f, 0.67f, 0.92f);
            var trunkList = UiFactory.ScrollList(panel.transform, "TrunkList", out _trunkContent);
            UiFactory.Region(trunkList, 0.37f, 0.04f, 0.67f, 0.85f);

            var detailPanel = UiFactory.Panel(panel.transform, "Detail", new Color(0f, 0f, 0f, 0.28f));
            UiFactory.Region(detailPanel.rectTransform, 0.69f, 0.04f, 0.97f, 0.92f);
            _detail = UiFactory.Label(detailPanel.transform, "", 20, UiFactory.TextColor);
            UiFactory.Stretch(_detail.rectTransform);
            _detail.rectTransform.offsetMin = new Vector2(14f, 10f);
            _detail.rectTransform.offsetMax = new Vector2(-14f, -10f);

            backpack.Changed += MarkDirty;
            actions.Trunk.Changed += MarkDirty;
            _collection.Changed += MarkDirty;
            _root.SetActive(false);
        }

        void OnDestroy()
        {
            if (_backpack != null) _backpack.Changed -= MarkDirty;
            if (_actions != null && _actions.Trunk != null) _actions.Trunk.Changed -= MarkDirty;
            if (_collection != null) _collection.Changed -= MarkDirty;
        }

        void MarkDirty() { _dirty = true; }

        public void Open() { _open = true; _root.SetActive(true); _dirty = true; }
        public void Close() { _open = false; _root.SetActive(false); }

        void Update() { if (_open && _dirty) { _dirty = false; Rebuild(); } }

        void Rebuild()
        {
            UiFactory.Clear(_packContent);
            UiFactory.Clear(_trunkContent);
            _packTitle.text = "MOCHILA  " + _backpack.TotalWeight.ToString("0.0") + " / " + _backpack.CapacityKg.ToString("0.0") + " kg";
            bool near = _actions.TrunkReachable;
            var trunk = _actions.Trunk;
            _trunkTitle.text = "BAÚL  " + trunk.TotalWeight.ToString("0.0") + " / " + trunk.CapacityKg.ToString("0.0") + " kg"
                               + (near ? "" : "  <color=#" + UiFactory.Hex(UiFactory.Muted) + ">(lejos)</color>");

            if (_backpack.Items.Count == 0) UiFactory.Label(_packContent, "La mochila está vacía.", 18, UiFactory.Muted);
            foreach (var it in _backpack.Items)
            {
                var item = it;
                var row = UiFactory.Row(_packContent);
                var label = UiFactory.RowText(row, Describe(item));
                var sel = row.gameObject.AddComponent<Button>();
                sel.targetGraphic = row.GetComponent<Image>();
                sel.onClick.AddListener(() => Select(item.Uid));
                if (near) UiFactory.MakeButton(row, "→ Baúl", () => _actions.MoveToTrunk(item.Uid), 96f, 34f, 17);
                UiFactory.MakeButton(row, "Tirar", () => _actions.Drop(item.Uid), 76f, 34f, 17);
                if (item.Uid == _selected) label.fontStyle = FontStyle.Bold;
            }

            if (near && trunk.Items.Count == 0) UiFactory.Label(_trunkContent, "El baúl está vacío.", 18, UiFactory.Muted);
            if (!near) UiFactory.Label(_trunkContent, "Acercate al baúl del vehículo para guardar o tomar objetos.", 18, UiFactory.Muted);
            else
            {
                if (_backpack.Items.Count > 0)
                {
                    var allRow = UiFactory.Row(_trunkContent, 44f);
                    UiFactory.RowText(allRow, "Guardar todo lo que entre");
                    UiFactory.MakeButton(allRow, "Guardar", () => _actions.DepositAll(), 100f, 34f, 17);
                }
                foreach (var it in trunk.Items)
                {
                    var item = it;
                    var row = UiFactory.Row(_trunkContent);
                    UiFactory.RowText(row, Describe(item));
                    var sel = row.gameObject.AddComponent<Button>();
                    sel.targetGraphic = row.GetComponent<Image>();
                    sel.onClick.AddListener(() => Select(item.Uid));
                    UiFactory.MakeButton(row, "← Tomar", () => _actions.TakeFromTrunk(item.Uid), 96f, 34f, 17);
                }
            }
            RefreshDetail();
        }

        void Select(string uid) { _selected = uid; _dirty = true; }

        string Describe(ItemInstance item)
        {
            if (!_lookup.TryGet(item.DefinitionId, out var info)) return item.DefinitionId;
            return "<color=#" + UiFactory.Hex(RarityStyle.ColorOf(info.Rarity)) + ">" + info.DisplayName + "</color>  <color=#"
                   + UiFactory.Hex(UiFactory.Muted) + ">" + info.WeightKg.ToString("0.0#") + " kg</color>";
        }

        void RefreshDetail()
        {
            var item = _backpack.Find(_selected) ?? _actions.Trunk.Find(_selected);
            if (item == null || !_lookup.TryGet(item.DefinitionId, out var info))
            {
                _detail.text = "<color=#" + UiFactory.Hex(UiFactory.Muted) + ">Seleccioná un objeto para ver su detalle.\n\nTu decisión: ¿volvés a asegurar lo que encontraste o arriesgás por una pieza más?</color>";
                return;
            }
            var sb = new StringBuilder();
            sb.AppendLine("<b><size=26><color=#" + UiFactory.Hex(RarityStyle.ColorOf(info.Rarity)) + ">" + info.DisplayName + "</color></size></b>");
            sb.AppendLine(RarityStyle.NameOf(info.Rarity) + " · " + RarityStyle.NameOf(info.Category));
            sb.AppendLine();
            sb.AppendLine(info.Description);
            sb.AppendLine();
            sb.AppendLine("Peso: " + info.WeightKg.ToString("0.0#") + " kg");
            sb.AppendLine("Estado: " + Mathf.RoundToInt(item.Condition * 100f) + "%");
            sb.AppendLine("Valor estimado: $" + _trade.PriceOf(item));
            int times = _collection.TimesFound(info.Id);
            if (times > 1) sb.AppendLine("Encontrados: " + times + (_collection.IsDisplayed(info.Id) ? "  (duplicado)" : ""));
            sb.AppendLine();
            sb.AppendLine("<color=#" + UiFactory.Hex(info.IsFiction ? UiFactory.Accent : UiFactory.Muted) + ">"
                          + (info.IsFiction ? "FICCIÓN: " : "Fuente: ") + info.SourceNote + "</color>");
            _detail.text = sb.ToString();
        }
    }
}
