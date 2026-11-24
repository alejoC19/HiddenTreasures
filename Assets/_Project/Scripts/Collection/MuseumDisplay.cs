using System.Collections.Generic;
using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Collection
{
    /// <summary>Vitrina del campamento: un pedestal por tipo de pieza, que se ilumina cuando se exhibe.</summary>
    public sealed class MuseumDisplay : MonoBehaviour
    {
        readonly Dictionary<string, GameObject> _pieces = new Dictionary<string, GameObject>();
        CollectionLog _log;

        public void Initialize(CollectionLog log, IReadOnlyList<IItemInfo> items)
        {
            _log = log;
            var stone = MaterialUtil.Lit(new Color(0.42f, 0.4f, 0.36f), 0.2f);
            int n = items.Count;
            int perRow = 6;
            for (int i = 0; i < n; i++)
            {
                int row = i / perRow, col = i % perRow;
                var pos = new Vector3((col - (perRow - 1) * 0.5f) * 0.9f, 0f, -row * 1.1f);
                var ped = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Pedestal_" + items[i].Id, transform, stone);
                ped.transform.localPosition = pos + new Vector3(0f, 0.4f, 0f);
                ped.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
                var piece = MaterialUtil.Primitive(items[i].Category == FindCategory.Fossil ? PrimitiveType.Sphere : PrimitiveType.Cube,
                    "Piece_" + items[i].Id, transform, MaterialUtil.Emissive(RarityStyle.ColorOf(items[i].Rarity)));
                piece.transform.localPosition = pos + new Vector3(0f, 0.95f, 0f);
                piece.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
                piece.SetActive(false);
                _pieces[items[i].Id] = piece;
            }
            log.Changed += Refresh;
            Refresh();
        }

        void OnDestroy() { if (_log != null) _log.Changed -= Refresh; }

        void Refresh()
        {
            foreach (var kv in _pieces) kv.Value.SetActive(_log.IsDisplayed(kv.Key));
        }

        void Update()
        {
            foreach (var kv in _pieces)
                if (kv.Value.activeSelf) kv.Value.transform.Rotate(0f, 45f * Time.deltaTime, 0f);
        }
    }
}
