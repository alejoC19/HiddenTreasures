using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>Materiales con texturas PBR (URP Lit o Standard segun el pipeline) y de follaje con transparencia por recorte.</summary>
    public static class ArtMaterials
    {
        static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>();

        static Shader Lit()
        {
            var s = MaterialUtil.UsingScriptablePipeline ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            return s != null ? s : Shader.Find("Sprites/Default");
        }

        /// <summary>Material opaco con textura de color y normales (por nombre en Resources/Art/Textures, sin sufijo).</summary>
        public static Material Textured(string name, Color tint, float smoothness = 0.12f)
        {
            string key = "T:" + name + ColorUtility.ToHtmlStringRGB(tint) + smoothness.ToString("0.00") + (MaterialUtil.UsingScriptablePipeline ? "u" : "b");
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Lit()) { name = name, color = tint };
            var diff = TextureLibrary.Get(name + "_diff");
            if (diff != null) m.mainTexture = diff;
            var nor = TextureLibrary.Get(name + "_nor");
            if (nor != null && m.HasProperty("_BumpMap")) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); }
            SetSmooth(m, smoothness);
            m.enableInstancing = true;
            Cache[key] = m;
            return m;
        }

        /// <summary>Material con texturas cargadas por ruta de Resources (props de Poly Haven).</summary>
        public static Material TexturedPath(string diffPath, string norPath, Color tint, float smoothness)
        {
            string key = "P:" + diffPath + (MaterialUtil.UsingScriptablePipeline ? "u" : "b");
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Lit()) { name = diffPath, color = tint };
            var diff = Resources.Load<Texture2D>(diffPath);
            if (diff != null) m.mainTexture = diff;
            var nor = norPath != null ? Resources.Load<Texture2D>(norPath) : null;
            if (nor != null && m.HasProperty("_BumpMap")) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); }
            SetSmooth(m, smoothness);
            Cache[key] = m;
            return m;
        }

        /// <summary>Follaje: textura con alfa, recorte, doble cara y poco brillo.</summary>
        public static Material Foliage(Texture2D tex, Color tint, string id)
        {
            string key = "F:" + id + ColorUtility.ToHtmlStringRGB(tint) + (MaterialUtil.UsingScriptablePipeline ? "u" : "b");
            if (Cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Lit()) { name = "Foliage_" + id, color = tint, mainTexture = tex };
            SetSmooth(m, 0.08f);
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.42f);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);   // URP
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);              // doble cara
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 1f);              // Standard: Cutout
            m.EnableKeyword("_ALPHATEST_ON");
            m.renderQueue = 2450;
            m.enableInstancing = true;
            Cache[key] = m;
            return m;
        }

        static void SetSmooth(Material m, float s)
        {
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", s);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", s);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        }
    }
}
