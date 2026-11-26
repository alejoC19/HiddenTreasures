using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Modelo 3D real ya optimizado por tools/art (geometria por material en varios LOD + texturas aparte).
    /// Meshes[lod][i] se dibuja con Materials[i]. Las unidades son metros; el origen esta en el centro de la base.
    /// </summary>
    public sealed class ArtModel
    {
        public string Name;
        public Vector3 Size;
        public Mesh[][] Meshes;
        public Material[] Materials;
        public int LodCount => Meshes.Length;
        public float Height => Size.y;

        public int Triangles(int lod)
        {
            int t = 0;
            foreach (var m in Meshes[Mathf.Clamp(lod, 0, Meshes.Length - 1)]) if (m != null) t += m.triangles.Length / 3;
            return t;
        }
    }

    /// <summary>Carga assets de Resources/Art/World/&lt;nombre&gt;/ (ver tools/art/prep.mjs) y arma sus materiales URP/Lit.</summary>
    public static class ArtLibrary
    {
        [System.Serializable] sealed class MatInfo
        {
            public string name, diff, nor, mr, alphaMode; public float cutoff, metallic, roughness; public bool doubleSided; public float[] color;
        }
        [System.Serializable] sealed class Manifest { public string name; public float height; public float[] size; public List<MatInfo> materials; }

        static readonly Dictionary<string, ArtModel> Cache = new Dictionary<string, ArtModel>();

        public static bool Exists(string name) => Resources.Load<TextAsset>("Art/World/" + name + "/" + name) != null;

        /// <summary>Devuelve el modelo (con cache) o null si no esta en el proyecto. tint multiplica el color base.</summary>
        public static ArtModel Get(string name, Color? tint = null, float smoothnessScale = 1f)
        {
            string key = name + (tint.HasValue ? ColorUtility.ToHtmlStringRGB(tint.Value) : "") + smoothnessScale.ToString("0.00");
            if (Cache.TryGetValue(key, out var cached) && cached != null && cached.Materials[0] != null) return cached;

            string folder = "Art/World/" + name + "/";
            var ta = Resources.Load<TextAsset>(folder + name);
            if (ta == null) return null;
            var man = JsonUtility.FromJson<Manifest>(ta.text);

            var lods = new List<Mesh[]>();
            for (int lod = 0; ; lod++)
            {
                var go = Resources.Load<GameObject>(folder + name + "_lod" + lod);
                if (go == null) break;
                var byName = new Dictionary<string, Mesh>(); var ordered = new List<Mesh>();
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) if (mf.sharedMesh != null) { byName[mf.gameObject.name] = mf.sharedMesh; ordered.Add(mf.sharedMesh); }
                var arr = new Mesh[man.materials.Count];
                for (int i = 0; i < arr.Length; i++)
                    if (!byName.TryGetValue(man.materials[i].name, out arr[i]) && ordered.Count == arr.Length) arr[i] = ordered[i];   // glTFast puede renombrar el nodo
                lods.Add(arr);
            }
            if (lods.Count == 0) return null;

            var mats = new Material[man.materials.Count];
            for (int i = 0; i < mats.Length; i++) mats[i] = BuildMaterial(folder, name, man.materials[i], tint, smoothnessScale);
            var model = new ArtModel
            {
                Name = name, Meshes = lods.ToArray(), Materials = mats,
                Size = man.size != null && man.size.Length >= 3 ? new Vector3(man.size[0], man.size[1], man.size[2]) : Vector3.one,
            };
            Cache[key] = model;
            return model;
        }

        static string NoExt(string f) => string.IsNullOrEmpty(f) ? null : System.IO.Path.ChangeExtension(f, null);

        static Material BuildMaterial(string folder, string asset, MatInfo mi, Color? tint, float smoothnessScale)
        {
            var shader = MaterialUtil.UsingScriptablePipeline ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            var m = new Material(shader) { name = asset + "_" + mi.name, enableInstancing = true };
            Color c = mi.color != null && mi.color.Length >= 3 ? new Color(mi.color[0], mi.color[1], mi.color[2], 1f) : Color.white;
            // Las cortezas de los escaneos vienen muy oscuras: con la luz del juego salen negras.
            if (mi.alphaMode == "OPAQUE" && (asset.StartsWith("pine") || asset.StartsWith("beech") || asset.StartsWith("dead"))) c *= new Color(1.5f, 1.2f, 0.95f);
            if (tint.HasValue) c *= tint.Value;
            var diff = !string.IsNullOrEmpty(mi.diff) ? Resources.Load<Texture2D>(folder + NoExt(mi.diff)) : null;
            var nor = !string.IsNullOrEmpty(mi.nor) ? Resources.Load<Texture2D>(folder + NoExt(mi.nor)) : null;
            var ms = !string.IsNullOrEmpty(mi.mr) ? Resources.Load<Texture2D>(folder + NoExt(mi.mr).Replace("_mr", "_ms")) : null;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); m.color = c;
            if (diff != null) { m.mainTexture = diff; if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", diff); }
            if (nor != null && m.HasProperty("_BumpMap")) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); }
            if (ms != null && m.HasProperty("_MetallicGlossMap"))
            {
                m.SetTexture("_MetallicGlossMap", ms); m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Metallic", 1f); m.SetFloat("_Smoothness", Mathf.Clamp01(smoothnessScale));
            }
            else
            {
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", Mathf.Clamp01((1f - mi.roughness) * smoothnessScale));
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", mi.metallic);
            }
            if (mi.alphaMode == "MASK" || mi.alphaMode == "BLEND")
            {
                float cut = mi.alphaMode == "MASK" ? Mathf.Clamp(mi.cutoff, 0.2f, 0.7f) : 0.45f;
                if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
                if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", cut);
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = 2450;
            }
            if (mi.doubleSided && m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            return m;
        }
    }
}
