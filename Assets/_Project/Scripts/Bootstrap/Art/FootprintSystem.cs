using UnityEngine;
using HuesoOxido.Player;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Huellas de botas sobre el camino de tierra, la explanada del campamento y el suelo de las ruinas. Son quads con alfa
    /// (barato, sin depender de un Decal Renderer Feature) en un pool circular de 48: al llenarse se reutiliza la mas vieja,
    /// y cada huella se desvanece sola en unos 28 s. Un solo material compartido y un MaterialPropertyBlock para el alfa.
    /// </summary>
    public sealed class FootprintSystem : MonoBehaviour
    {
        public const int MaxPrints = 48;
        public const float LifeSeconds = 28f;
        const float BaseAlpha = 0.62f;
        static readonly int ColorId = Shader.PropertyToID("_Color");

        struct Print { public Transform T; public MeshRenderer R; public float Born; public bool Used; }

        Print[] _pool;
        int _next;
        PlayerMotor _motor;
        Transform _player;
        WorldData _world;
        Material _mat;
        Mesh _mesh;
        MaterialPropertyBlock _mpb;
        Vector3 _last;
        float _stride;
        bool _left;

        public int ActivePrints { get { int n = 0; foreach (var p in _pool) if (p.Used) n++; return n; } }

        public void Initialize(PlayerMotor motor, Transform player, WorldData world)
        {
            _motor = motor; _player = player; _world = world;
            _mpb = new MaterialPropertyBlock();
            _mesh = BuildQuad(0.17f, 0.36f);
            _mat = BuildMaterial();
            _pool = new Print[MaxPrints];
            var root = new GameObject("Footprints").transform;
            root.SetParent(transform, false);
            for (int i = 0; i < MaxPrints; i++)
            {
                var go = new GameObject("Print");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = _mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = _mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                go.SetActive(false);
                _pool[i] = new Print { T = go.transform, R = r };
            }
            _last = player.position;
        }

        static Mesh BuildQuad(float w, float l)
        {
            var m = new Mesh { name = "PrintQuad" };
            m.vertices = new[] { new Vector3(-w / 2, 0, -l / 2), new Vector3(w / 2, 0, -l / 2), new Vector3(w / 2, 0, l / 2), new Vector3(-w / 2, 0, l / 2) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Huella de bota: suela ovalada con taco/tacon separados y surcos; el resto es transparente.</summary>
        static Texture2D BuildTexture()
        {
            const int w = 32, h = 64;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "BootPrint" };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x / (float)(w - 1) - 0.5f) * 2f, v = y / (float)(h - 1);
                    float a = 0f;
                    // taco (arriba, v 0.42..0.97) y tacon (abajo, v 0.03..0.28)
                    float forefoot = 1f - (u * u) / (0.85f * 0.85f) - Mathf.Pow((v - 0.7f) / 0.28f, 2f);
                    float heel = 1f - (u * u) / (0.62f * 0.62f) - Mathf.Pow((v - 0.16f) / 0.14f, 2f);
                    a = Mathf.Max(forefoot, heel);
                    a = Mathf.Clamp01(a * 5f);
                    float groove = Mathf.Abs(Mathf.Sin(v * 30f)) > 0.82f ? 0.35f : 0f;   // surcos de la suela
                    a *= 1f - groove;
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            t.SetPixels32(px); t.Apply(true);
            return t;
        }

        static Material BuildMaterial()
        {
            var sh = Shader.Find("HuesoOxido/AtmosphereSoft");
            Material m;
            if (sh != null)
            {
                m = new Material(sh);
                m.SetTexture("_MainTex", BuildTexture());
                m.SetColor("_Color", new Color(0.08f, 0.05f, 0.03f, BaseAlpha));
                m.SetFloat("_SrcBlend", 5f); m.SetFloat("_DstBlend", 10f);
                m.SetVector("_NearFade", new Vector4(0f, 0.05f, 0f, 0f));
                m.renderQueue = 2460;
            }
            else
            {
                m = new Material(MaterialUtil.ParticleShader()) { mainTexture = BuildTexture(), color = new Color(0.08f, 0.05f, 0.03f, BaseAlpha) };
            }
            m.name = "Footprint";
            return m;
        }

        void Update()
        {
            if (_motor == null || _world == null) return;
            Vector3 p = _player.position;
            Vector3 d = p - _last; d.y = 0f;
            _last = p;
            float moved = d.magnitude;
            bool walking = _motor.ControlEnabled && _motor.IsGrounded && moved > 0.0005f && moved < 1.5f;
            if (walking)
            {
                _stride += moved;
                float step = _motor.IsCrouched ? 0.5f : (_motor.IsSprinting ? 1.05f : 0.78f);
                if (_stride >= step)
                {
                    _stride = 0f;
                    if (_world.IsSoftGround(p.x, p.z)) Place(p, d.normalized);
                    _left = !_left;
                }
            }
            Fade();
        }

        void Place(Vector3 pos, Vector3 dir)
        {
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            Vector3 at = pos + right * (_left ? -0.11f : 0.11f);
            float x = at.x, z = at.z;
            float h = _world.HeightAt(x, z);
            // normal del terreno por diferencias finitas
            float dx = _world.HeightAt(x + 0.4f, z) - _world.HeightAt(x - 0.4f, z);
            float dz = _world.HeightAt(x, z + 0.4f) - _world.HeightAt(x, z - 0.4f);
            Vector3 n = new Vector3(-dx, 0.8f, -dz).normalized;
            var rot = Quaternion.FromToRotation(Vector3.up, n) * Quaternion.LookRotation(dir, Vector3.up);

            var pr = _pool[_next];
            _pool[_next].Born = Time.time; _pool[_next].Used = true;
            pr.T.gameObject.SetActive(true);
            pr.T.SetPositionAndRotation(new Vector3(x, h + 0.025f, z), rot);
            SetAlpha(pr.R, BaseAlpha);
            _next = (_next + 1) % MaxPrints;
        }

        void Fade()
        {
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].Used) continue;
                float age = (Time.time - _pool[i].Born) / LifeSeconds;
                if (age >= 1f) { _pool[i].Used = false; _pool[i].T.gameObject.SetActive(false); continue; }
                float k = 1f - Mathf.SmoothStep(0.5f, 1f, age);
                SetAlpha(_pool[i].R, BaseAlpha * k);
            }
        }

        void SetAlpha(MeshRenderer r, float a)
        {
            _mpb.SetColor(ColorId, new Color(0.08f, 0.05f, 0.03f, a));
            r.SetPropertyBlock(_mpb);
        }
    }
}
