using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HuesoOxido.Core.EditorTools
{
    /// <summary>
    /// Verificacion de configuracion que se puede hacer SIN entrar en Play Mode (no requiere licencia en batch):
    /// escena, script del bootstrap, URP, Input System y paquetes. Uso: menu "Hidden Treasures/Verificar proyecto"
    /// o por linea de comandos con -batchmode -executeMethod HuesoOxido.Core.EditorTools.ProjectVerifier.RunBatch
    /// </summary>
    public static class ProjectVerifier
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("Hidden Treasures/Verificar proyecto")]
        static void RunFromMenu() => Debug.Log(string.Join("\n", Verify(out bool ok)) + "\n=> " + (ok ? "OK" : "CON PROBLEMAS"));

        public static void RunBatch()
        {
            var lines = Verify(out bool ok);
            foreach (var l in lines) Debug.Log("[VERIFY] " + l);
            Debug.Log("[VERIFY] RESULTADO: " + (ok ? "OK" : "FALLO"));
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static List<string> Verify(out bool ok)
        {
            var r = new List<string>();
            bool good = true;
            void Check(bool cond, string okMsg, string badMsg) { r.Add((cond ? "OK    " : "FALLA ") + (cond ? okMsg : badMsg)); good &= cond; }

            // Escena
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Check(scene.IsValid() && scene.isLoaded, "Main.unity abre", "Main.unity no abre");
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "GameBootstrap");
            Check(root != null, "la escena tiene el objeto GameBootstrap", "falta el objeto GameBootstrap");
            if (root != null)
            {
                var comp = root.GetComponent<GameBootstrap>();
                Check(comp != null, "el componente GameBootstrap se resuelve por GUID (no es 'Missing Script')", "script faltante en GameBootstrap");
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root) == 0, "sin MonoBehaviours con script faltante", "hay scripts faltantes");
                if (comp != null)
                {
                    var seed = new SerializedObject(comp).FindProperty("worldSeed");
                    Check(seed != null && seed.intValue == 1337, "campo serializado worldSeed = 1337", "worldSeed no se deserializa");
                }
            }
            Check(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == ScenePath), "Main.unity esta en Build Settings", "Main.unity no esta en Build Settings");

            // Input System
            var ps = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
            int handler = ps != null ? new SerializedObject(ps).FindProperty("activeInputHandler").intValue : -1;
            Check(handler == 1 || handler == 2, "Active Input Handling = " + handler + " (el Input System esta activo)", "Active Input Handling = " + handler + ": el Input System NO esta activo");
            Check(PlayerSettings.colorSpace == ColorSpace.Linear, "espacio de color Linear", "espacio de color Gamma");

            // URP
            var rp = GraphicsSettings.defaultRenderPipeline;
            Check(rp != null, "pipeline por defecto: " + (rp != null ? rp.GetType().Name + " (" + rp.name + ")" : ""), "no hay URP asignado en Graphics Settings");
            if (rp != null)
            {
                Check(rp.GetType().Name == "UniversalRenderPipelineAsset", "es un UniversalRenderPipelineAsset", "el pipeline no es URP");
                var so = new SerializedObject(rp);
                var rend = so.FindProperty("m_RendererDataList");
                var rd = rend != null && rend.arraySize > 0 ? rend.GetArrayElementAtIndex(0).objectReferenceValue : null;
                Check(rd != null, "el URP Asset tiene Renderer: " + (rd != null ? rd.name : ""), "el URP Asset no tiene Renderer");
                if (rd != null)
                {
                    var feats = new SerializedObject(rd).FindProperty("m_RendererFeatures");
                    var names = new List<string>();
                    for (int i = 0; feats != null && i < feats.arraySize; i++) { var o = feats.GetArrayElementAtIndex(i).objectReferenceValue; if (o != null) names.Add(o.GetType().Name); }
                    Check(names.Contains("ScreenSpaceAmbientOcclusion"), "Renderer con SSAO (" + string.Join(", ", names) + ")", "el Renderer no tiene SSAO");
                }
            }

            // Paquetes
            var pkgs = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().ToDictionary(p => p.name, p => p.version);
            foreach (var need in new[] { "com.unity.inputsystem", "com.unity.render-pipelines.universal", "com.unity.ugui", "com.unity.test-framework" })
                Check(pkgs.ContainsKey(need), need + " " + (pkgs.ContainsKey(need) ? pkgs[need] : ""), need + " no esta instalado");
            Check(!pkgs.ContainsKey("com.unity.cinemachine"), "sin Cinemachine (no se usa)", "Cinemachine instalado sin necesidad");

            ok = good;
            return r;
        }
    }
}
