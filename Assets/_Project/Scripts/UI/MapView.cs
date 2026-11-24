using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HuesoOxido.UI
{
    public sealed class MapMarker
    {
        public string Label;
        public Color Color;
        public System.Func<Vector3> Position;
    }

    /// <summary>Mapa cenital. La textura la genera quien construye el mundo; aca solo se dibuja y se ubican los marcadores.</summary>
    public sealed class MapView : MonoBehaviour
    {
        const float Size = 760f;

        GameObject _root;
        Rect _bounds;
        Transform _player;
        RectTransform _playerMarker;
        readonly List<(MapMarker marker, RectTransform rect)> _markers = new List<(MapMarker, RectTransform)>();
        bool _open;

        public bool IsOpen => _open;

        public void Initialize(Transform canvas, Texture2D mapTexture, Rect worldBounds, Transform player, IEnumerable<MapMarker> markers)
        {
            _bounds = worldBounds; _player = player;
            var dim = UiFactory.Panel(canvas, "MapView", new Color(0f, 0f, 0f, 0.6f));
            _root = dim.gameObject;
            UiFactory.Stretch(dim.rectTransform);
            var frame = UiFactory.Panel(dim.transform, "Frame", UiFactory.PanelColor);
            UiFactory.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Size + 40f, Size + 90f));
            var title = UiFactory.Label(frame.transform, "MAPA  ·  Bosque de las Ruinas", 26, UiFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -10f), new Vector2(Size, 40f));

            var mapRect = UiFactory.Rect("Map", frame.transform);
            UiFactory.Place(mapRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(Size, Size));
            var raw = mapRect.gameObject.AddComponent<RawImage>();
            raw.texture = mapTexture;

            foreach (var m in markers) AddMarker(mapRect, m);

            _playerMarker = UiFactory.Rect("Player", mapRect);
            UiFactory.Place(_playerMarker, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(16f, 16f));
            var diamond = UiFactory.Panel(_playerMarker, "Dot", new Color(1f, 0.95f, 0.3f));
            UiFactory.Stretch(diamond.rectTransform);
            diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            var nose = UiFactory.Panel(_playerMarker, "Nose", new Color(1f, 0.95f, 0.3f));
            UiFactory.Place(nose.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(4f, 16f));
            _root.SetActive(false);
        }

        void AddMarker(RectTransform parent, MapMarker m)
        {
            var r = UiFactory.Rect("Marker_" + m.Label, parent);
            UiFactory.Place(r, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f));
            var img = r.gameObject.AddComponent<Image>();
            img.color = m.Color;
            var t = UiFactory.Label(r, m.Label, 16, UiFactory.TextColor, TextAnchor.MiddleLeft);
            UiFactory.Place(t.rectTransform, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(180f, 24f));
            _markers.Add((m, r));
        }

        public void Open() { _open = true; _root.SetActive(true); }
        public void Close() { _open = false; _root.SetActive(false); }

        Vector2 ToMap(Vector3 world)
        {
            float nx = Mathf.InverseLerp(_bounds.xMin, _bounds.xMax, world.x);
            float ny = Mathf.InverseLerp(_bounds.yMin, _bounds.yMax, world.z);
            return new Vector2(nx * Size, ny * Size);
        }

        void Update()
        {
            if (!_open) return;
            foreach (var (marker, rect) in _markers) rect.anchoredPosition = ToMap(marker.Position());
            _playerMarker.anchoredPosition = ToMap(_player.position);
            _playerMarker.localRotation = Quaternion.Euler(0f, 0f, -_player.eulerAngles.y);
        }
    }
}
