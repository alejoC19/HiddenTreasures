using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using HuesoOxido.Player;
using HuesoOxido.Survival;
using HuesoOxido.Vehicle;

namespace HuesoOxido.Core.EditorTools
{
    /// <summary>
    /// Genera imagenes del mundo REAL del juego sin entrar en Play Mode: construye el mismo mundo con el mismo codigo
    /// (WorldBuilder, campamento, personaje, vehiculo, iluminacion y post-proceso) en modo edicion y lo renderiza.
    /// NO es gameplay: no hay simulacion, movimiento ni UI interactiva. Sirve para revisar composicion, luz y entorno.
    /// Menu: Hidden Treasures/Generar vistas previas  ·  Batch: -executeMethod HuesoOxido.Core.EditorTools.PreviewRenderer.RunBatch
    /// (variable HT_PREVIEW_DIR = carpeta de salida)
    /// </summary>
    public static class PreviewRenderer
    {
        const int W = 1600, H = 900;

        static readonly Vector3[] VehicleMounts =
        {
            new Vector3(-0.85f, 0.35f, 1.35f), new Vector3(0.85f, 0.35f, 1.35f),
            new Vector3(-0.85f, 0.35f, -1.35f), new Vector3(0.85f, 0.35f, -1.35f),
        };

        sealed class Stage
        {
            public WorldData World; public UnityEngine.Camera Cam; public DayNightCycle Day;
            public GameClock Clock; public WeatherModel Weather; public GameObject Player, Vehicle; public AtmosphereFx Atmo;
            public ReflectionBaker Refl; public PlaceholderCharacterAnimator Anim; public PlaceholderCharacter Char; public Transform Det;
        }

        [MenuItem("Hidden Treasures/Generar vistas previas")]
        static void RunFromMenu()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Previews");
            Generate(dir);
            EditorUtility.RevealInFinder(dir);
        }

        public static void RunBatch()
        {
            try
            {
                string dir = Environment.GetEnvironmentVariable("HT_PREVIEW_DIR") ?? Path.Combine(Directory.GetCurrentDirectory(), "Previews");
                Generate(dir);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError("[PREVIEW] fallo: " + e);
                EditorApplication.Exit(1);
            }
        }

