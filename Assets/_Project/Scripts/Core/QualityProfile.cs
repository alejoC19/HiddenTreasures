using System;

namespace HuesoOxido.Core
{
    public enum QualityTier { Low = 0, Medium = 1, High = 2 }

    /// <summary>Valores de cada nivel de calidad grafica. Logica pura (los aplica GraphicsQuality en Unity).</summary>
    public sealed class QualityProfile
    {
        public QualityTier Tier;
        public string Name;
        public float VegetationDistanceScale;   // multiplica la distancia de dibujo de plantas y arboles
        public float TreeLodScale;              // multiplica la distancia a la que los arboles usan el modelo simple
        public float TreeShadowScale;           // multiplica la distancia de sombras de los arboles
        public float RenderScale;               // resolucion interna (URP)
        public float ShadowDistance;            // metros
        public int ShadowCascades;
        public bool Ssao, DepthOfField;

        public static QualityProfile For(QualityTier t)
        {
            switch (t)
            {
                case QualityTier.Low:
                    return new QualityProfile { Tier = t, Name = "Bajo", VegetationDistanceScale = 0.6f, TreeLodScale = 0.65f, TreeShadowScale = 0.5f, RenderScale = 0.75f, ShadowDistance = 28f, ShadowCascades = 1, Ssao = false, DepthOfField = false };
                case QualityTier.High:
                    return new QualityProfile { Tier = t, Name = "Alto", VegetationDistanceScale = 1f, TreeLodScale = 1.3f, TreeShadowScale = 1.3f, RenderScale = 1f, ShadowDistance = 55f, ShadowCascades = 3, Ssao = true, DepthOfField = true };
                default:
                    return new QualityProfile { Tier = QualityTier.Medium, Name = "Medio", VegetationDistanceScale = 0.8f, TreeLodScale = 1f, TreeShadowScale = 0.9f, RenderScale = 0.9f, ShadowDistance = 40f, ShadowCascades = 2, Ssao = true, DepthOfField = false };
            }
        }

        public static string NameOf(QualityTier t) => For(t).Name;

        /// <summary>Nivel inicial segun la memoria de video (MB): una GPU media (p. ej. RX 580 de 8 GB) arranca en Medio.</summary>
        public static QualityTier Auto(int videoMemoryMb)
        {
            if (videoMemoryMb > 0 && videoMemoryMb < 3500) return QualityTier.Low;
            if (videoMemoryMb >= 12000) return QualityTier.High;
            return QualityTier.Medium;
        }

        public static QualityTier Next(QualityTier t) => (QualityTier)(((int)t + 1) % 3);
    }

    /// <summary>
    /// Baja la calidad un escalon si los FPS promedio quedan bajos durante un rato de juego (no cuenta los menus ni los primeros
    /// segundos de carga). Nunca sube sola. Logica pura: se alimenta con dt cada frame.
    /// </summary>
    public sealed class AdaptiveQuality
    {
        public float TargetFps = 40f;
        public float WarmupSeconds = 8f;
        public float WindowSeconds = 6f;
        public float CooldownSeconds = 15f;

        float _active, _time, _sinceChange = 999f;
        int _frames;

        /// <summary>Devuelve el nuevo nivel si hay que bajar, o null.</summary>
        public QualityTier? Feed(float dt, bool gameplayActive, QualityTier current)
        {
            _sinceChange += dt;
            if (!gameplayActive) { _time = 0f; _frames = 0; return null; }
            _active += dt;
            if (_active < WarmupSeconds) return null;
            _time += dt; _frames++;
            if (_time < WindowSeconds) return null;
            float avg = _frames / _time;
            _time = 0f; _frames = 0;
            if (avg < TargetFps && current > QualityTier.Low && _sinceChange >= CooldownSeconds)
            {
                _sinceChange = 0f;
                return (QualityTier)((int)current - 1);
            }
            return null;
        }

        /// <summary>Reinicia la medicion (p. ej. al cambiar el nivel a mano).</summary>
        public void Reset() { _time = 0f; _frames = 0; _sinceChange = 0f; }
    }
}
