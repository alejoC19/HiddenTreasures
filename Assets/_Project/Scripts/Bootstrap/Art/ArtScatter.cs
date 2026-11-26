using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>Un modelo de arte listo para instanciar por GPU: una capa por material, con su malla de LOD lejano.</summary>
    public sealed class ArtKit
    {
        public ArtModel Model;
        public InstancedVegetation.Layer[] Layers;
        /// <summary>Radio aproximado del tronco, para el colisionador.</summary>
        public float TrunkRadius;
    }

    /// <summary>Puente entre los modelos reales (ArtLibrary) y los sistemas existentes: instanciado por GPU y objetos sueltos con LODGroup.</summary>
    public static class ArtScatter
    {
        public static ArtKit AddKit(InstancedVegetation veg, string tag, string asset, Color? tint, float maxDist, bool shadows, float lodDist, float shadowDist, float smoothness = 1f)
        {
            var model = ArtLibrary.Get(asset, tint, smoothness);
            if (model == null) return null;
            var layers = new List<InstancedVegetation.Layer>();
            for (int i = 0; i < model.Materials.Length; i++)
            {
                var mesh = model.Meshes[0][i]; if (mesh == null) continue;
                var l = veg.AddLayer(tag + "_" + asset + "_" + i, mesh, model.Materials[i], maxDist, shadows, true);
                if (model.LodCount > 1 && model.Meshes[1][i] != null) { l.LodMesh = model.Meshes[1][i]; l.LodDistance = lodDist; }
                l.ShadowDistance = shadowDist;
                layers.Add(l);
            }
            return new ArtKit { Model = model, Layers = layers.ToArray(), TrunkRadius = Mathf.Clamp(Mathf.Min(model.Size.x, model.Size.z) * 0.025f, 0.2f, 0.6f) };
        }

        public static void Place(InstancedVegetation veg, ArtKit k, Matrix4x4 m) { foreach (var l in k.Layers) veg.Add(l, m); }

        /// <summary>Objeto suelto (rocas grandes, ruinas): LOD0 cerca, LOD1 lejos, colision con la malla simple.</summary>
        public static GameObject Spawn(ArtModel model, Transform parent, Vector3 pos, Quaternion rot, float scale, bool collider = true, bool staticBatch = false)
        {
            var root = new GameObject("Art_" + model.Name);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, rot);
            root.transform.localScale = Vector3.one * scale;
            var lodRenderers = new List<Renderer[]>();
            int lodCount = Mathf.Min(model.LodCount, 2);
            for (int lod = 0; lod < lodCount; lod++)
            {
                var holder = new GameObject("LOD" + lod).transform; holder.SetParent(root.transform, false);
                var rs = new List<Renderer>();
                for (int i = 0; i < model.Materials.Length; i++)
                {
                    var mesh = model.Meshes[lod][i]; if (mesh == null) continue;
                    var g = new GameObject("m" + i); g.transform.SetParent(holder, false);
                    g.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = model.Materials[i]; rs.Add(r);
                }
                lodRenderers.Add(rs.ToArray());
            }
            if (lodCount > 1)
            {
                var group = root.AddComponent<LODGroup>();
                var lods = new LOD[lodCount];
                float[] heights = { 0.30f, 0.01f };
                for (int i = 0; i < lodCount; i++) lods[i] = new LOD(heights[i], lodRenderers[i]);
                group.SetLODs(lods); group.RecalculateBounds();
            }
            if (collider)
            {
                // Malla simple (LOD1 si existe): las ruinas se pueden recorrer y los arcos se atraviesan.
                var cm = model.Meshes[Mathf.Min(1, model.LodCount - 1)];
                for (int i = 0; i < cm.Length; i++)
                {
                    if (cm[i] == null) continue;
                    var cg = new GameObject("col" + i); cg.transform.SetParent(root.transform, false);
                    cg.AddComponent<MeshCollider>().sharedMesh = cm[i];
                }
            }
            root.isStatic = true;
            return root;
        }
    }
}
