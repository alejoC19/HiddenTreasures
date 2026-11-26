using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HuesoOxido.Core.EditorTools
{
    /// <summary>Compila un build de DESARROLLO para Windows listo para medir rendimiento (mas fiel que el Play Mode del Editor).</summary>
    public static class PerfBuild
    {
        const string Output = "Builds/Perf/HiddenTreasuresPerf.exe";

        [MenuItem("Hidden Treasures/Compilar build de rendimiento (Windows)")]
        public static void Build()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) { Debug.LogError("[PerfBuild] No hay escenas en Build Settings."); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            PlayerSettings.enableFrameTimingStats = true;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes, locationPathName = Output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development,
            });
            Debug.Log("[PerfBuild] Resultado: " + report.summary.result + " -> " + Path.GetFullPath(Output)
                      + "\nEjecutar:  " + Output.Replace('/', '\\') + " -htperf -screen-width 1920 -screen-height 1080 -screen-fullscreen 0");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