        static void Generate(string dir)
        {
            Directory.CreateDirectory(dir);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var st = BuildStage();
            Debug.Log("[PREVIEW] mundo construido en " + sw.ElapsedMilliseconds + " ms (Mono del Editor, sin GPU)");
            File.WriteAllBytes(Path.Combine(dir, "00_mapa.png"), st.World.MapTexture.EncodeToPNG());

            var camp = st.World.CampPosition; var ruins = st.World.RuinsCenter;
            // El primer cuadro tras construir en modo edicion sale sin luz ambiente calculada: se descarta.
            st.Cam.transform.position = camp + Vector3.up * 3f; st.Day.ApplyNow(); DynamicGI.UpdateEnvironment();
            Capture(st.Cam, Path.Combine(dir, "_warmup.png")); File.Delete(Path.Combine(dir, "_warmup.png"));
            string only = System.Environment.GetEnvironmentVariable("HT_PREVIEW_ONLY");
            bool onlyChar = only == "char", onlyForest = only == "forest", onlyVeh = only == "vehicle";
            if (onlyVeh)
            {
                var vt = st.Vehicle.transform;
                Shot(st, dir, "30_camioneta_frente", 17.3f, WeatherKind.Clear, vt.TransformPoint(new Vector3(-4.8f, 1.5f, 6.2f)), vt.position + Vector3.up * 0.9f, 48f);
                Shot(st, dir, "31_camioneta_lado", 17.3f, WeatherKind.Clear, vt.TransformPoint(new Vector3(-7.5f, 1.4f, 0.3f)), vt.position + Vector3.up * 0.9f, 42f);
                Shot(st, dir, "32_camioneta_atras", 17.0f, WeatherKind.Clear, vt.TransformPoint(new Vector3(4.5f, 1.8f, -7f)), vt.position + Vector3.up * 1.0f, 48f);
                Shot(st, dir, "33_camioneta_ruedas", 17.3f, WeatherKind.Clear, vt.TransformPoint(new Vector3(-3.2f, 0.7f, 2.6f)), vt.TransformPoint(new Vector3(-1f, 0.45f, 1.6f)), 50f);
            }
            if (!onlyChar && !onlyForest && !onlyVeh && only != "biome")
            {
            Shot(st, dir, "01_campamento_hora_dorada", 17.2f, WeatherKind.Clear, camp + new Vector3(12f, 2.4f, -7f), camp + new Vector3(-1f, 1.6f, 6f), 62f);
            Shot(st, dir, "02_ruinas_hora_dorada", 17.4f, WeatherKind.Clear, ruins + new Vector3(7f, 2.6f, -21f), ruins + new Vector3(0f, 2.2f, 0f), 62f);
            Shot(st, dir, "03_ruinas_interior", 17.0f, WeatherKind.Clear, ruins + new Vector3(0f, 1.7f, -12f), ruins + new Vector3(0f, 1.8f, 8f), 70f);
            Shot(st, dir, "04_bosque_camino", 16.8f, WeatherKind.Clear, camp + new Vector3(42f, 2.2f, 30f), ruins + new Vector3(-6f, 3f, -20f), 66f);
            Shot(st, dir, "05_vista_aerea", 17.5f, WeatherKind.Clear, new Vector3(-150f, 95f, -150f), new Vector3(0f, 8f, 0f), 60f);
            Shot(st, dir, "06_vehiculo", 17.3f, WeatherKind.Clear, st.Vehicle.transform.position + new Vector3(-4.5f, 1.6f, -4.5f), st.Vehicle.transform.position + Vector3.up * 0.8f, 55f);
            Shot(st, dir, "07_niebla_manana", 7.0f, WeatherKind.Fog, camp + new Vector3(12f, 2.4f, -7f), camp + new Vector3(-1f, 1.6f, 6f), 62f);
            Shot(st, dir, "08_campamento_noche", 22.0f, WeatherKind.Clear, camp + new Vector3(9f, 2.2f, -6f), camp + new Vector3(0f, 1.2f, 0f), 62f);
            Shot(st, dir, "10_props_campamento", 17.4f, WeatherKind.Clear, camp + new Vector3(3f, 1.5f, 2.5f), camp + new Vector3(0.5f, 0.5f, 7f), 60f);
            Shot(st, dir, "11_fogata_cerca", 17.6f, WeatherKind.Clear, camp + new Vector3(2.2f, 1.3f, -4.4f), camp + new Vector3(0f, 0.3f, -1.5f), 55f);
            }
            if (only == "biome")
            {
                string t = st.World.Biome.Id;
                Shot(st, dir, t + "_01_campamento", 17.2f, WeatherKind.Clear, camp + new Vector3(12f, 2.4f, -7f), camp + new Vector3(-1f, 1.6f, 6f), 62f);
                Shot(st, dir, t + "_02_ruinas", 17.4f, WeatherKind.Clear, OnGround(st, ruins.x + 6f, ruins.z - 15f, 2.6f), ruins + new Vector3(0f, 2.2f, 0f), 62f);
                Shot(st, dir, t + "_03_camino", 11.0f, WeatherKind.Clear, OnGround(st, camp.x + 30f, camp.z + 20f, 1.8f), OnGround(st, ruins.x - 6f, ruins.z - 20f, 3f), 66f);
                Shot(st, dir, t + "_04_aerea", 15.0f, WeatherKind.Clear, new Vector3(-150f, 95f, -150f), new Vector3(0f, 8f, 0f), 60f);
                Shot(st, dir, t + "_05_suelo", 16.0f, WeatherKind.Clear, OnGround(st, -20f, 30f, 1.7f), OnGround(st, 10f, 60f, 2.5f), 70f);
                Shot(st, dir, t + "_06_niebla", 8.0f, WeatherKind.Fog, camp + new Vector3(36f, 1.8f, 24f), ruins + new Vector3(-6f, 3f, -20f), 66f);
                Shot(st, dir, t + "_07_noche", 22.0f, WeatherKind.Clear, camp + new Vector3(9f, 2.2f, -6f), camp + new Vector3(0f, 1.2f, 0f), 62f);
            }
            if (onlyForest || only == null || only == "")
            {
                // Bosque denso: a ras de suelo, dentro del follaje y con niebla profunda.
                var road = camp + new Vector3(42f, 0f, 30f);
                Shot(st, dir, "14_bosque_denso_camino", 17.0f, WeatherKind.Clear, camp + new Vector3(36f, 1.8f, 24f), ruins + new Vector3(-6f, 3f, -20f), 66f);
                Shot(st, dir, "15_bosque_interior", 16.6f, WeatherKind.Clear, OnGround(st, -20f, 30f, 1.7f), OnGround(st, 10f, 60f, 2.5f), 70f);
                Shot(st, dir, "16_vista_aerea_densa", 17.5f, WeatherKind.Clear, new Vector3(-150f, 95f, -150f), new Vector3(0f, 8f, 0f), 60f);
                Shot(st, dir, "17_bosque_niebla", 8.0f, WeatherKind.Fog, camp + new Vector3(36f, 1.8f, 24f), ruins + new Vector3(-6f, 3f, -20f), 66f);
                MenuCinematic.Pose(camp + new Vector3(0f, 0f, -1.5f), st.World.HeightAt, 12f, out var mp, out var ml);
                Shot(st, dir, "20_menu_cinematico_a", 17.0f, WeatherKind.Clear, mp, ml, 52f);
                MenuCinematic.Pose(camp + new Vector3(0f, 0f, -1.5f), st.World.HeightAt, 45f, out mp, out ml);
                Shot(st, dir, "21_menu_cinematico_b", 17.6f, WeatherKind.Clear, mp, ml, 52f);
                Shot(st, dir, "18_borde_del_mapa", 17.2f, WeatherKind.Clear, OnGround(st, 110f, 0f, 1.8f), OnGround(st, 160f, 0f, 6f), 66f);
            }
            if (!onlyForest && st.Char != null)
            {
                Vector3 pp = st.Player.transform.position;
                PoseCharacter(st, false);
                Shot(st, dir, "12_personaje_detector", 17.3f, WeatherKind.Clear, pp + st.Player.transform.TransformDirection(new Vector3(-2.3f, 1.4f, 3.0f)), pp + Vector3.up * 1.2f, 50f);
                Shot(st, dir, "12b_detector_frente", 17.3f, WeatherKind.Clear, pp + st.Player.transform.TransformDirection(new Vector3(0f, 1.5f, 4.0f)), pp + Vector3.up * 1.2f, 50f);
                Shot(st, dir, "12c_detector_lado", 17.3f, WeatherKind.Clear, pp + st.Player.transform.TransformDirection(new Vector3(4.0f, 1.5f, 0f)), pp + Vector3.up * 1.2f, 50f);
                PoseCharacter(st, true);
                Shot(st, dir, "13b_excavando_lado", 17.3f, WeatherKind.Clear, pp + st.Player.transform.TransformDirection(new Vector3(4.0f, 1.4f, 0.5f)), pp + Vector3.up * 1.0f, 50f);
                Shot(st, dir, "13_personaje_excavando", 17.3f, WeatherKind.Clear, pp + st.Player.transform.TransformDirection(new Vector3(-2.8f, 1.5f, 2.2f)), pp + Vector3.up * 1.0f, 50f);
            }
            if (only != "biome") Shot(st, dir, "09_lluvia_nublado", 14.0f, WeatherKind.Rain, ruins + new Vector3(7f, 2.6f, -21f), ruins + new Vector3(0f, 2.2f, 0f), 62f);
            Debug.Log("[PREVIEW] listo en " + dir);
        }

