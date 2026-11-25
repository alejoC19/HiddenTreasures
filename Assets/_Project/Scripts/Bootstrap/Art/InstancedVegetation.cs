using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Dibuja decenas de miles de plantas y arboles con GPU Instancing (Graphics.RenderMeshInstanced), sin un GameObject por planta.
    /// Las instancias se agrupan en trozos de 40 m; cada trozo se descarta si esta fuera del frustum o mas lejos que la
    /// distancia maxima de su capa (la niebla ya oculta lo lejano). Se dibuja en beginCameraRendering, por camara, asi
    /// funciona igual en el juego y en las vistas previas del Editor.
    /// </summary>
    [ExecuteAlways]
    public sealed class InstancedVegetation : MonoBehaviour
    {
        public sealed class Layer
        {
            public string Name; public Mesh Mesh; public Material Material; public float MaxDistance; public bool CastShadows; public bool ReceiveShadows;
            /// <summary>Version simple de la malla para lejos (null = sin LOD) y desde que distancia se usa.</summary>
            public Mesh LodMesh; public float LodDistance;
            /// <summary>Las sombras solo se proyectan hasta esta distancia (0 = siempre que CastShadows).</summary>
            public float ShadowDistance;
            internal readonly Dictionary<Vector2Int, List<Matrix4x4>> Pending = new Dictionary<Vector2Int, List<Matrix4x4>>();
            internal readonly List<Page> Pages = new List<Page>();
            public int Count;
        }

        internal struct Page { public Matrix4x4[] Matrices; public Bounds Bounds; }

        const float ChunkSize = 40f;
        const int MaxPerCall = 1000;
        readonly List<Layer> _layers = new List<Layer>();
        readonly Plane[] _planes = new Plane[6];

        /// <summary>Escalas por calidad grafica (1 = completo).</summary>
        public float DistanceScale { get; set; } = 1f;
        public float LodScale { get; set; } = 1f;
        public float ShadowScale { get; set; } = 1f;

        public int InstanceCount { get; private set; }
        public int DrawnLastFrame { get; private set; }
        public int LayerCount => _layers.Count;

        public Layer AddLayer(string name, Mesh mesh, Material mat, float maxDistance, bool castShadows, bool receiveShadows = false)
        {
            mat.enableInstancing = true;
            var l = new Layer { Name = name, Mesh = mesh, Material = mat, MaxDistance = maxDistance, CastShadows = castShadows, ReceiveShadows = receiveShadows };
            _layers.Add(l);
            return l;
        }

        public void Add(Layer layer, Matrix4x4 m)
        {
            Vector3 p = m.GetColumn(3);
            var key = new Vector2Int(Mathf.FloorToInt(p.x / ChunkSize), Mathf.FloorToInt(p.z / ChunkSize));
            if (!layer.Pending.TryGetValue(key, out var list)) { list = new List<Matrix4x4>(); layer.Pending[key] = list; }
            list.Add(m);
            layer.Count++; InstanceCount++;
        }

        /// <summary>Cierra la carga: convierte las listas en paginas de hasta 1000 instancias con su caja de culling.</summary>
        public void Finish()
        {
            foreach (var l in _layers)
            {
                Vector3 ext = l.Mesh.bounds.extents;
                float reach = Mathf.Max(ext.x, Mathf.Max(ext.y, ext.z)) * 1.6f + 1f;
                foreach (var kv in l.Pending)
                {
                    var list = kv.Value;
                    for (int start = 0; start < list.Count; start += MaxPerCall)
                    {
                        int n = Mathf.Min(MaxPerCall, list.Count - start);
                        var arr = new Matrix4x4[n];
                        list.CopyTo(start, arr, 0, n);
                        var b = new Bounds((Vector3)arr[0].GetColumn(3), Vector3.zero);
                        for (int i = 1; i < n; i++) b.Encapsulate((Vector3)arr[i].GetColumn(3));
                        b.Expand(reach * 2f);
                        l.Pages.Add(new Page { Matrices = arr, Bounds = b });
                    }
                }
                l.Pending.Clear();
            }
        }

        void OnEnable() { RenderPipelineManager.beginCameraRendering += OnBeginCamera; }
        void OnDisable() { RenderPipelineManager.beginCameraRendering -= OnBeginCamera; }

        void OnBeginCamera(ScriptableRenderContext ctx, UnityEngine.Camera cam)
        {
            if (cam == null || (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView)) return;
            Draw(cam);
        }

        public void Draw(UnityEngine.Camera cam)
        {
            GeometryUtility.CalculateFrustumPlanes(cam, _planes);
            Vector3 cp = cam.transform.position;
            int drawn = 0;
            foreach (var l in _layers)
            {
                float maxD = l.MaxDistance * DistanceScale, maxSq = maxD * maxD;
                float lodD = l.LodDistance * LodScale, lodSq = lodD * lodD;
                float shD = l.ShadowDistance * ShadowScale, shSq = shD * shD;
                var rp = new RenderParams(l.Material)
                {
                    camera = cam,
                    receiveShadows = l.ReceiveShadows,
                    lightProbeUsage = LightProbeUsage.Off,
                    reflectionProbeUsage = ReflectionProbeUsage.Off,
                    motionVectorMode = MotionVectorGenerationMode.ForceNoMotion,
                };
                foreach (var page in l.Pages)
                {
                    float d2 = page.Bounds.SqrDistance(cp);
                    if (d2 > maxSq) continue;
                    if (!GeometryUtility.TestPlanesAABB(_planes, page.Bounds)) continue;
                    rp.worldBounds = page.Bounds;
                    rp.shadowCastingMode = l.CastShadows && (l.ShadowDistance <= 0f || d2 <= shSq) ? ShadowCastingMode.On : ShadowCastingMode.Off;
                    var mesh = l.LodMesh != null && d2 > lodSq ? l.LodMesh : l.Mesh;
                    Graphics.RenderMeshInstanced(rp, mesh, 0, page.Matrices);
                    drawn += page.Matrices.Length;
                }
            }
            DrawnLastFrame = drawn;
        }
    }
}
