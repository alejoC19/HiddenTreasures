using System.Collections.Generic;
using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Vehicle
{
    /// <summary>
    /// Camioneta 4x4 de expedicion, vieja (estilo Land Rover/Jeep de los 70): capot largo, cabina cuadrada con ventanillas,
    /// guardabarros, parrilla con faros redondos, defensa con malacate, estribos, rueda de repuesto, parrilla de techo con
    /// equipaje y bidon. Todo armado con primitivas y materiales PBR pintados por codigo (pintura gastada, caucho, acero).
    /// </summary>
    public static class PlaceholderVehicle
    {
        public static float WheelRadius = 0.38f;
        public const string ModelPath = "Art/Models/WillysJeep";

        /// <summary>Crea el cuerpo y devuelve los 4 pivotes de rueda (delanteras primero). Agrega el BoxCollider.</summary>
        public static Transform[] Build(Transform root, Vector3[] mounts)
        {
            var pickup = BuildFromPickup(root, mounts);
            if (pickup != null) return pickup;
            var real = BuildFromModel(root, mounts);
            if (real != null) return real;
            return BuildProcedural(root, mounts);
        }

        public const string PickupPath = "Art/Models/Silverado";
        const float GroundBelowRoot = -0.402f;     // en reposo el suelo queda ~0,40 m bajo el origen del vehiculo (suspension)
        const float PickupLength = 5.75f;          // largo real de una pickup de cabina doble (m)

        static Bounds RootBounds(Transform root, IEnumerable<MeshFilter> filters)
        {
            bool first = true; var b = new Bounds();
            var toRoot = root.worldToLocalMatrix;
            foreach (var f in filters)
            {
                if (f == null || f.sharedMesh == null) continue;
                var m = toRoot * f.transform.localToWorldMatrix;
                var mb = f.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = m.MultiplyPoint3x4(c);
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
                }
            }
            return b;
        }

        /// <summary>
        /// Pickup moderna (Resources/Art/Models/Silverado.glb, CC-BY, ver CREDITS_ASSETS.md): se orienta con las marcas MARK_FRONT/MARK_REAR,
        /// se escala a su largo real, se apoya sobre las llantas y cada rueda (llanta + rin) cuelga de un pivote que mueve la fisica.
        /// Devuelve null si no hay modelo o no se reconocen las 4 ruedas.
        /// </summary>
        static Transform[] BuildFromPickup(Transform root, Vector3[] mounts)
        {
            var prefab = Resources.Load<GameObject>(PickupPath);
            if (prefab == null) { Debug.LogWarning("[Vehiculo] No se pudo cargar " + PickupPath + ": se usa el respaldo."); return null; }
            var model = Object.Instantiate(prefab, root);
            model.name = "PickupModel";
            model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity; model.transform.localScale = Vector3.one;

            Transform mFront = null, mRear = null;
            var wheelFilters = new List<MeshFilter>(); var bodyFilters = new List<MeshFilter>();
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.StartsWith("MARK_")) { var mr = mf.GetComponent<Renderer>(); if (mr != null) mr.enabled = false; continue; }
                bool wheel = false;
                for (var t = mf.transform; t != null && t != model.transform; t = t.parent) if (t.name.StartsWith("WHEEL_")) { wheel = true; break; }
                (wheel ? wheelFilters : bodyFilters).Add(mf);
            }
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) { if (t.name == "MARK_FRONT") mFront = t; else if (t.name == "MARK_REAR") mRear = t; }
            if (mFront == null || mRear == null || wheelFilters.Count < 8)
            {
                Debug.LogWarning("[Vehiculo] Silverado no reconocida (marcas " + (mFront != null) + "/" + (mRear != null) + ", partes de rueda " + wheelFilters.Count + "): se usa el respaldo.");
                MaterialUtil.Dispose(model); return null;
            }

            GltfMaterials.ConvertToLit(model);

            // 1) orientacion: el frente (puerta delantera) debe quedar hacia +Z
            Vector3 f = root.InverseTransformDirection(mFront.position - mRear.position); f.y = 0f;
            if (f.sqrMagnitude < 1e-12f) { MaterialUtil.Dispose(model); return null; }
            model.transform.localRotation = Quaternion.FromToRotation(f.normalized, Vector3.forward);
            // 2) escala real
            float len = RootBounds(root, bodyFilters).size.z;
            float k = len > 1e-4f ? PickupLength / len : 1f;
            model.transform.localScale = Vector3.one * k;

            // 3) agrupar las partes de rueda por esquina y medir
            var wheelCenters = new List<(MeshFilter f, Vector3 c, Vector3 size)>();
            foreach (var wf in wheelFilters) { var b = RootBounds(root, new[] { wf }); wheelCenters.Add((wf, b.center, b.size)); }
            Vector3 mid = Vector3.zero; foreach (var w in wheelCenters) mid += w.c; mid /= wheelCenters.Count;
            float groundY = float.MaxValue, radius = 0f;
            foreach (var w in wheelCenters) { groundY = Mathf.Min(groundY, w.c.y - w.size.y * 0.5f); radius = Mathf.Max(radius, w.size.y * 0.5f); }
            // 4) centrar entre ejes y apoyar las llantas en el suelo de reposo
            model.transform.localPosition = new Vector3(-mid.x, GroundBelowRoot - groundY, -mid.z);

            var groups = new List<(MeshFilter f, Vector3 c)>[4];
            for (int i = 0; i < 4; i++) groups[i] = new List<(MeshFilter, Vector3)>();
            foreach (var wf in wheelFilters)
            {
                var c = RootBounds(root, new[] { wf }).center;
                int corner = (c.z > 0f ? 0 : 2) + (c.x > 0f ? 1 : 0);          // 0 FL, 1 FR, 2 RL, 3 RR
                groups[corner].Add((wf, c));
            }
            var pivots = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                if (groups[i].Count == 0) { MaterialUtil.Dispose(model); return null; }
                Vector3 avg = Vector3.zero; foreach (var g in groups[i]) avg += g.c; avg /= groups[i].Count;
                var pivot = new GameObject("Wheel" + i).transform;
                pivot.SetParent(root, false);
                pivot.localPosition = avg;
                foreach (var g in groups[i]) { var host = g.f.transform; while (host.parent != null && host.parent != model.transform && !host.name.StartsWith("WHEEL_")) host = host.parent; host.SetParent(pivot, true); }
                pivots[i] = pivot;
                mounts[i] = new Vector3(avg.x, 0.35f, avg.z);
            }
            WheelRadius = Mathf.Max(0.25f, radius);

            var body = RootBounds(root, bodyFilters);
            var box = root.gameObject.AddComponent<BoxCollider>();
            float bottom = GroundBelowRoot + 0.55f, top = Mathf.Max(bottom + 0.8f, body.max.y - 0.1f);
            box.center = new Vector3(0f, (bottom + top) * 0.5f, 0f);
            box.size = new Vector3(Mathf.Min(body.size.x * 0.92f, 2.1f), top - bottom, Mathf.Min(body.size.z * 0.94f, PickupLength));
            return pivots;
        }

        // Medidas del Willys Jeep (malla en centimetros * 0,01): ruedas halladas por analisis de vertices.
        static readonly Vector3[] WheelCenters =
        {
            new Vector3(-0.76f, 0.39f, 1.425f), new Vector3(0.88f, 0.39f, 1.425f),
            new Vector3(-0.76f, 0.39f, -1.18f), new Vector3(0.88f, 0.39f, -1.18f),
        };
        const float WheelCatchRadius = 0.41f, WheelCatchHalfWidth = 0.22f;

        /// <summary>
        /// Usa el modelo 3D real (Resources/Art/Models/WillysJeep.fbx, jeep clasico) si existe: separa las 4 ruedas de la malla
        /// y las cuelga de pivotes que mueve la fisica del vehiculo. Devuelve null si no hay modelo.
        /// </summary>
        static Transform[] BuildFromModel(Transform root, Vector3[] mounts)
        {
            var prefab = Resources.Load<GameObject>(ModelPath);
            if (prefab == null) return null;
            var srcFilter = prefab.GetComponentInChildren<MeshFilter>();
            if (srcFilter == null || srcFilter.sharedMesh == null || !srcFilter.sharedMesh.isReadable) return null;
            var mesh = srcFilter.sharedMesh;

            // Origen: suelo a 0,40 m bajo el origen del vehiculo, eje de ruedas centrado, via centrada.
            Vector3 shift = new Vector3(-0.06f, -0.40f, -0.12f);
            var toLocal = Matrix4x4.Translate(shift) * Matrix4x4.Scale(Vector3.one * 100f) ;
            Vector3[] centers = new Vector3[4];
            for (int i = 0; i < 4; i++) centers[i] = WheelCenters[i] + shift;

            var parts = MeshSplitter.Split(mesh, toLocal, c =>
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 d = c - centers[i];
                    if (Mathf.Abs(d.x) < WheelCatchHalfWidth && new Vector2(d.y, d.z).magnitude < WheelCatchRadius) return i;
                }
                return -1;
            }, centers, 4);

            var mats = BuildJeepMaterials(srcFilter.GetComponent<MeshRenderer>() != null ? srcFilter.GetComponent<MeshRenderer>().sharedMaterials.Length : mesh.subMeshCount);
            var model = new GameObject("CarModel").transform;
            model.SetParent(root, false);
            AddMeshObject(model, "Body", parts[0], mats, Vector3.zero);

            var pivots = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var pivot = new GameObject("Wheel" + i).transform;
                pivot.SetParent(root, false);
                pivot.localPosition = centers[i];
                AddMeshObject(pivot, "Tire", parts[i + 1], mats, Vector3.zero);
                pivots[i] = pivot;
                mounts[i] = new Vector3(centers[i].x, 0.35f, centers[i].z);
            }
            WheelRadius = 0.39f;

            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.55f, 0f);
            box.size = new Vector3(1.8f, 1.0f, 4.0f);
            return pivots;
        }

        static void AddMeshObject(Transform parent, string name, Mesh mesh, Material[] mats, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        static Material[] BuildJeepMaterials(int count)
        {
            var diff = Resources.Load<Texture2D>("Art/Models/willys_jeep_diff");
            var nor = Resources.Load<Texture2D>("Art/Models/willys_jeep_nor");
            var ao = Resources.Load<Texture2D>("Art/Models/willys_jeep_ao_arm");
            var m = MaterialUtil.LitMaterialTemplate();
            m.name = "WillysJeep";
            if (diff != null) { m.SetTexture("_BaseMap", diff); m.SetTexture("_MainTex", diff); }
            if (nor != null) { m.SetTexture("_BumpMap", nor); m.EnableKeyword("_NORMALMAP"); }
            if (ao != null) { m.SetTexture("_OcclusionMap", ao); m.EnableKeyword("_OCCLUSIONMAP"); }
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.3f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.25f);
            var arr = new Material[count];
            for (int i = 0; i < count; i++) arr[i] = m;
            return arr;
        }

        static Transform[] BuildProcedural(Transform root, Vector3[] mounts)
        {
            var paint = ProceduralSurfaces.WornPaint("truck_olive", new Color(0.36f, 0.42f, 0.27f), 0.55f);
            var paintRoof = ProceduralSurfaces.WornPaint("truck_cream", new Color(0.78f, 0.74f, 0.62f), 0.35f);
            var black = ProceduralSurfaces.Steel("truck_black", new Color(0.09f, 0.09f, 0.1f), 0.3f, 0.4f);
            var steel = ProceduralSurfaces.Steel("truck_steel", new Color(0.62f, 0.63f, 0.65f), 0.55f, 0.85f);
            var rubber = ProceduralSurfaces.Rubber();
            var glass = ProceduralSurfaces.Glass();
            var canvas = ProceduralSurfaces.Fabric("truck_canvas", new Color(0.55f, 0.48f, 0.33f), 40f);
            var leather = ProceduralSurfaces.Leather("truck_case", new Color(0.4f, 0.26f, 0.14f));
            var jerry = ProceduralSurfaces.WornPaint("truck_jerry", new Color(0.55f, 0.18f, 0.12f), 0.6f);
            var lensWarm = MaterialUtil.Lit(new Color(1f, 0.93f, 0.72f), 0.95f, 0f);
            var lensRed = MaterialUtil.Lit(new Color(0.7f, 0.06f, 0.05f), 0.9f, 0f);

            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.7f, 0f);
            box.size = new Vector3(1.95f, 1.2f, 4.1f);

            // Chasis oscuro y bajos.
            Cube("Underbody", root, black, new Vector3(0f, 0.3f, 0f), new Vector3(1.5f, 0.18f, 3.9f));
            // Cuerpo inferior (puertas y paneles) y cola.
            Cube("LowerBody", root, paint, new Vector3(0f, 0.66f, -0.1f), new Vector3(1.8f, 0.52f, 3.2f));
            Cube("Tailgate", root, paint, new Vector3(0f, 0.7f, -1.78f), new Vector3(1.78f, 0.6f, 0.08f));
            // Capot largo y ligeramente inclinado + cubierta de aleta.
            var hood = Cube("Hood", root, paint, new Vector3(0f, 0.97f, 1.32f), new Vector3(1.62f, 0.1f, 1.15f));
            hood.transform.localRotation = Quaternion.Euler(-3f, 0f, 0f);
            Cube("FrontPanel", root, paint, new Vector3(0f, 0.7f, 1.88f), new Vector3(1.7f, 0.56f, 0.1f));
            Cube("HoodBulgeL", root, paint, new Vector3(-0.35f, 1.03f, 1.32f), new Vector3(0.38f, 0.04f, 1.0f));
            // Cabina (parabrisas, laterales, techo).
            Cube("Cabin", root, paint, new Vector3(0f, 1.2f, -0.45f), new Vector3(1.7f, 0.62f, 1.75f));
            Cube("Roof", root, paintRoof, new Vector3(0f, 1.54f, -0.42f), new Vector3(1.76f, 0.06f, 1.9f));
            var ws = Cube("Windshield", root, glass, new Vector3(0f, 1.25f, 0.43f), new Vector3(1.5f, 0.5f, 0.04f));
            ws.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            Cube("WindshieldFrame", root, black, new Vector3(0f, 1.25f, 0.405f), new Vector3(1.6f, 0.58f, 0.03f)).transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            Cube("RearGlass", root, glass, new Vector3(0f, 1.25f, -1.33f), new Vector3(1.4f, 0.4f, 0.04f));
            for (int s = -1; s <= 1; s += 2)
            {
                Cube("SideGlassF", root, glass, new Vector3(s * 0.86f, 1.26f, 0.0f), new Vector3(0.04f, 0.42f, 0.7f));
                Cube("SideGlassR", root, glass, new Vector3(s * 0.86f, 1.26f, -0.85f), new Vector3(0.04f, 0.42f, 0.75f));
                Cube("DoorLine", root, black, new Vector3(s * 0.905f, 0.78f, -0.4f), new Vector3(0.01f, 0.5f, 0.02f));
                Cube("DoorHandle", root, steel, new Vector3(s * 0.915f, 0.95f, 0.08f), new Vector3(0.03f, 0.03f, 0.14f));
                // Estribos.
                Cube("RunningBoard", root, black, new Vector3(s * 0.99f, 0.36f, -0.1f), new Vector3(0.24f, 0.04f, 1.8f));
                // Espejos.
                Cube("MirrorArm", root, black, new Vector3(s * 0.98f, 1.18f, 0.5f), new Vector3(0.12f, 0.025f, 0.025f));
                Cube("Mirror", root, steel, new Vector3(s * 1.06f, 1.2f, 0.5f), new Vector3(0.05f, 0.18f, 0.13f));
                // Aletas (guardabarros) delanteras y traseras sobre las ruedas.
                Cube("FenderF", root, paint, new Vector3(s * 0.9f, 0.88f, 1.35f), new Vector3(0.26f, 0.09f, 1.0f));
                Cube("FenderR", root, paint, new Vector3(s * 0.9f, 0.88f, -1.3f), new Vector3(0.26f, 0.09f, 1.0f));
                Cube("FlareF", root, black, new Vector3(s * 0.99f, 0.8f, 1.35f), new Vector3(0.1f, 0.08f, 1.0f));
                Cube("FlareR", root, black, new Vector3(s * 0.99f, 0.8f, -1.3f), new Vector3(0.1f, 0.08f, 1.0f));
                // Faros redondos con aro cromado.
                Cyl("HeadRim", root, steel, new Vector3(s * 0.58f, 0.86f, 1.95f), new Vector3(0.3f, 0.02f, 0.3f), 90f);
                Cyl("Headlight", root, lensWarm, new Vector3(s * 0.58f, 0.86f, 1.965f), new Vector3(0.24f, 0.015f, 0.24f), 90f);
                // Luces traseras y direccionales.
                Cube("TailLight", root, lensRed, new Vector3(s * 0.72f, 0.82f, -1.84f), new Vector3(0.14f, 0.2f, 0.04f));
                Cyl("Blinker", root, lensRed, new Vector3(s * 0.84f, 0.84f, 1.94f), new Vector3(0.09f, 0.02f, 0.09f), 90f);
            }
            // Parrilla frontal con barras.
            Cube("Grille", root, black, new Vector3(0f, 0.78f, 1.945f), new Vector3(0.7f, 0.34f, 0.04f));
            for (int i = 0; i < 4; i++) Cube("GrilleBar", root, steel, new Vector3(0f, 0.65f + i * 0.09f, 1.97f), new Vector3(0.66f, 0.018f, 0.02f));
            // Defensa de acero y malacate.
            Cube("Bumper", root, steel, new Vector3(0f, 0.46f, 2.0f), new Vector3(1.9f, 0.16f, 0.16f));
            Cube("BumperBack", root, steel, new Vector3(0f, 0.5f, -2.0f), new Vector3(1.85f, 0.15f, 0.14f));
            Cyl("WinchDrum", root, black, new Vector3(0f, 0.5f, 2.06f), new Vector3(0.14f, 0.28f, 0.14f), 0f, 90f);
            Cube("Winch", root, black, new Vector3(0f, 0.52f, 2.0f), new Vector3(0.36f, 0.14f, 0.12f));
            // Rueda de repuesto en la puerta trasera.
            Cyl("SpareTire", root, rubber, new Vector3(0f, 0.95f, -2.08f), new Vector3(WheelRadius * 2f, 0.12f, WheelRadius * 2f), 90f);
            Cyl("SpareHub", root, steel, new Vector3(0f, 0.95f, -2.15f), new Vector3(0.36f, 0.02f, 0.36f), 90f);
            // Snorkel en el lateral derecho.
            Cyl("Snorkel", root, black, new Vector3(0.93f, 1.15f, 0.55f), new Vector3(0.07f, 0.55f, 0.07f));
            Cyl("SnorkelTop", root, black, new Vector3(0.93f, 1.72f, 0.5f), new Vector3(0.09f, 0.05f, 0.09f), 0f, 90f);
            // Parrilla de techo con equipaje, tabla y bidon.
            for (int i = 0; i < 4; i++) Cube("RackBar", root, black, new Vector3(0f, 1.62f, -1.2f + i * 0.55f), new Vector3(1.6f, 0.03f, 0.04f));
            Cube("RackRailL", root, black, new Vector3(-0.8f, 1.6f, -0.45f), new Vector3(0.03f, 0.05f, 1.9f));
            Cube("RackRailR", root, black, new Vector3(0.8f, 1.6f, -0.45f), new Vector3(0.03f, 0.05f, 1.9f));
            Cube("LuggageRoll", root, canvas, new Vector3(0.0f, 1.8f, -0.9f), new Vector3(1.3f, 0.3f, 0.55f));
            Cube("StrapA", root, leather, new Vector3(-0.35f, 1.8f, -0.9f), new Vector3(0.05f, 0.33f, 0.58f));
            Cube("StrapB", root, leather, new Vector3(0.35f, 1.8f, -0.9f), new Vector3(0.05f, 0.33f, 0.58f));
            Cube("Case", root, leather, new Vector3(0.0f, 1.76f, 0.05f), new Vector3(0.7f, 0.22f, 0.45f));
            Cube("Jerrycan", root, jerry, new Vector3(-0.55f, 1.8f, 0.05f), new Vector3(0.18f, 0.32f, 0.38f));
            Cube("JerrycanCap", root, black, new Vector3(-0.55f, 1.99f, 0.05f), new Vector3(0.08f, 0.06f, 0.08f));
            // Escape trasero.
            Cyl("Exhaust", root, black, new Vector3(-0.55f, 0.3f, -2.05f), new Vector3(0.07f, 0.1f, 0.07f), 90f);

            var wheels = new Transform[mounts.Length];
            for (int i = 0; i < mounts.Length; i++)
            {
                var pivot = new GameObject("Wheel" + i).transform;
                pivot.SetParent(root, false);
                pivot.localPosition = mounts[i] + Vector3.down * 0.45f;
                float side = mounts[i].x < 0f ? -1f : 1f;
                Cyl("Tire", pivot, rubber, Vector3.zero, new Vector3(WheelRadius * 2f, 0.14f, WheelRadius * 2f), 0f, 90f);
                Cyl("Rim", pivot, steel, new Vector3(side * 0.075f, 0f, 0f), new Vector3(0.44f, 0.012f, 0.44f), 0f, 90f);
                Cyl("Hub", pivot, black, new Vector3(side * 0.09f, 0f, 0f), new Vector3(0.16f, 0.012f, 0.16f), 0f, 90f);
                for (int b = 0; b < 5; b++)
                {
                    float a = b * 72f * Mathf.Deg2Rad;
                    Cyl("Lug", pivot, steel, new Vector3(side * 0.097f, Mathf.Cos(a) * 0.1f, Mathf.Sin(a) * 0.1f), new Vector3(0.03f, 0.01f, 0.03f), 0f, 90f);
                }
                wheels[i] = pivot;
            }
            return wheels;
        }

        static GameObject Cube(string name, Transform parent, Material m, Vector3 pos, Vector3 scale)
        {
            var go = MaterialUtil.Primitive(PrimitiveType.Cube, name, parent, m);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return go;
        }

        /// <summary>Cilindro. <paramref name="rotX"/> lo tumba hacia Z (faros, rueda de repuesto); <paramref name="rotZ"/> hacia X (ruedas).</summary>
        static GameObject Cyl(string name, Transform parent, Material m, Vector3 pos, Vector3 diamHeightDiam, float rotX = 0f, float rotZ = 0f)
        {
            var go = MaterialUtil.Primitive(PrimitiveType.Cylinder, name, parent, m);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(rotX, 0f, rotZ);
            // El cilindro de Unity mide 2 de alto: la altura se escala a la mitad.
            go.transform.localScale = new Vector3(diamHeightDiam.x, diamHeightDiam.y, diamHeightDiam.z);
            return go;
        }
    }
}
