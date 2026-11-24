using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HuesoOxido.Core
{
    /// <summary>Materiales de placeholder que funcionan con URP o con el pipeline integrado.</summary>
    public static class MaterialUtil
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();
        static Shader _lit;
        static bool _litForUrp;

        /// <summary>True si hay un pipeline scriptable (URP) activo. Los shaders de URP se ven magenta con el pipeline integrado.</summary>
        public static bool UsingScriptablePipeline => GraphicsSettings.currentRenderPipeline != null;

        static Shader LitShader()
        {
            bool urp = UsingScriptablePipeline;
            if (_lit != null && _litForUrp == urp) return _lit;
            _litForUrp = urp;
            _lit = urp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (_lit == null) _lit = Shader.Find("Universal Render Pipeline/Lit");
            if (_lit == null) _lit = Shader.Find("Standard");
            if (_lit == null) _lit = Shader.Find("Sprites/Default");
            return _lit;
        }

        /// <summary>Shader sin iluminacion con transparencia, valido en ambos pipelines (particulas, lluvia).</summary>
        public static Shader ParticleShader()
        {
            Shader s = UsingScriptablePipeline ? Shader.Find("Universal Render Pipeline/Particles/Unlit") : Shader.Find("Particles/Standard Unlit");
            return s != null ? s : Shader.Find("Sprites/Default");
        }

        public static Material Lit(Color color, float smoothness = 0.15f, float metallic = 0f, Texture2D texture = null)
        {
            string key = (UsingScriptablePipeline ? "u" : "b") + ColorUtility.ToHtmlStringRGBA(color) + smoothness.ToString("0.00") + metallic.ToString("0.00")
                         + (texture != null ? texture.GetInstanceID().ToString() : "");
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(LitShader()) { color = color };
            if (texture != null) m.mainTexture = texture;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            Cache[key] = m;
            return m;
        }

        /// <summary>Material con emision (para el indicador del detector).</summary>
        public static Material Emissive(Color color)
        {
            var m = new Material(LitShader()) { color = color };
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 2f);
            }
            return m;
        }

        /// <summary>Crea una primitiva sin collider y con material asignado.</summary>
        public static GameObject Primitive(PrimitiveType type, string name, Transform parent, Material mat, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!keepCollider) { var c = go.GetComponent<Collider>(); if (c != null) Object.Destroy(c); }
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
