using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Post-procesado moderado (bloom suave, color grading, vineta y profundidad de campo leve) con un Volume global de URP.
    /// Usa reflexion para no romper la compilacion si el paquete URP cambia; si algo falla, el juego sigue sin post-proceso.
    /// </summary>
    public static class PostProcessSetup
    {
        static readonly System.Collections.Generic.Dictionary<string, object> Effects = new System.Collections.Generic.Dictionary<string, object>();

        const string Urp = "Unity.RenderPipelines.Universal.Runtime";
        const string CoreRp = "Unity.RenderPipelines.Core.Runtime";

        public static void Apply(UnityEngine.Camera cam)
        {
            if (GraphicsSettings.currentRenderPipeline == null) return; // pipeline integrado: sin Volume
            try
            {
                var volumeType = Type.GetType("UnityEngine.Rendering.Volume, " + CoreRp);
                var profileType = Type.GetType("UnityEngine.Rendering.VolumeProfile, " + CoreRp);
                var camDataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, " + Urp);
                if (volumeType == null || profileType == null || camDataType == null) return;

                var camData = cam.GetComponent(camDataType) ?? cam.gameObject.AddComponent(camDataType);
                camDataType.GetProperty("renderPostProcessing")?.SetValue(camData, true);
                var aaProp = camDataType.GetProperty("antialiasing");
                if (aaProp != null)
                {
                    var aaType = aaProp.PropertyType;
                    aaProp.SetValue(camData, Enum.Parse(aaType, "SubpixelMorphologicalAntiAliasing"));
                }

                var go = new GameObject("PostProcessVolume");
                var volume = go.AddComponent(volumeType);
                volumeType.GetProperty("isGlobal").SetValue(volume, true);
                var profile = ScriptableObject.CreateInstance(profileType);
                var add = profileType.GetMethod("Add", new[] { typeof(Type), typeof(bool) });

                object Add(string typeName) { var c = add.Invoke(profile, new object[] { Type.GetType("UnityEngine.Rendering.Universal." + typeName + ", " + Urp), true }); Effects[typeName] = c; return c; }

                var bloom = Add("Bloom");
                Set(bloom, "intensity", 0.45f); Set(bloom, "threshold", 0.9f); Set(bloom, "scatter", 0.7f);
                Set(bloom, "tint", new Color(1f, 0.92f, 0.8f));

                var color = Add("ColorAdjustments");
                Set(color, "postExposure", 0.3f); Set(color, "contrast", 22f); Set(color, "saturation", 6f);
                Set(color, "colorFilter", new Color(1f, 0.96f, 0.9f));

                var vignette = Add("Vignette");
                Set(vignette, "intensity", 0.32f); Set(vignette, "smoothness", 0.5f);

                var tone = Add("Tonemapping");
                SetEnum(tone, "mode", "ACES");

                var dof = Add("DepthOfField");
                SetEnum(dof, "mode", "Gaussian");
                Set(dof, "gaussianStart", 45f); Set(dof, "gaussianEnd", 160f); Set(dof, "gaussianMaxRadius", 0.8f);

                volumeType.GetProperty("sharedProfile").SetValue(volume, profile);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[PostProcess] No se pudo configurar el post-procesado: " + e.Message);
            }
        }

        /// <summary>Enciende o apaga un efecto del Volume global ("DepthOfField", "Bloom", "Vignette"...). Si no existe, no hace nada.</summary>
        public static void SetEffect(string typeName, bool on)
        {
            if (!Effects.TryGetValue(typeName, out var c) || c == null) return;
            var t = c.GetType();
            var f = t.GetField("active");
            if (f != null) { f.SetValue(c, on); return; }
            t.GetProperty("active")?.SetValue(c, on);
        }

        static void Set(object component, string field, object value)
        {
            if (component == null) return;
            var f = component.GetType().GetField(field);
            var param = f?.GetValue(component);
            var m = param?.GetType().GetMethod("Override", new[] { value.GetType() });
            m?.Invoke(param, new[] { value });
        }

        static void SetEnum(object component, string field, string enumName)
        {
            if (component == null) return;
            var f = component.GetType().GetField(field);
            var param = f?.GetValue(component);
            if (param == null) return;
            var valueType = param.GetType().BaseType.GetGenericArguments()[0];
            var m = param.GetType().GetMethod("Override", new[] { valueType });
            m?.Invoke(param, new[] { Enum.Parse(valueType, enumName) });
        }
    }
}
