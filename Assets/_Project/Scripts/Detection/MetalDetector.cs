using UnityEngine;

namespace HuesoOxido.Detection
{
    /// <summary>Calcula la señal por proximidad a piezas enterradas. Igual para ambas perspectivas.</summary>
    public sealed class MetalDetector : MonoBehaviour
    {
        [SerializeField] float baseRange = 4.5f;
        [SerializeField] float digReadyRadius = 1.15f;
        [SerializeField] float headForward = 1.1f;
        [SerializeField] float headSide = 0.3f;

        SiteRegistry _registry;
        Transform _player;
        float _signal;
        float _rangeBonus;

        public bool DetectorEnabled { get; set; } = true;
        public float Range => baseRange + _rangeBonus;
        /// <summary>0 = nada, 1 = justo encima.</summary>
        public float Signal => _signal;
        public BuriedSite Target { get; private set; }
        public bool InDigRange { get; private set; }
        public float HorizontalDistance { get; private set; }

        public void Initialize(SiteRegistry registry, Transform player) { _registry = registry; _player = player; }
        public void SetRangeBonus(float meters) { _rangeBonus = Mathf.Max(0f, meters); }

        public Vector3 HeadPosition
            => _player.position + _player.forward * headForward + _player.right * headSide;

        void Update()
        {
            if (_registry == null || _player == null) return;
            float raw = 0f;
            BuriedSite best = null;
            float bestScore = 0f;
            Vector3 head = HeadPosition;
            float range = Range;

            if (DetectorEnabled)
            {
                var sites = _registry.Sites;
                for (int i = 0; i < sites.Count; i++)
                {
                    var s = sites[i];
                    if (s.Excavated) continue;
                    Vector3 p = s.transform.position;
                    float dx = p.x - head.x, dz = p.z - head.z;
                    float h = Mathf.Sqrt(dx * dx + dz * dz);
                    float dist = Mathf.Sqrt(h * h + s.DepthMeters * s.DepthMeters);
                    if (dist >= range) continue;
                    float score = Mathf.Pow(1f - dist / range, 1.4f);
                    if (score > bestScore) { bestScore = score; best = s; HorizontalDistance = h; }
                }
                raw = bestScore;
            }

            Target = best;
            InDigRange = best != null && HorizontalDistance <= digReadyRadius;
            _signal = Mathf.Lerp(_signal, raw, 1f - Mathf.Exp(-9f * Time.deltaTime));
            if (best == null) HorizontalDistance = float.MaxValue;
        }
    }
}
