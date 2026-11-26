using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Unity.Profiling;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Perfilado reproducible (solo Editor y builds de desarrollo). F9 en juego, o el argumento <c>-htperf</c> al abrir el .exe:
    /// recorre 4 lugares (campamento, camino, bosque denso, ruinas) en los 3 niveles de calidad con una camara que gira,
    /// y mide por cuadro: tiempo total, CPU y GPU (FrameTimingManager), draw calls, SetPass, triangulos y memoria.
    /// Escribe perf_summary.md y perf_frames.csv en la carpeta de datos del juego y la muestra en pantalla / log.
    /// </summary>
    [DefaultExecutionOrder(600)]
    public sealed class PerfRecorder : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        struct Place { public string Name; public Vector3 Pos; public Vector3 Look; }

        const float WarmupSeconds = 2.5f, RecordSeconds = 12f;

        GameContext _c;
        bool _running, _autoTried;
        string _status = "", _lastReport = "";
        Vector3 _camPos, _camLook; bool _applyCam;
        float _yawOffset;

        public bool IsRunning => _running;

        public void Initialize(GameContext c) { _c = c; }

        void Update()
        {
            if (_c == null || _running) return;
            bool key = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f9Key.wasPressedThisFrame;
            bool auto = !_autoTried && Array.IndexOf(Environment.GetCommandLineArgs(), "-htperf") >= 0 && Time.realtimeSinceStartup > 4f;
            if (auto) _autoTried = true;
            if (key || auto) StartCoroutine(Run(auto));
        }

        void LateUpdate()
        {
            if (!_applyCam || _c == null || _c.Cam == null) return;
            var rot = Quaternion.LookRotation(_camLook - _camPos, Vector3.up) * Quaternion.Euler(0f, _yawOffset, 0f);
            _c.Cam.transform.SetPositionAndRotation(_camPos, rot);
            _c.Cam.fieldOfView = 66f;
        }

        Vector3 G(float x, float z, float h) => new Vector3(x, _c.World.HeightAt(x, z) + h, z);

        List<Place> Places()
        {
            var camp = _c.World.CampPosition; var ruins = _c.World.RuinsCenter;
            return new List<Place>
            {
                new Place { Name = "Campamento", Pos = camp + new Vector3(9f, 2.2f, -6f), Look = camp + new Vector3(-1f, 1.4f, 5f) },
                new Place { Name = "Camino", Pos = G(camp.x + 36f, camp.z + 24f, 1.8f), Look = G(ruins.x - 6f, ruins.z - 20f, 3f) },
                new Place { Name = "Bosque denso", Pos = G(-20f, 30f, 1.7f), Look = G(10f, 60f, 2.5f) },
                new Place { Name = "Ruinas", Pos = ruins + new Vector3(0f, 1.7f, -12f), Look = ruins + new Vector3(0f, 1.8f, 8f) },
            };
        }

        IEnumerator Run(bool quitAtEnd)
        {
            _running = true;
            var s = _c.Services;
            var quality = _c.Quality;
            var originalTier = quality != null ? quality.Tier : QualityTier.Medium;
            bool adaptive = quality != null && quality.AdaptiveEnabled;
            if (quality != null) quality.AdaptiveEnabled = false;
            _c.Ui.CloseAll();
            if (_c.Rig != null) _c.Rig.enabled = false;
            _c.Input.SetUiMode(false);

            var report = new StringBuilder();
            var csv = new StringBuilder("tier,place,frame,ms,cpu_ms,gpu_ms,draw_calls,setpass,triangles\n");
            report.AppendLine("# Informe de rendimiento — HiddenTreasures");
            report.AppendLine();
            report.AppendLine("- Fecha: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            report.AppendLine("- GPU: " + SystemInfo.graphicsDeviceName + " (" + SystemInfo.graphicsMemorySize + " MB) · API: " + SystemInfo.graphicsDeviceType);
            report.AppendLine("- CPU: " + SystemInfo.processorType + " (" + SystemInfo.processorCount + " hilos) · RAM: " + SystemInfo.systemMemorySize + " MB");
            report.AppendLine("- Resolucion: " + Screen.width + "x" + Screen.height + " · Build: " + (Application.isEditor ? "EDITOR (menos representativo)" : "Desarrollo") + " · Unity " + Application.unityVersion);
            report.AppendLine("- FrameTimingManager (CPU/GPU): " + (FrameTimingManager.IsFeatureEnabled() ? "activo" : "NO disponible (se usa solo el tiempo de cuadro)"));
            report.AppendLine("- Recorrido: " + RecordSeconds + " s por lugar tras " + WarmupSeconds + " s de calentamiento, hora 17:12, despejado, camara girando.");
            report.AppendLine();
            report.AppendLine("| Calidad | Lugar | FPS prom. | 1 % low | ms prom. | ms p99 | CPU ms | GPU ms | Draw calls | SetPass | Triangulos (M) | Tirones |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");

            using (var dc = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
            using (var sp = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count"))
            using (var tr = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count"))
            {
                var timings = new FrameTiming[1];
                foreach (var tier in new[] { QualityTier.High, QualityTier.Medium, QualityTier.Low })
                {
                    if (quality != null) quality.SetTier(tier, false);
                    foreach (var place in Places())
                    {
                        s.Clock.Set(17.2f, 1); s.Weather.Set(WeatherKind.Clear);
                        _camPos = place.Pos; _camLook = place.Look; _yawOffset = 0f; _applyCam = true;
                        float t = 0f;
                        while (t < WarmupSeconds) { t += Time.unscaledDeltaTime; _status = Label(tier, place.Name, t, WarmupSeconds, true); _yawOffset = t * 20f; yield return null; }

                        var ms = new List<float>(); var cpu = new List<float>(); var gpu = new List<float>();
                        var dcs = new List<float>(); var sps = new List<float>(); var tris = new List<float>();
                        t = 0f; int frame = 0;
                        while (t < RecordSeconds)
                        {
                            float dt = Time.unscaledDeltaTime; t += dt;
                            _yawOffset = (t + WarmupSeconds) * 20f;
                            _status = Label(tier, place.Name, t, RecordSeconds, false);
                            float frameMs = dt * 1000f;
                            float cpuMs = 0f, gpuMs = 0f;
                            if (FrameTimingManager.IsFeatureEnabled())
                            {
                                FrameTimingManager.CaptureFrameTimings();
                                if (FrameTimingManager.GetLatestTimings(1, timings) > 0) { cpuMs = (float)timings[0].cpuFrameTime; gpuMs = (float)timings[0].gpuFrameTime; }
                            }
                            ms.Add(frameMs); if (cpuMs > 0f) cpu.Add(cpuMs); if (gpuMs > 0f) gpu.Add(gpuMs);
                            float d = dc.Valid ? dc.LastValue : 0f, p = sp.Valid ? sp.LastValue : 0f, tv = tr.Valid ? tr.LastValue : 0f;
                            dcs.Add(d); sps.Add(p); tris.Add(tv);
                            csv.Append(tier).Append(',').Append(place.Name).Append(',').Append(frame++).Append(',').Append(frameMs.ToString("0.00")).Append(',')
                               .Append(cpuMs.ToString("0.00")).Append(',').Append(gpuMs.ToString("0.00")).Append(',').Append(d).Append(',').Append(p).Append(',').Append(tv).Append('\n');
                            yield return null;
                        }
                        var sum = PerfStats.Summarize(ms);
                        report.AppendLine("| " + QualityProfile.NameOf(tier) + " | " + place.Name + " | " + sum.AvgFps.ToString("0.0") + " | " + sum.OnePercentLowFps.ToString("0.0") + " | "
                            + sum.AvgMs.ToString("0.0") + " | " + sum.P99Ms.ToString("0.0") + " | " + Fmt(cpu) + " | " + Fmt(gpu) + " | " + PerfStats.Average(dcs).ToString("0") + " | "
                            + PerfStats.Average(sps).ToString("0") + " | " + (PerfStats.Average(tris) / 1e6f).ToString("0.00") + " | " + sum.Hitches + " |");
                    }
                }
            }

            report.AppendLine();
            report.AppendLine("Memoria al final: " + (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)) + " MB asignados · "
                              + (UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024 * 1024)) + " MB reservados.");
            report.AppendLine("Lectura rapida: si GPU ms > CPU ms el limite es la tarjeta; si CPU ms > GPU ms es el procesador (draw calls, scripts). 60 FPS = 16,7 ms.");

            string dir = Path.Combine(Application.persistentDataPath, "perf", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "perf_summary.md"), report.ToString(), Encoding.UTF8);
                File.WriteAllText(Path.Combine(dir, "perf_frames.csv"), csv.ToString(), Encoding.UTF8);
                _lastReport = dir;
                Debug.Log("[PERF] Informe en " + dir + "\n" + report);
            }
            catch (Exception e) { Debug.LogError("[PERF] No se pudo escribir el informe: " + e.Message); }

            _applyCam = false;
            if (_c.Rig != null) _c.Rig.enabled = true;
            if (quality != null) { quality.AdaptiveEnabled = adaptive; quality.SetTier(originalTier, false); }
            _status = "";
            _running = false;
            if (quitAtEnd) Application.Quit();
        }

        static string Fmt(List<float> v) => v.Count == 0 ? "-" : PerfStats.Average(v).ToString("0.0");

        static string Label(QualityTier tier, string place, float t, float total, bool warm)
            => "Midiendo rendimiento…  " + QualityProfile.NameOf(tier) + " · " + place + " · " + (warm ? "calentando " : "") + Mathf.Min(t, total).ToString("0") + "/" + total.ToString("0") + " s";

        void OnGUI()
        {
            if (_running && _status.Length > 0) GUI.Box(new Rect(10, 10, 620, 30), _status);
            else if (_lastReport.Length > 0 && Time.unscaledTime < 99999f) GUI.Label(new Rect(10, 10, 900, 24), "Informe de rendimiento guardado en: " + _lastReport + "  (F9 para repetir)");
        }
#else
        public void Initialize(GameContext c) { }
#endif
    }
}