        static Stage BuildStage()
        {
            var st = new Stage();
            var items = DefaultItems.Specs().Cast<IItemInfo>().ToList();
            var root = new GameObject("World").transform;
            st.Clock = new GameClock(17f, 1);
            st.Weather = new WeatherModel(7);
            var bp = Biomes.ById(System.Environment.GetEnvironmentVariable("HT_BIOME"));
            st.World = new WorldBuilder(1337 + bp.SeedOffset, bp).Build(items, root, st.Clock);

            st.Player = new GameObject("Player");
            var ch = st.Player.AddComponent<PlaceholderCharacter>(); ch.Build();
            st.Player.transform.position = st.World.PlayerSpawn;
            st.Player.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            if (ch.IsModel)
            {
                st.Anim = st.Player.AddComponent<PlaceholderCharacterAnimator>();
                st.Anim.Initialize(ch, null, null);
                st.Char = ch;
                // Detector real (construido por el propio componente) para verlo en la mano.
                var dv = new GameObject("DetectorPreview").AddComponent<HuesoOxido.Detection.DetectorVisual>();
                typeof(HuesoOxido.Detection.DetectorVisual).GetMethod("Build", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(dv, null);
                st.Det = (Transform)typeof(HuesoOxido.Detection.DetectorVisual).GetField("_root", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dv);
            }
            else ch.RightArm.localRotation = Quaternion.Euler(-62f, 0f, 0f);

            st.Vehicle = new GameObject("Vehicle");
            st.Vehicle.transform.SetPositionAndRotation(new Vector3(st.World.VehicleSpawn.x, st.World.HeightAt(st.World.VehicleSpawn.x, st.World.VehicleSpawn.z) + 0.40f, st.World.VehicleSpawn.z), Quaternion.Euler(0f, 140f, 0f));
            PlaceholderVehicle.Build(st.Vehicle.transform, VehicleMounts);

            var camGo = new GameObject("PreviewCamera");
            st.Cam = camGo.AddComponent<UnityEngine.Camera>();
            st.Cam.nearClipPlane = 0.1f; st.Cam.farClipPlane = 700f; st.Cam.allowHDR = true;
            st.Cam.clearFlags = CameraClearFlags.Skybox;
            PostProcessSetup.Apply(st.Cam);

            st.Day = new GameObject("DayNight").AddComponent<DayNightCycle>();
            st.Day.Initialize(st.World.Sun, st.World.Moon, st.Clock, st.Weather, st.World.Skybox, st.Cam, st.World.Biome);
            st.Refl = new GameObject("Reflections").AddComponent<ReflectionBaker>();
            st.Refl.Initialize(st.Player.transform, st.Clock, st.Weather);
            st.Atmo = new GameObject("Atmosphere").AddComponent<AtmosphereFx>();
            st.Atmo.Initialize(st.Cam.transform, st.World.Sun, st.Clock, st.Weather, st.World.HeightAt);
            return st;
        }

        static readonly System.Collections.Generic.List<GameObject> BakedClones = new System.Collections.Generic.List<GameObject>();

        /// <summary>En modo edicion no corre el Player Loop y el skinning no se actualiza: se hornea la pose actual en mallas estaticas.</summary>
        static void BakeCharacter(Stage st)
        {
            foreach (var g in BakedClones) if (g != null) UnityEngine.Object.DestroyImmediate(g);
            BakedClones.Clear();
            foreach (var smr in st.Player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.GetComponent<MeshFilter>() != null) continue;
                var baked = new Mesh();
                smr.BakeMesh(baked, false);
                var go = new GameObject("Baked_" + smr.name);
                go.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
                go.transform.localScale = smr.transform.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh = baked;
                var mats = smr.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) { mats[i] = new Material(mats[i]); mats[i].SetFloat("_Cull", 0f); }
                go.AddComponent<MeshRenderer>().sharedMaterials = mats;
                Debug.Log("[PREVIEW] baked " + smr.name + " verts=" + baked.vertexCount + " bounds=" + baked.bounds.size.ToString("0.00") + " lossy=" + smr.transform.lossyScale.ToString("0.00"));
                smr.enabled = false;
                BakedClones.Add(go);
            }
        }

        static Vector3 cam0Pos(Stage st) => st.Cam.transform.position;

        static Vector3 OnGround(Stage st, float x, float z, float height) => new Vector3(x, st.World.HeightAt(x, z) + height, z);

        static void PoseCharacter(Stage st, bool digging)
        {
            st.Anim.PreviewPose(0f, 0.4f, digging ? PlayerAction.Digging : PlayerAction.Idle);
            BakeCharacter(st);
            var t = st.Player.transform;
            Vector3 dir = Quaternion.AngleAxis(24f, t.right) * t.forward;
            st.Det.gameObject.SetActive(!digging);
            st.Det.SetParent(null, true);
            var tp = st.Player.transform;
            Debug.Log("[PREVIEW] mano(local jugador)=" + tp.InverseTransformPoint(st.Char.RightHand.position).ToString("0.00") + " hombro=" + tp.InverseTransformPoint(st.Char.ArmRBone.position).ToString("0.00") + " izq=" + tp.InverseTransformPoint(st.Char.ArmLBone.position).ToString("0.00") + " dir(local)=" + tp.InverseTransformDirection(dir).ToString("0.00") + " pala activa=" + st.Char.Shovel.gameObject.activeSelf);
            st.Det.rotation = Quaternion.LookRotation(dir, Vector3.up);
            st.Det.position = st.Char.RightHand.position - dir * 0.04f;
        }

        static void Shot(Stage st, string dir, string name, float hour, WeatherKind weather, Vector3 pos, Vector3 look, float fov)
        {
            st.Clock.Set(hour, 1);
            st.Weather.Set(weather);
            st.Day.ApplyNow();
            DynamicGI.UpdateEnvironment();
            st.Cam.transform.position = pos;
            st.Cam.transform.LookAt(look);
            st.Cam.fieldOfView = fov;
            if (st.Refl != null) st.Refl.BakeAll(cam0Pos(st));
            st.Atmo.ApplyNow();
            st.Atmo.PrewarmForPreview(40f);
            { var ff = GameObject.Find("FireFx"); if (ff != null) CampfireFx.Prewarm(ff.transform, 4f); }
            st.Atmo.ApplyNow();
            string path = Path.Combine(dir, name + ".png");
            Capture(st.Cam, path);
            Debug.Log("[PREVIEW] " + name + "  hora=" + hour + " clima=" + weather);
        }

        internal static void Capture(UnityEngine.Camera cam, string path)
        {
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = prevTarget;
            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
