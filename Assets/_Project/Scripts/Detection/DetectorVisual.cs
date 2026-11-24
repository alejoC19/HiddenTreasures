using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Detection
{
    /// <summary>Modelo placeholder del detector: en primera persona cuelga de la camara, en tercera del personaje.</summary>
    public sealed class DetectorVisual : MonoBehaviour
    {
        Transform _root;
        Transform _fpAnchor, _tpAnchor;
        IViewState _view;
        MetalDetector _detector;
        IDigActivity _dig;
        Material _lamp;
        bool _inFirstPerson;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public void Initialize(IViewState view, Transform cameraTransform, Transform playerRoot, MetalDetector detector, IDigActivity dig)
        {
            _view = view; _detector = detector; _dig = dig;

            _fpAnchor = new GameObject("DetectorAnchor_FP").transform;
            _fpAnchor.SetParent(cameraTransform, false);
            _fpAnchor.localPosition = new Vector3(0.3f, -0.3f, 0.45f);
            _fpAnchor.localRotation = Quaternion.Euler(25f, -8f, 0f);

            _tpAnchor = new GameObject("DetectorAnchor_TP").transform;
            _tpAnchor.SetParent(playerRoot, false);
            _tpAnchor.localPosition = new Vector3(0.3f, 1.0f, 0.3f);
            _tpAnchor.localRotation = Quaternion.Euler(42f, 0f, 0f);

            Build();
            Attach(false);
        }

        void Build()
        {
            _root = new GameObject("MetalDetector").transform;
            var metal = MaterialUtil.Lit(new Color(0.25f, 0.27f, 0.3f), 0.45f, 0.7f);
            var grip = MaterialUtil.Lit(new Color(0.08f, 0.08f, 0.08f), 0.2f);
            var coil = MaterialUtil.Lit(new Color(0.12f, 0.13f, 0.12f), 0.3f);
            _lamp = MaterialUtil.Emissive(new Color(0.1f, 0.8f, 0.2f));

            var shaft = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Shaft", _root, metal);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localPosition = new Vector3(0f, 0f, 0.5f);
            shaft.transform.localScale = new Vector3(0.025f, 0.55f, 0.025f);

            var handle = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Grip", _root, grip);
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            handle.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            handle.transform.localScale = new Vector3(0.035f, 0.07f, 0.035f);

            var disc = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Coil", _root, coil);
            disc.transform.localPosition = new Vector3(0f, -0.02f, 1.05f);
            disc.transform.localScale = new Vector3(0.3f, 0.012f, 0.3f);

            var box = MaterialUtil.Primitive(PrimitiveType.Cube, "ControlBox", _root, metal);
            box.transform.localPosition = new Vector3(0f, 0.06f, 0.18f);
            box.transform.localScale = new Vector3(0.07f, 0.05f, 0.1f);

            var lamp = MaterialUtil.Primitive(PrimitiveType.Sphere, "Lamp", _root, _lamp);
            lamp.transform.localPosition = new Vector3(0f, 0.1f, 0.2f);
            lamp.transform.localScale = Vector3.one * 0.03f;
        }

        void Attach(bool firstPerson)
        {
            _inFirstPerson = firstPerson;
            _root.SetParent(firstPerson ? _fpAnchor : _tpAnchor, false);
            _root.localPosition = Vector3.zero;
            _root.localRotation = Quaternion.identity;
        }

        void LateUpdate()
        {
            if (_root == null || _view == null) return;
            bool fp = _view.FirstPersonWeight > 0.5f;
            if (fp != _inFirstPerson) Attach(fp);

            bool hidden = (_dig != null && _dig.IsDigging) || !_detector.DetectorEnabled;
            if (_root.gameObject.activeSelf == hidden) _root.gameObject.SetActive(!hidden);
            if (hidden) return;

            // Balanceo suave del barrido.
            float sweep = Mathf.Sin(Time.time * 1.3f) * 7f;
            _root.localRotation = Quaternion.Euler(0f, sweep, 0f);

            float s = _detector.Signal;
            Color c = s < 0.5f ? Color.Lerp(new Color(0.1f, 0.8f, 0.2f), new Color(0.95f, 0.8f, 0.1f), s * 2f)
                               : Color.Lerp(new Color(0.95f, 0.8f, 0.1f), new Color(1f, 0.15f, 0.1f), (s - 0.5f) * 2f);
            _lamp.color = c;
            if (_lamp.HasProperty(EmissionId)) _lamp.SetColor(EmissionId, c * 2f);
        }

        void OnDestroy() { if (_root != null) Destroy(_root.gameObject); }
    }
}
