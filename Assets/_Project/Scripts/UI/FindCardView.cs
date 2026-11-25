using UnityEngine;
using UnityEngine.UI;
using HuesoOxido.Core;

namespace HuesoOxido.UI
{
    /// <summary>
    /// Tarjeta de hallazgo: aparece arriba al centro cuando termina la limpieza de una pieza, con el color de su rareza, nombre,
    /// categoria, descripcion y valor. Dura mas cuanto mas rara es la pieza. Usa tiempo sin escala.
    /// </summary>
    public sealed class FindCardView : MonoBehaviour
    {
        GameObject _root;
        CanvasGroup _group;
        Image _stripe, _banner;
        Text _banner_text, _title, _meta, _desc, _value;
        float _t = -1f, _life;

        public bool IsShowing => _t >= 0f;

        public void Initialize(Transform canvas)
        {
            var panel = UiFactory.Panel(canvas, "FindCard", new Color(0.05f, 0.045f, 0.035f, 0.92f));
            _root = panel.gameObject;
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(700f, 168f));
            _group = _root.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false; _group.interactable = false;

            _stripe = UiFactory.Panel(panel.transform, "Stripe", Color.white);
            UiFactory.Region(_stripe.rectTransform, 0f, 0f, 0.012f, 1f);
            _banner_text = UiFactory.Label(panel.transform, "", 15, UiFactory.Muted, TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Place(_banner_text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -10f), new Vector2(640f, 20f));
            _title = UiFactory.Label(panel.transform, "", 30, UiFactory.TextColor, TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -32f), new Vector2(650f, 40f));
            _meta = UiFactory.Label(panel.transform, "", 17, UiFactory.Muted, TextAnchor.UpperLeft);
            UiFactory.Place(_meta.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -72f), new Vector2(650f, 24f));
            _desc = UiFactory.Label(panel.transform, "", 18, UiFactory.TextColor, TextAnchor.UpperLeft);
            UiFactory.Place(_desc.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -98f), new Vector2(650f, 48f));
            _value = UiFactory.Label(panel.transform, "", 22, UiFactory.Accent, TextAnchor.LowerRight, FontStyle.Bold);
            UiFactory.Place(_value.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-18f, 8f), new Vector2(300f, 30f));
            _root.SetActive(false);
        }

        public void Show(FindCardData d)
        {
            if (d == null) return;
            Color c = RarityStyle.ColorOf(d.Rarity);
            _stripe.color = c;
            _banner_text.text = (d.IsRelic ? "★ RELIQUIA PREHISPÁNICA  ·  " : "") + "<color=#" + UiFactory.Hex(c) + ">" + d.RarityName.ToUpperInvariant() + "</color>"
                                + (d.IsNew ? "   <color=#" + UiFactory.Hex(UiFactory.Good) + ">¡NUEVA EN TU COLECCIÓN!</color>" : "");
            _title.text = "<color=#" + UiFactory.Hex(c) + ">" + d.Title + "</color>";
            _meta.text = d.CategoryName;
            _desc.text = d.Description;
            _value.text = "Valor estimado  $" + d.Value;
            _life = d.Seconds; _t = 0f;
            _root.SetActive(true);
            _group.alpha = 0f;
        }

        public void Hide() { _t = -1f; if (_root != null) _root.SetActive(false); }

        void Update()
        {
            if (_t < 0f) return;
            _t += Time.unscaledDeltaTime;
            float fadeIn = Mathf.Clamp01(_t / 0.25f), fadeOut = Mathf.Clamp01((_life - _t) / 0.5f);
            _group.alpha = Mathf.Min(fadeIn, fadeOut);
            if (_t >= _life) Hide();
        }
    }
}
