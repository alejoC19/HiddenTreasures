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
        Transform _tpHand, _player;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public void Initialize(IViewState view, Transform cameraTransform, Transform playerRoot, MetalDetector detector, IDigActivity dig, Transform tpHand = null)
        {
            _view = view; _detector = detector; _dig = dig; _tpHand = tpHand; _player = playerRoot;

            _fpAnchor = new GameObject("DetectorAnchor_FP").transform;
            _fpAnchor.SetParent(cameraTransform, false);
            _fpAnchor.localPosition = new Vector3(0.3f, -0.3f, 0.45f);
            _fpAnchor.localRotation = Quaternion.Euler(25f, -8f, 0f);

            _tpAnchor = new GameObject("DetectorAnchor_TP").transform;
            _tpAnchor.SetParent(tpHand != null ? tpHand : playerRoot, false);
            if (tpHand == null)
            {
                _tpAnchor.localPosition = new Vector3(0.3f, 1.0f, 0.3f);
                _tpAnchor.localRotation = Quaternion.Euler(42f, 0f, 0f);
            }

            Build();
            Attach(false);
        }

        void Build()
        {
            _root = new GameObject("MetalDetector").transform;
            var steel = ProceduralSurfaces.Steel("det_steel", new Color(0.55f, 0.57f, 0.6f), 0.55f, 0.8f);
            var plastic = ProceduralSurfaces.Steel("det_plastic", new Color(0.1f, 0.1f, 0.11f), 0.35f, 0.1f);
            var yellow = ProceduralSurfaces.WornPaint("det_yellow", new Color(0.85f, 0.65f, 0.12f), 0.15f);
            var rubber = ProceduralSurfaces.Rubber();
            var screen = MaterialUtil.Lit(new Color(0.05f, 0.1f, 0.07f), 0.9f, 0f);
            _lamp = MaterialUtil.Emissive(new Color(0.1f, 0.8f, 0.2f));

            // Empunadura con agarre de goma y correa del antebrazo.
            var grip = Part(PrimitiveType.Cylinder, "Grip", rubber, new Vector3(0f, 0f, 0f), new Vector3(0.04f, 0.075f, 0.04f));
            grip.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Part(PrimitiveType.Cube, "GripPost", plastic, new Vector3(0f, 0.03f, 0.1f), new Vector3(0.035f, 0.09f, 0.04f));
            var cuff = Part(PrimitiveType.Cube, "ArmCuff", plastic, new Vector3(0f, 0.06f, -0.2f), new Vector3(0.08f, 0.015f, 0.2f));
            cuff.transform.localRotation = Quaternion.Euler(-5f, 0f, 0f);
            Part(PrimitiveType.Cube, "ArmRest", plastic, new Vector3(0f, 0.03f, -0.08f), new Vector3(0.05f, 0.05f, 0.16f));

            // Caja de control con pantalla y perillas.
            Part(PrimitiveType.Cube, "ControlBox", plastic, new Vector3(0f, 0.1f, 0.2f), new Vector3(0.1f, 0.07f, 0.14f));
            var scr = Part(PrimitiveType.Cube, "Screen", screen, new Vector3(0f, 0.136f, 0.2f), new Vector3(0.07f, 0.004f, 0.085f));
            scr.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            for (int i = 0; i < 2; i++)
            {
                var knob = Part(PrimitiveType.Cylinder, "Knob", yellow, new Vector3(-0.03f + i * 0.06f, 0.14f, 0.13f), new Vector3(0.022f, 0.008f, 0.022f));
                knob.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
            }
            var lamp = Part(PrimitiveType.Cylinder, "Lamp", _lamp, new Vector3(0f, 0.14f, 0.27f), new Vector3(0.018f, 0.006f, 0.018f));
            lamp.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);

            // Varilla en dos tramos (el inferior telescopico) con abrazaderas y el disco inclinado al final.
            var upper = Part(PrimitiveType.Cylinder, "ShaftUpper", steel, new Vector3(0f, 0.06f, 0.55f), new Vector3(0.03f, 0.35f, 0.03f));
            upper.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var lower = Part(PrimitiveType.Cylinder, "ShaftLower", yellow, new Vector3(0f, 0.02f, 1.0f), new Vector3(0.024f, 0.22f, 0.024f));
            lower.transform.localRotation = Quaternion.Euler(95f, 0f, 0f);
            Part(PrimitiveType.Cylinder, "Clamp", plastic, new Vector3(0f, 0.04f, 0.78f), new Vector3(0.04f, 0.018f, 0.04f)).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var coilRoot = new GameObject("Coil").transform;
            coilRoot.SetParent(_root, false);
            coilRoot.localPosition = new Vector3(0f, -0.06f, 1.28f);
            coilRoot.localRotation = Quaternion.Euler(10f, 0f, 0f);
            var disc = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Disc", coilRoot, plastic);
            disc.transform.localScale = new Vector3(0.34f, 0.012f, 0.34f);
            var ring = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Ring", coilRoot, yellow);
            ring.transform.localPosition = new Vector3(0f, -0.004f, 0f);
            ring.transform.localScale = new Vector3(0.355f, 0.008f, 0.355f);
            var core = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Core", coilRoot, steel);
            core.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            core.transform.localScale = new Vector3(0.08f, 0.01f, 0.08f);
            var bracket = MaterialUtil.Primitive(PrimitiveType.Cube, "Bracket", coilRoot, steel);
            bracket.transform.localPosition = new Vector3(0f, 0.02f, -0.14f);
            bracket.transform.localScale = new Vector3(0.05f, 0.03f, 0.08f);

            // Cable en espiral (varios segmentos) desde la caja hasta el disco.
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f;
                var seg = Part(PrimitiveType.Cylinder, "Cable", rubber, new Vector3(0.025f, 0.075f - t * 0.06f, 0.22f + t * 1.0f), new Vector3(0.008f, 0.09f, 0.008f));
                seg.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        GameObject Part(PrimitiveType type, string name, Material m, Vector3 pos, Vector3 scale)
        {
            var go = MaterialUtil.Primitive(type, name, _root, m);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            return go;
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

            if (!fp && _tpHand != null && _player != null)
            {
                // En la mano del personaje: misma direccion que el brazo (ver PlaceholderCharacterAnimator).
                float arm = Mathf.Sin(Time.time * 1.3f) * 14f;
                Vector3 dir = Quaternion.AngleAxis(arm, Vector3.up) * (Quaternion.AngleAxis(24f, _player.right) * _player.forward);
                _root.rotation = Quaternion.LookRotation(dir, Vector3.up);
                _root.position = _tpHand.position + dir * -0.04f;
            }
            else
            {
                // Balanceo suave del barrido.
                float sweep = Mathf.Sin(Time.time * 1.3f) * 7f;
                _root.localRotation = Quaternion.Euler(0f, sweep, 0f);
            }

            float s = _detector.Signal;
            Color c = s < 0.5f ? Color.Lerp(new Color(0.1f, 0.8f, 0.2f), new Color(0.95f, 0.8f, 0.1f), s * 2f)
                               : Color.Lerp(new Color(0.95f, 0.8f, 0.1f), new Color(1f, 0.15f, 0.1f), (s - 0.5f) * 2f);
            _lamp.color = c;
            if (_lamp.HasProperty(EmissionId)) _lamp.SetColor(EmissionId, c * 2f);
        }

        void OnDestroy() { if (_root != null) Destroy(_root.gameObject); }
    }
}
