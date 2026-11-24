using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Efecto al desenterrar una pieza: destello de luz y chispas con el color de su rareza. Las reliquias de la mision ademas
    /// muestran un CUBO dorado que sale del hoyo, gira y flota mientras el personaje la recoge y la limpia (PickingUp + Cleaning).
    /// Todo se genera por codigo; el objeto se destruye solo.
    /// </summary>
    public sealed class FindReveal : MonoBehaviour
    {
        static Texture2D _dot;
        static Material _goldMat;

        Transform _cube, _lightT;
        Light _light;
        float _t, _life;
        Vector3 _origin;
        float _riseHeight;
        Color _color;

        public static void Spawn(Vector3 groundPos, Rarity rarity, bool relicCube)
        {
            var go = new GameObject("FindReveal");
            go.transform.position = groundPos;
            var fx = go.AddComponent<FindReveal>();
            fx.Begin(groundPos, RarityStyle.ColorOf(rarity), (int)rarity, relicCube);
        }

        void Begin(Vector3 pos, Color color, int rarityLevel, bool cube)
        {
            _origin = pos; _color = color;
            _life = cube ? PlayerStateMachine.PickUpSeconds + PlayerStateMachine.CleanSeconds + 0.4f : 1.6f;
            _riseHeight = 1.15f;

            var lightGo = new GameObject("Glow");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = Vector3.up * 0.6f;
            _light = lightGo.AddComponent<Light>();
            _light.type = LightType.Point; _light.color = color; _light.range = 5f + rarityLevel; _light.intensity = 0f;
            _light.shadows = LightShadows.None;
            _lightT = lightGo.transform;

            if (cube)
            {
                if (_goldMat == null)
                {
                    _goldMat = MaterialUtil.Emissive(new Color(1f, 0.78f, 0.28f));
                    if (_goldMat.HasProperty("_Metallic")) _goldMat.SetFloat("_Metallic", 0.9f);
                    if (_goldMat.HasProperty("_Smoothness")) _goldMat.SetFloat("_Smoothness", 0.85f);
                    if (_goldMat.HasProperty("_EmissionColor")) _goldMat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.15f) * 0.9f);
                }
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = "RelicCube";
                var col = c.GetComponent<Collider>(); if (col != null) MaterialUtil.Dispose(col);
                c.GetComponent<Renderer>().sharedMaterial = _goldMat;
                c.transform.SetParent(transform, false);
                c.transform.localScale = Vector3.one * 0.26f;
                _cube = c.transform;
            }
            BuildSparks(color, 14 + rarityLevel * 10, cube);
        }

        void BuildSparks(Color color, int count, bool lingering)
        {
            if (_dot == null)
            {
                _dot = new Texture2D(16, 16, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, name = "RevealDot" };
                var px = new Color32[256];
                for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                {
                    float rr = Mathf.Sqrt((x - 7.5f) * (x - 7.5f) + (y - 7.5f) * (y - 7.5f)) / 8f;
                    px[y * 16 + x] = new Color32(255, 255, 255, (byte)(Mathf.Pow(Mathf.Clamp01(1f - rr), 1.6f) * 255f));
                }
                _dot.SetPixels32(px); _dot.Apply(true);
            }
            var go = new GameObject("Sparks");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 0.15f;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            var sh = Shader.Find("HuesoOxido/AtmosphereSoft");
            Material mat;
            if (sh != null)
            {
                mat = new Material(sh); mat.SetTexture("_MainTex", _dot); mat.SetColor("_Color", Color.white);
                mat.SetFloat("_SrcBlend", 5f); mat.SetFloat("_DstBlend", 1f); mat.SetVector("_NearFade", new Vector4(0f, 0.1f, 0, 0));
            }
            else mat = new Material(MaterialUtil.ParticleShader()) { mainTexture = _dot };
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var m = ps.main;
            m.loop = false; m.playOnAwake = false;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.6f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            m.gravityModifier = 0.25f;
            m.maxParticles = 200;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startColor = new Color(color.r, color.g, color.b, 1f);
            var em = ps.emission;
            em.rateOverTime = 0f;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            if (lingering) { em.rateOverTime = 7f; m.duration = _life; m.loop = false; }
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 38f; shape.radius = 0.2f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var colOver = ps.colorOverLifetime; colOver.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.4f), new GradientColorKey(color * 0.6f, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colOver.color = g;
            ps.Play();
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / _life);
            if (_light != null)
            {
                float flash = k < 0.15f ? k / 0.15f : Mathf.Clamp01(1f - (k - 0.15f) / 0.85f);
                _light.intensity = 3.2f * flash;
            }
            if (_cube != null)
            {
                float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_t / PlayerStateMachine.PickUpSeconds));
                float bob = Mathf.Sin(_t * 3f) * 0.05f;
                _cube.localPosition = Vector3.up * (0.1f + rise * _riseHeight + bob);
                _cube.localRotation = Quaternion.Euler(18f, _t * 140f, 12f);
                float fade = _t > _life - 0.4f ? Mathf.Clamp01((_life - _t) / 0.4f) : 1f;
                _cube.localScale = Vector3.one * 0.26f * (0.6f + 0.4f * rise) * fade;
            }
            if (_t >= _life + 1.2f) Destroy(gameObject);
        }
    }
}
