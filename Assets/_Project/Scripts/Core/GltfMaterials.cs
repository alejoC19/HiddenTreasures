using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Convierte los materiales que crea glTFast (Shader Graph "glTF-pbrMetallicRoughness") a URP/Lit estandar:
    /// color, metal/rugosidad por factor, normal, emision y vidrio transparente. Es mas barato y no depende del Shader Graph.
    /// </summary>
    public static class GltfMaterials
    {
        static readonly Dictionary<int, Material> Cache = new Dictionary<int, Material>();

        static Texture Tex(Material m, string prop) => m.HasProperty(prop) ? m.GetTexture(prop) : null;
        static float Num(Material m, string prop, float def) => m.HasProperty(prop) ? m.GetFloat(prop) : def;
        static Color Col(Material m, string prop, Color def) => m.HasProperty(prop) ? m.GetColor(prop) : def;

        public static void ConvertToLit(GameObject root, float smoothnessCap = 1f)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++) dst[i] = Convert(src[i], smoothnessCap);
                r.sharedMaterials = dst;
            }
        }

        static Material Convert(Material m, float smoothCap)
        {
            if (m == null) return null;
            if (!m.HasProperty("baseColorFactor")) return m;            // ya es un material normal
            if (Cache.TryGetValue(m.GetInstanceID(), out var cached) && cached != null) return cached;

            string n = (m.name ?? "").ToLowerInvariant();
            bool paint = n.Contains("paint");
            bool glass = Num(m, "transmissionFactor", 0f) > 0.5f;

            var o = new Material(MaterialUtil.LitMaterialTemplate()) { name = m.name + "_Lit" };
            Color baseCol = Col(m, "baseColorFactor", Color.white);
            var baseTex = Tex(m, "baseColorTexture");
            float metal = Mathf.Clamp01(Num(m, "metallicFactor", 0f));
            float smooth = Mathf.Clamp01(1f - Num(m, "roughnessFactor", 1f));
            if (Tex(m, "metallicRoughnessTexture") != null) { metal = Mathf.Min(metal, 0.85f); smooth = Mathf.Max(smooth, 0.4f); }
            smooth = Mathf.Min(smooth, smoothCap);

            if (o.HasProperty("_BaseColor")) o.SetColor("_BaseColor", baseCol);
            if (o.HasProperty("_Color")) o.SetColor("_Color", baseCol);
            if (baseTex != null) { if (o.HasProperty("_BaseMap")) o.SetTexture("_BaseMap", baseTex); if (o.HasProperty("_MainTex")) o.SetTexture("_MainTex", baseTex); }
            if (o.HasProperty("_Metallic")) o.SetFloat("_Metallic", metal);
            if (o.HasProperty("_Smoothness")) o.SetFloat("_Smoothness", smooth);

            var nor = Tex(m, "normalTexture");
            if (nor != null && !paint && o.HasProperty("_BumpMap"))
            {
                o.SetTexture("_BumpMap", nor);
                o.EnableKeyword("_NORMALMAP");
            }

            // Emision (luces del auto, tablero): solo si hay un color propio o una textura emisiva.
            Color em = Col(m, "emissiveFactor", Color.black);
            bool emissive = Tex(m, "emissiveTexture") == null ? (em.r + em.g + em.b) > 0.05f && !(em.r > 0.95f && em.g > 0.95f && em.b > 0.95f) : false;
            if (emissive && o.HasProperty("_EmissionColor"))
            {
                o.EnableKeyword("_EMISSION");
                o.SetColor("_EmissionColor", em * 2.2f);
                o.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            if (glass)
            {
                SetTransparent(o);
                var tint = new Color(0.08f, 0.1f, 0.12f, 0.28f);
                if (o.HasProperty("_BaseColor")) o.SetColor("_BaseColor", tint);
                if (o.HasProperty("_Smoothness")) o.SetFloat("_Smoothness", 0.97f);
                if (o.HasProperty("_Metallic")) o.SetFloat("_Metallic", 0f);
            }
            Cache[m.GetInstanceID()] = o;
            return o;
        }

        static void SetTransparent(Material m)
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", 5f);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", 10f);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
        }
    }
}
