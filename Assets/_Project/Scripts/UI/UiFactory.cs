using System;
using UnityEngine;
using UnityEngine.UI;

namespace HuesoOxido.UI
{
    /// <summary>Constructores de uGUI por codigo (sin prefabs). Solo presentacion.</summary>
    public static class UiFactory
    {
        public static readonly Color PanelColor = new Color(0.06f, 0.055f, 0.045f, 0.86f);
        public static readonly Color DarkPanel = new Color(0.03f, 0.03f, 0.025f, 0.9f);
        public static readonly Color Accent = new Color(0.95f, 0.66f, 0.26f);
        public static readonly Color TextColor = new Color(0.93f, 0.90f, 0.82f);
        public static readonly Color Muted = new Color(0.62f, 0.59f, 0.52f);
        public static readonly Color Good = new Color(0.55f, 0.86f, 0.5f);
        public static readonly Color Bad = new Color(0.92f, 0.42f, 0.36f);
        public static readonly Color ButtonColor = new Color(0.22f, 0.18f, 0.12f, 0.96f);

        static Font _font;

        public static Font DefaultFont
        {
            get
            {
                if (_font != null) return _font;
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { }
                if (_font == null) { try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { } }
                return _font;
            }
        }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = anchor; r.anchorMax = anchor; r.pivot = pivot;
            r.anchoredPosition = pos; r.sizeDelta = size;
        }

        public static void Region(RectTransform r, float xMin, float yMin, float xMax, float yMax)
        {
            r.anchorMin = new Vector2(xMin, yMin); r.anchorMax = new Vector2(xMax, yMax);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            var r = Rect(name, parent);
            var img = r.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
                                 TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            var r = Rect("Label", parent);
            var t = r.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        public static Button MakeButton(Transform parent, string text, Action onClick, float width = 140f, float height = 40f, int fontSize = 20)
        {
            var img = Panel(parent, "Button_" + text, ButtonColor);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.15f, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.7f, 0.5f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = colors;
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = width; le.preferredHeight = height; le.minWidth = width;
            var label = Label(img.transform, text, fontSize, TextColor, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        /// <summary>Barra horizontal: devuelve el rect de relleno, cuyo ancho se ajusta con SetBar.</summary>
        public static RectTransform Bar(Transform parent, string name, Color fill, out Image backImage)
        {
            backImage = Panel(parent, name, new Color(0f, 0f, 0f, 0.55f));
            var f = Panel(backImage.transform, "Fill", fill);
            f.rectTransform.anchorMin = new Vector2(0f, 0f);
            f.rectTransform.anchorMax = new Vector2(1f, 1f);
            f.rectTransform.offsetMin = new Vector2(2f, 2f);
            f.rectTransform.offsetMax = new Vector2(-2f, -2f);
            return f.rectTransform;
        }

        public static void SetBar(RectTransform fill, float fraction, Color? color = null)
        {
            fraction = Mathf.Clamp01(fraction);
            fill.anchorMax = new Vector2(fraction, 1f);
            if (color.HasValue) fill.GetComponent<Image>().color = color.Value;
        }

        public static RectTransform ScrollList(Transform parent, string name, out RectTransform content)
        {
            var root = Panel(parent, name, new Color(0f, 0f, 0f, 0.28f));
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;

            var viewport = Rect("Viewport", root.transform);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(4f, 0f); content.offsetMax = new Vector2(-4f, 0f);
            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(2, 2, 4, 4);
            vlg.childControlHeight = true; vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return root.rectTransform;
        }

        /// <summary>Fila horizontal para listas (texto flexible + botones a la derecha).</summary>
        public static RectTransform Row(Transform parent, float height = 46f)
        {
            var img = Panel(parent, "Row", new Color(1f, 1f, 1f, 0.05f));
            var h = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8f;
            h.padding = new RectOffset(10, 6, 3, 3);
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            return img.rectTransform;
        }

        public static Text RowText(Transform row, string text, int size = 20)
        {
            var t = Label(row, text, size, TextColor, TextAnchor.MiddleLeft);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f; le.minWidth = 60f;
            return t;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                UnityEngine.Object.Destroy(c);
            }
        }
    }
}
