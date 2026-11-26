using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Calidad grafica: tres niveles (Bajo / Medio / Alto) que ajustan distancia de dibujo de la vegetacion, LOD y sombras de
    /// arboles, resolucion interna, sombras de URP, SSAO y profundidad de campo. El nivel inicial sale de la memoria de video,
    /// se recuerda (PlayerPrefs) y baja solo un escalon si los FPS son bajos. Los ajustes de URP usan reflexion y se restauran
    /// al salir para no ensuciar los assets del proyecto.
    /// </summary>
    public sealed class GraphicsQuality : MonoBehaviour
    {
        const string PrefKey = "ht_quality";

        InstancedVegetation _veg;
        Func<bool> _gameplayActive;
        Action<string> _notify;
        readonly AdaptiveQuality _adaptive = new AdaptiveQuality();
        readonly Dictionary<string, object> _originals = new Dictionary<string, object>();
        object _asset;

        /// <summary>El perfilado lo apaga para medir cada nivel sin que el juego cambie de nivel solo.</summary>
        public bool AdaptiveEnabled { get; set; } = true;
        public QualityTier Tier { get; private set; } = QualityTier.Medium;
        public string TierName => QualityProfile.NameOf(Tier);

        public void Initialize(InstancedVegetation veg, Func<bool> gameplayActive, Action<string> notify)
        {
            _veg = veg; _gameplayActive = gameplayActive; _notify = notify;
            _asset = GraphicsSettings.currentRenderPipeline;
            int saved = PlayerPrefs.GetInt(PrefKey, -1);
            Tier = saved >= 0 && saved <= 2 ? (QualityTier)saved : QualityProfile.Auto(SystemInfo.graphicsMemorySize);
            Apply();
        }

        public void SetTier(QualityTier tier, bool persist)
        {
            Tier = tier;
            if (persist) { PlayerPrefs.SetInt(PrefKey, (int)tier); PlayerPrefs.Save(); }
            _adaptive.Reset();
            Apply();
        }

        public void Cycle() => SetTier(QualityProfile.Next(Tier), true);

        void Apply()
        {
            var p = QualityProfile.For(Tier);
            if (_veg != null) { _veg.DistanceScale = p.VegetationDistanceScale; _veg.LodScale = p.TreeLodScale; _veg.ShadowScale = p.TreeShadowScale; }
            PostProcessSetup.SetEffect("DepthOfField", p.DepthOfField);
            if (_asset == null) return;
            SetAsset("renderScale", p.RenderScale);
            SetAsset("shadowDistance", p.ShadowDistance);
            SetAsset("shadowCascadeCount", p.ShadowCascades);
            SetSsao(p.Ssao);
        }

        void SetAsset(string property, object value)
        {
            try
            {
                var prop = _asset.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || !prop.CanWrite) return;
                if (!_originals.ContainsKey(property)) _originals[property] = prop.GetValue(_asset);
                prop.SetValue(_asset, Convert.ChangeType(value, prop.PropertyType));
            }
            catch (Exception e) { Debug.LogWarning("[Calidad] No se pudo ajustar " + property + ": " + e.Message); }
        }

        void SetSsao(bool on)
        {
            try
            {
                var renderer = _asset.GetType().GetProperty("scriptableRenderer", BindingFlags.Public | BindingFlags.Instance)?.GetValue(_asset);
                var features = renderer?.GetType().GetProperty("rendererFeatures", BindingFlags.Public | BindingFlags.Instance)?.GetValue(renderer) as System.Collections.IEnumerable;
                if (features == null) return;
                foreach (var f in features)
                {
                    if (f == null || f.GetType().Name != "ScreenSpaceAmbientOcclusion") continue;
                    var isActive = f.GetType().GetProperty("isActive", BindingFlags.Public | BindingFlags.Instance);
                    if (isActive != null && !_originals.ContainsKey("ssao")) _originals["ssao"] = isActive.GetValue(f);
                    f.GetType().GetMethod("SetActive", new[] { typeof(bool) })?.Invoke(f, new object[] { on });
                }
            }
            catch (Exception e) { Debug.LogWarning("[Calidad] No se pudo ajustar SSAO: " + e.Message); }
        }

        void Update()
        {
            if (_gameplayActive == null || !AdaptiveEnabled) return;
            var drop = _adaptive.Feed(Time.unscaledDeltaTime, _gameplayActive(), Tier);
            if (drop.HasValue)
            {
                SetTier(drop.Value, true);
                _notify?.Invoke("Rendimiento bajo: calidad gráfica en " + TierName + " (cambiala en el menú)");
            }
        }

        void RestoreAsset()
        {
            if (_asset == null) return;
            foreach (var kv in _originals)
            {
                try
                {
                    if (kv.Key == "ssao") SetSsao((bool)kv.Value);
                    else _asset.GetType().GetProperty(kv.Key, BindingFlags.Public | BindingFlags.Instance)?.SetValue(_asset, kv.Value);
                }
                catch { /* al salir no importa */ }
            }
            _originals.Clear();
        }

        void OnDestroy() { RestoreAsset(); }
        void OnApplicationQuit() { RestoreAsset(); }
    }
}
