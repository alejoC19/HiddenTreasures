using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HuesoOxido.Core;

namespace HuesoOxido.UI
{
    /// <summary>
    /// Libreta de campo (tecla J): todos los sitios con tesoro del mapa. Lo no descubierto aparece como "???"; al desenterrarlo
    /// se muestra nombre, rareza y una descripcion breve. Solo lee datos (FieldJournal arma las lineas).
    /// </summary>
    public sealed class JournalView : MonoBehaviour
    {
        Func<IEnumerable<JournalSite>> _sites;
        IItemLookup _lookup;
        QuestManager _quests;
        GameObject _root;
        RectTransform _content;
        Text _summary, _questLine;
        bool _open;

        public bool IsOpen => _open;

        public void Initialize(Transform canvas, Func<IEnumerable<JournalSite>> sites, IItemLookup lookup, QuestManager quests)
        {
            _sites = sites; _lookup = lookup; _quests = quests;
            var dim = UiFactory.Panel(canvas, "JournalView", new Color(0f, 0f, 0f, 0.6f));
            _root = dim.gameObject;
            UiFactory.Stretch(dim.rectTransform);
            var panel = UiFactory.Panel(dim.transform, "Panel", new Color(0.10f, 0.085f, 0.06f, 0.95f)); // papel oscuro de libreta
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 760f));

            var title = UiFactory.Label(panel.transform, "LIBRETA DE CAMPO", 30, UiFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Region(title.rectTransform, 0.03f, 0.92f, 0.6f, 0.99f);
            var hint = UiFactory.Label(panel.transform, "J / Esc para cerrar", 16, UiFactory.Muted, TextAnchor.MiddleRight);
            UiFactory.Region(hint.rectTransform, 0.6f, 0.92f, 0.97f, 0.99f);

            _summary = UiFactory.Label(panel.transform, "", 22, UiFactory.TextColor, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Region(_summary.rectTransform, 0.03f, 0.86f, 0.97f, 0.92f);
            _questLine = UiFactory.Label(panel.transform, "", 18, UiFactory.Muted, TextAnchor.MiddleLeft);
            UiFactory.Region(_questLine.rectTransform, 0.03f, 0.815f, 0.97f, 0.865f);

            var list = UiFactory.ScrollList(panel.transform, "Entries", out _content);
            UiFactory.Region(list, 0.03f, 0.04f, 0.97f, 0.81f);
            _root.SetActive(false);
        }

        public void Open() { _open = true; _root.SetActive(true); Rebuild(); }
        public void Close() { _open = false; if (_root != null) _root.SetActive(false); }

        void Rebuild()
        {
            var sum = FieldJournal.Build(_sites(), _lookup);
            _summary.text = "Descubiertos " + sum.TotalFound + " / " + sum.Total + "   ·   Reliquias " + sum.RelicsFound + " / " + sum.Relics.Count;
            _questLine.text = _quests != null && _quests.HasQuest ? "Objetivo: " + _quests.TitleText + "  (" + _quests.ProgressText + ")" : "";

            UiFactory.Clear(_content);
            Header("RELIQUIAS PREHISPÁNICAS", sum.RelicsFound, sum.Relics.Count);
            foreach (var e in sum.Relics) Entry(e);
            Header("HALLAZGOS DEL BOSQUE", sum.FindsFound, sum.Finds.Count);
            foreach (var e in sum.Finds) Entry(e);
        }

        void Header(string text, int found, int total)
        {
            var row = UiFactory.Row(_content, 38f);
            row.GetComponent<Image>().color = new Color(0.95f, 0.66f, 0.26f, 0.14f);
            UiFactory.RowText(row, "<b>" + text + "</b>   " + found + " / " + total, 20).color = UiFactory.Accent;
        }

        void Entry(JournalEntry e)
        {
            var row = UiFactory.Row(_content, e.Discovered ? 66f : 40f);
            var le = row.GetComponent<LayoutElement>();
            var v = row.gameObject.AddComponent<VerticalLayoutGroup>();
            Destroy(row.GetComponent<HorizontalLayoutGroup>());
            v.padding = new RectOffset(12, 8, 4, 4); v.spacing = 2f;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;

            string num = "<color=#" + UiFactory.Hex(UiFactory.Muted) + ">" + e.Title + "</color>   ";
            if (!e.Discovered)
            {
                var t = UiFactory.Label(row, num + "<color=#" + UiFactory.Hex(UiFactory.Muted) + ">" + FieldJournal.Hidden + "</color>", 20, UiFactory.TextColor, TextAnchor.MiddleLeft);
                t.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
                return;
            }
            string rar = RarityStyle.NameOf(e.Rarity);
            var head = UiFactory.Label(row, num + "<b><color=#" + UiFactory.Hex(RarityStyle.ColorOf(e.Rarity)) + ">" + e.Name + "</color></b>   <size=16><color=#"
                                       + UiFactory.Hex(RarityStyle.ColorOf(e.Rarity)) + ">" + rar + "</color></size>", 21, UiFactory.TextColor, TextAnchor.MiddleLeft);
            head.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            var desc = UiFactory.Label(row, e.Description, 16, UiFactory.Muted, TextAnchor.UpperLeft);
            desc.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        }
    }
}
