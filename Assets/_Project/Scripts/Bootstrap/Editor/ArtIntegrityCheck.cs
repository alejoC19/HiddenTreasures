using System.Text;
using UnityEditor;
using UnityEngine;

namespace HuesoOxido.Core.EditorTools
{
    /// <summary>
    /// Comprueba en Unity real que cada asset de Resources/Art/World carga completo: manifiesto, mallas de todos los LOD, materiales y
    /// texturas referenciadas. Lote: -executeMethod HuesoOxido.Core.EditorTools.ArtIntegrityCheck.RunBatch. Menu: Hidden Treasures/Comprobar arte.
    /// </summary>
    public static class ArtIntegrityCheck
    {
        [MenuItem("Hidden Treasures/Comprobar arte")]
        static void FromMenu() { bool ok = Run(out string r); Debug.Log("[ARTCHECK] " + (ok ? "OK" : "FALLOS") + "\n" + r); EditorUtility.DisplayDialog("Comprobar arte", ok ? "Todo el arte carga bien." : "Hay problemas: ver la consola.", "OK"); }

        public static void RunBatch() { bool ok = Run(out string r); Debug.Log("[ARTCHECK] " + (ok ? "OK" : "FALLOS") + "\n" + r); EditorApplication.Exit(ok ? 0 : 1); }

        static bool Run(out string report)
        {
            var sb = new StringBuilder(); bool ok = true; int assets = 0, problems = 0;
            foreach (var ta in Resources.LoadAll<TextAsset>("Art/World"))
            {
                assets++;
                var m = ArtLibrary.Get(ta.name);
                if (m == null) { sb.AppendLine("FALLO " + ta.name + ": no carga"); ok = false; problems++; continue; }
                for (int lod = 0; lod < m.LodCount; lod++)
                {
                    int meshes = 0;
                    for (int i = 0; i < m.Meshes[lod].Length; i++)
                    {
                        if (m.Meshes[lod][i] == null) { sb.AppendLine("FALLO " + ta.name + " LOD" + lod + ": falta la malla del material " + i); ok = false; problems++; }
                        else if (m.Meshes[lod][i].vertexCount == 0) { sb.AppendLine("FALLO " + ta.name + " LOD" + lod + ": malla vacia " + i); ok = false; problems++; }
                        else meshes++;
                    }
                    if (meshes == 0) { sb.AppendLine("FALLO " + ta.name + " LOD" + lod + ": sin mallas"); ok = false; problems++; }
                }
                if (m.LodCount > 1 && m.Triangles(m.LodCount - 1) >= m.Triangles(0) && m.Triangles(0) > 400)
                    sb.AppendLine("AVISO " + ta.name + ": el ultimo LOD no reduce triangulos (" + m.Triangles(m.LodCount - 1) + " vs " + m.Triangles(0) + ")");
                foreach (var mat in m.Materials)
                {
                    if (mat == null || mat.shader == null || mat.shader.name.Contains("Hidden/InternalErrorShader")) { sb.AppendLine("FALLO " + ta.name + ": material o shader invalido"); ok = false; problems++; continue; }
                    bool hasTex = mat.mainTexture != null;
                    if (!hasTex && mat.color.maxColorComponent > 0.001f && ta.name != "detector") sb.AppendLine("AVISO " + ta.name + "/" + mat.name + ": sin textura de color (color plano)");
                }
                if (m.Size.y < 0.02f || m.Size.y > 40f) sb.AppendLine("AVISO " + ta.name + ": tamano inusual " + m.Size.ToString("F2"));
            }
            sb.Insert(0, "Assets revisados: " + assets + ", problemas: " + problems + "\n");
            report = sb.ToString();
            return ok && assets > 0;
        }
    }
}
