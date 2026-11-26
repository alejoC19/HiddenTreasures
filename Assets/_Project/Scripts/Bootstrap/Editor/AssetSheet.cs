using System.IO;
using UnityEditor;
using UnityEngine;

namespace HuesoOxido.Core.EditorTools
{
    /// <summary>
    /// Evalua assets 3D candidatos dentro de Unity real: instancia cada modelo (.glb importado) con luz de dia y lo fotografia desde
    /// dos angulos, con triangulos y tamano reales en la etiqueta del log. Batch:
    /// -executeMethod HuesoOxido.Core.EditorTools.AssetSheet.RunBatch  (HT_SHEET_DIR = carpeta bajo Assets, HT_SHEET_OUT = salida)
    /// </summary>
    public static class AssetSheet
    {
        public static void RunBatch()
        {
            try { Generate(); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogError("[SHEET] " + e); EditorApplication.Exit(1); }
        }

        /// <summary>Modo "art": fotografia los assets ya procesados (Resources/Art/World/*) con ArtLibrary, tal como los vera el juego.</summary>
        static void GenerateArt(UnityEngine.Camera cam, GameObject ground, string outDir)
        {
            string only = System.Environment.GetEnvironmentVariable("HT_SHEET_FILTER");
            foreach (var ta in Resources.LoadAll<TextAsset>("Art/World"))
            {
                string name = ta.name;
                if (!string.IsNullOrEmpty(only) && !(";" + only + ";").Contains(";" + name + ";")) continue;
                var model = ArtLibrary.Get(name);
                if (model == null) { Debug.LogWarning("[SHEET] sin modelo " + name); continue; }
                var root = new GameObject(name);
                for (int i = 0; i < model.Meshes[0].Length; i++)
                {
                    if (model.Meshes[0][i] == null) continue;
                    var g = new GameObject("part" + i); g.transform.SetParent(root.transform, false);
                    g.AddComponent<MeshFilter>().sharedMesh = model.Meshes[0][i];
                    g.AddComponent<MeshRenderer>().sharedMaterial = model.Materials[i];
                }
                var rs = root.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) { Debug.LogWarning("[SHEET] " + name + " sin mallas"); Object.DestroyImmediate(root); continue; }
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                ground.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
                float size = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                ground.transform.localScale = Vector3.one * Mathf.Max(1f, size * 0.4f);
                Debug.Log("[SHEET] " + name + " lods=" + model.LodCount + " tris=" + model.Triangles(0) + "/" + model.Triangles(model.LodCount - 1) + " tamano=" + model.Size.ToString("F2"));
                float dist = size / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) * 1.25f;
                for (int v = 0; v < 2; v++)
                {
                    Vector3 dirV = v == 0 ? new Vector3(0.8f, 0.35f, -1f).normalized : new Vector3(-0.9f, 0.2f, 0.55f).normalized;
                    cam.transform.position = b.center + dirV * dist; cam.transform.LookAt(b.center);
                    PreviewRenderer.Capture(cam, Path.Combine(outDir, name + "_" + v + ".png"));
                }
                Object.DestroyImmediate(root);
            }
        }

        static void Generate()
        {
            string dir = System.Environment.GetEnvironmentVariable("HT_SHEET_DIR") ?? "Assets/_Project/Art/Candidates";
            string outDir = System.Environment.GetEnvironmentVariable("HT_SHEET_OUT") ?? Path.Combine(Directory.GetCurrentDirectory(), "Sheet");
            Directory.CreateDirectory(outDir);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.6f; sun.color = new Color(1f, 0.95f, 0.86f); sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            var camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.62f, 0.7f, 0.78f);
            cam.allowHDR = true; cam.fieldOfView = 35f; cam.nearClipPlane = 0.01f; cam.farClipPlane = 5000f;
            PostProcessSetup.Apply(cam);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.GetComponent<MeshRenderer>().sharedMaterial = MaterialUtil.Lit(new Color(0.35f, 0.33f, 0.28f), 0.05f);

            if (System.Environment.GetEnvironmentVariable("HT_SHEET_MODE") == "art") { GenerateArt(cam, ground, outDir); return; }
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".glb") && !path.EndsWith(".gltf")) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { Debug.LogWarning("[SHEET] no se pudo cargar " + path); continue; }
                string only = System.Environment.GetEnvironmentVariable("HT_SHEET_FILTER");
                if (!string.IsNullOrEmpty(only) && !(";" + only + ";").Contains(";" + Path.GetFileNameWithoutExtension(path) + ";")) continue;
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (System.Environment.GetEnvironmentVariable("HT_SHEET_DEBUG") != null)
                    foreach (var rr in inst.GetComponentsInChildren<Renderer>())
                        foreach (var mm in rr.sharedMaterials)
                        {
                            if (mm == null) continue;
                            var sb = new System.Text.StringBuilder("[SHEET-MAT] " + mm.name + " shader=" + mm.shader.name + " :");
                            foreach (var pn in mm.GetTexturePropertyNames()) { var t = mm.GetTexture(pn); if (t != null) sb.Append(" " + pn + "=" + t.name + "(" + t.width + ")"); }
                            Debug.Log(sb.ToString());
                        }
                GltfMaterials.ConvertToLit(inst);
                var rs = inst.GetComponentsInChildren<Renderer>();
                if (rs.Length == 0) { Object.DestroyImmediate(inst); continue; }
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                int tris = 0; foreach (var mf in inst.GetComponentsInChildren<MeshFilter>()) if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
                foreach (var sm in inst.GetComponentsInChildren<SkinnedMeshRenderer>()) if (sm.sharedMesh != null) tris += sm.sharedMesh.triangles.Length / 3;
                ground.transform.position = new Vector3(b.center.x, b.min.y, b.center.z);
                float size = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
                ground.transform.localScale = Vector3.one * Mathf.Max(1f, size * 0.4f);
                string name = Path.GetFileNameWithoutExtension(path);
                Debug.Log("[SHEET] " + name + " tris=" + tris + " tamano=" + b.size.ToString("F2") + " renderers=" + rs.Length);
                float dist = size / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad)) * 1.25f;
                for (int v = 0; v < 2; v++)
                {
                    Vector3 dirV = v == 0 ? new Vector3(0.8f, 0.35f, -1f).normalized : new Vector3(-0.9f, 0.2f, 0.55f).normalized;
                    cam.transform.position = b.center + dirV * dist;
                    cam.transform.LookAt(b.center);
                    PreviewRenderer.Capture(cam, Path.Combine(outDir, name + "_" + v + ".png"));
                }
                Object.DestroyImmediate(inst);
            }
        }
    }
}
