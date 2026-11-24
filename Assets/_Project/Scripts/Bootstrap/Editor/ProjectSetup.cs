using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HuesoOxido.Core.EditorTools
{
    /// <summary>
    /// Configura el proyecto la primera vez que se abre: Input System, espacio de color, URP y la escena Main.
    /// Cada paso es idempotente y se puede relanzar desde el menu "Hidden Treasures".
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        const string SettingsDir = "Assets/_Project/Settings";

        static ProjectSetup()
        {
            EditorApplication.delayCall += AutoRun;
        }

        static void AutoRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EnsureInputHandler()) return; // reinicia el Editor
            EnsureColorSpace();
            EnsureUrp();
            EnsureScene();
        }

        [MenuItem("Hidden Treasures/Configurar proyecto")]
        static void RunFromMenu()
        {
            if (EnsureInputHandler()) return;
            EnsureColorSpace();
            EnsureUrp();
            EnsureScene();
            Debug.Log("[Hidden Treasures] Proyecto configurado.");
        }

        // ---- Input System ----
        static bool EnsureInputHandler()
        {
            var settings = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
            if (settings == null) return false;
            var so = new SerializedObject(settings);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null || prop.intValue != 0) return false;

            prop.intValue = 1; // 0 = Input Manager viejo, 1 = Input System, 2 = ambos
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            if (EditorUtility.DisplayDialog("Hidden Treasures",
                    "Se activó el nuevo Input System como sistema de entrada. Unity necesita reiniciarse para aplicarlo.",
                    "Reiniciar ahora", "Después"))
            {
                var m = typeof(EditorApplication).GetMethod("RestartEditorAndRecompileScripts", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
                m?.Invoke(null, null);
            }
            return true;
        }

        static void EnsureColorSpace()
        {
            if (PlayerSettings.colorSpace != ColorSpace.Linear) PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        // ---- URP ----
        static void EnsureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return;
            try
            {
                var assetType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
                var rendererType = Type.GetType("UnityEngine.Rendering.Universal.UniversalRendererData, Unity.RenderPipelines.Universal.Runtime");
                var baseRendererType = Type.GetType("UnityEngine.Rendering.Universal.ScriptableRendererData, Unity.RenderPipelines.Universal.Runtime");
                if (assetType == null || rendererType == null || baseRendererType == null)
                {
                    Debug.LogWarning("[Hidden Treasures] El paquete URP no está instalado todavía; reabrí el proyecto cuando termine de importar.");
                    return;
                }

                if (!AssetDatabase.IsValidFolder(SettingsDir)) AssetDatabase.CreateFolder("Assets/_Project", "Settings");
                var renderer = ScriptableObject.CreateInstance(rendererType);
                AssetDatabase.CreateAsset(renderer, SettingsDir + "/HT_Renderer.asset");

                var create = assetType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static, null, new[] { baseRendererType }, null);
                var pipeline = create?.Invoke(null, new object[] { renderer }) as RenderPipelineAsset;
                if (pipeline == null) { Debug.LogWarning("[Hidden Treasures] No se pudo crear el URP Asset."); return; }
                AssetDatabase.CreateAsset(pipeline, SettingsDir + "/HT_URP.asset");

                // Ajustes moderados: sombras suaves y alcance razonable.
                var so = new SerializedObject(pipeline);
                SetIfExists(so, "m_ShadowDistance", 110f);
                SetIfExists(so, "m_MainLightShadowmapResolution", 2048);
                so.ApplyModifiedPropertiesWithoutUndo();

                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                AssetDatabase.SaveAssets();
                Debug.Log("[Hidden Treasures] URP configurado (" + SettingsDir + "/HT_URP.asset). Para el ambient occlusion: HT_Renderer → Add Renderer Feature → Screen Space Ambient Occlusion.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Hidden Treasures] No se pudo configurar URP automáticamente: " + e.Message);
            }
        }

        static void SetIfExists(SerializedObject so, string name, float v)
        {
            var p = so.FindProperty(name);
            if (p == null) return;
            if (p.propertyType == SerializedPropertyType.Float) p.floatValue = v;
            else if (p.propertyType == SerializedPropertyType.Integer) p.intValue = (int)v;
        }

        // ---- Escena ----
        static void EnsureScene()
        {
            if (!System.IO.File.Exists(ScenePath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes")) AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            var scenes = EditorBuildSettings.scenes;
            if (!scenes.Any(s => s.path == ScenePath))
                EditorBuildSettings.scenes = scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();

            var active = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path) && !active.isDirty && active.rootCount == 0)
                EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
