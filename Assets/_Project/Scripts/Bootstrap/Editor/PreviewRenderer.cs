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
            Shot(st, dir, "09_lluvia_nublado", 14.0f, WeatherKind.Rain, ruins + new Vector3(7f, 2.6f, -21f), ruins + new Vector3(0f, 2.2f, 0f), 62f);
            Debug.Log("[PREVIEW] listo en " + dir);
        }

        static Stage BuildStage()
        {
            var st = new Stage();
            var items = DefaultItems.Specs().Cast<IItemInfo>().ToList();
            var root = new GameObject("World").transform;
            st.Clock = new GameClock(17f, 1);
            st.Weather = new WeatherModel(7);
            st.World = new WorldBuilder(1337).Build(items, root, st.Clock);

            st.Player = new GameObject("Player");
            var ch = st.Player.AddComponent<PlaceholderCharacter>(); ch.Build();
            st.Player.transform.position = st.World.PlayerSpawn;
            st.Player.transform.rotation = Quaternion.Euler(0f, 35f, 0f);
            ch.RightArm.localRotation = Quaternion.Euler(-62f, 0f, 0f);

            st.Vehicle = new GameObject("Vehicle");
            st.Vehicle.transform.SetPositionAndRotation(st.World.VehicleSpawn - Vector3.up * 0.55f, Quaternion.Euler(0f, 140f, 0f));
            PlaceholderVehicle.Build(st.Vehicle.transform, VehicleMounts);

            var camGo = new GameObject("PreviewCamera");
            st.Cam = camGo.AddComponent<UnityEngine.Camera>();
            st.Cam.nearClipPlane = 0.1f; st.Cam.farClipPlane = 700f; st.Cam.allowHDR = true;
            st.Cam.clearFlags = CameraClearFlags.Skybox;
            PostProcessSetup.Apply(st.Cam);

            st.Day = new GameObject("DayNight").AddComponent<DayNightCycle>();
            st.Day.Initialize(st.World.Sun, st.World.Moon, st.Clock, st.Weather, st.World.Skybox, st.Cam);
            st.Atmo = new GameObject("Atmosphere").AddComponent<AtmosphereFx>();
            st.Atmo.Initialize(st.Cam.transform, st.World.Sun, st.Clock, st.Weather, st.World.HeightAt);
            return st;
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
            st.Atmo.ApplyNow();
            st.Atmo.PrewarmForPreview(40f);
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
