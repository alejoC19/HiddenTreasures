using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Modelos 3D reales (props CC0 de Poly Haven) cargados desde Resources/Art/Props/&lt;id&gt;/.
    /// Escala el modelo a su tamano real (dato de info.json) sin depender de las unidades del FBX y lo apoya sobre el suelo.
    /// Si el modelo no existe devuelve null y el llamador conserva su respaldo.
    /// </summary>
    public static class PropLibrary
    {
        [System.Serializable] sealed class Info { public float[] dimensions_mm; }

        public static GameObject Spawn(string id, Transform parent, Vector3 localPos, float yaw, float scaleMul = 1f, bool collider = true, float smoothness = 0.2f)
        {
            var prefab = Resources.Load<GameObject>("Art/Props/" + id + "/" + id + "_1k");
            if (prefab == null) return null;

            var root = new GameObject("Prop_" + id);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var model = Object.Instantiate(prefab, root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // los FBX de Poly Haven vienen con Z hacia arriba

            string tex = "Art/Props/" + id + "/textures/" + id;
            var mat = ArtMaterials.TexturedPath(tex + "_diff_1k", tex + "_nor_gl_1k", Color.white, smoothness);
            var renderers = model.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
            }

            float targetMax = 1f;
            var ta = Resources.Load<TextAsset>("Art/Props/" + id + "/info");
            if (ta != null) { var info = JsonUtility.FromJson<Info>(ta.text); if (info != null && info.dimensions_mm != null && info.dimensions_mm.Length >= 3) targetMax = Mathf.Max(info.dimensions_mm) / 1000f; }

            Bounds b = Measure(model);
            float cur = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (cur > 1e-6f) model.transform.localScale = model.transform.localScale * (targetMax / cur * scaleMul);
            b = Measure(model);
            model.transform.position += root.transform.position - new Vector3(b.center.x, b.min.y, b.center.z);
            b = Measure(model);
            if (!Application.isPlaying) Debug.Log("[PROP] " + id + " tamano=" + b.size.ToString("F2") + " objetivo_max=" + targetMax.ToString("F2"));
            if (collider)
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = root.transform.InverseTransformPoint(b.center);
                box.size = new Vector3(b.size.x, b.size.y, b.size.z);
            }
            return root;
        }

        /// <summary>Caja envolvente en mundo calculada desde las mallas y las matrices (no usa la cache de Renderer.bounds).</summary>
        static Bounds Measure(GameObject go)
        {
            bool any = false; var b = new Bounds();
            void Add(Mesh m, Transform t)
            {
                if (m == null) return;
                var mb = m.bounds; var c = mb.center; var e = mb.extents;
                for (int i = 0; i < 8; i++)
                {
                    var corner = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                    var w = t.localToWorldMatrix.MultiplyPoint3x4(corner);
                    if (!any) { b = new Bounds(w, Vector3.zero); any = true; } else b.Encapsulate(w);
                }
            }
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>()) Add(mf.sharedMesh, mf.transform);
            foreach (var sm in go.GetComponentsInChildren<SkinnedMeshRenderer>()) Add(sm.sharedMesh, sm.transform);
            return b;
        }
    }
}
