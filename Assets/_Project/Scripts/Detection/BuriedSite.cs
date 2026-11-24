using System.Collections.Generic;
using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Detection
{
    /// <summary>Punto del terreno con una pieza enterrada. El id es estable para el guardado.</summary>
    public sealed class BuriedSite : MonoBehaviour
    {
        [SerializeField] string siteId;
        [SerializeField] string zoneId = "forest_ruins";
        [SerializeField] string itemId;
        [SerializeField] float depthMeters = 0.45f;
        [SerializeField] float digSeconds = 3.5f;

        GameObject _hint;
        GameObject _hole;

        public string SiteId => siteId;
        public string ZoneId => zoneId;
        public string ItemId => itemId;
        public float DepthMeters => depthMeters;
        public float DigSeconds => digSeconds;
        public bool Excavated { get; private set; }

        public void Setup(string id, string zone, string item, float depth, float seconds)
        {
            siteId = id; zoneId = zone; itemId = item; depthMeters = depth; digSeconds = seconds;
        }

        /// <summary>Marca visual (tierra removida) que sirve de pista sin el detector.</summary>
        public void ShowHint()
        {
            if (_hint != null || Excavated) return;
            var mat = MaterialUtil.Lit(new Color(0.22f, 0.16f, 0.10f), 0.05f);
            _hint = MaterialUtil.Primitive(PrimitiveType.Cylinder, "SoilPatch", transform, mat);
            _hint.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            _hint.transform.localScale = new Vector3(0.8f, 0.012f, 0.8f);
        }

        public void MarkExcavated(bool animate)
        {
            if (Excavated) return;
            Excavated = true;
            if (_hint != null) Destroy(_hint);
            var mat = MaterialUtil.Lit(new Color(0.12f, 0.08f, 0.05f), 0.02f);
            _hole = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Hole", transform, mat);
            _hole.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            _hole.transform.localScale = new Vector3(0.7f, 0.012f, 0.7f);
            var mound = MaterialUtil.Primitive(PrimitiveType.Sphere, "Mound", transform, MaterialUtil.Lit(new Color(0.28f, 0.2f, 0.12f), 0.05f));
            mound.transform.localPosition = new Vector3(0.55f, 0.02f, 0.1f);
            mound.transform.localScale = new Vector3(0.7f, 0.22f, 0.55f);
        }
    }

    /// <summary>Lista de sitios de la escena (sin singleton: se inyecta).</summary>
    public sealed class SiteRegistry
    {
        readonly List<BuriedSite> _sites = new List<BuriedSite>();
        public IReadOnlyList<BuriedSite> Sites => _sites;

        public void Add(BuriedSite s) { if (s != null && !_sites.Contains(s)) _sites.Add(s); }

        public BuriedSite ById(string id)
        {
            for (int i = 0; i < _sites.Count; i++) if (_sites[i].SiteId == id) return _sites[i];
            return null;
        }

        public List<string> ExcavatedIds()
        {
            var l = new List<string>();
            foreach (var s in _sites) if (s.Excavated) l.Add(s.SiteId);
            return l;
        }
    }
}
