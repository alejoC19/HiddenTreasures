using System;
using System.Collections.Generic;

namespace HuesoOxido.Core
{
    /// <summary>Resumen de una medicion de rendimiento (tiempos de cuadro en milisegundos). Logica pura, testeable.</summary>
    public sealed class PerfSummary
    {
        public int Frames;
        public float AvgFps;
        public float AvgMs, MedianMs, P95Ms, P99Ms, MaxMs;
        /// <summary>FPS del 1 % de cuadros mas lentos (el "1 % low" que se siente como tirones).</summary>
        public float OnePercentLowFps;
        /// <summary>Cuadros mas lentos que el doble de la mediana (tirones).</summary>
        public int Hitches;
    }

    public static class PerfStats
    {
        public static float Percentile(IReadOnlyList<float> sortedAscending, float p)
        {
            if (sortedAscending.Count == 0) return 0f;
            float pos = Math.Max(0f, Math.Min(1f, p)) * (sortedAscending.Count - 1);
            int lo = (int)Math.Floor(pos), hi = (int)Math.Ceiling(pos);
            return sortedAscending[lo] + (sortedAscending[hi] - sortedAscending[lo]) * (pos - lo);
        }

        public static PerfSummary Summarize(IEnumerable<float> frameMs)
        {
            var list = new List<float>();
            foreach (var m in frameMs) if (m > 0f && !float.IsNaN(m) && !float.IsInfinity(m)) list.Add(m);
            var s = new PerfSummary { Frames = list.Count };
            if (list.Count == 0) return s;
            list.Sort();
            double sum = 0; foreach (var m in list) sum += m;
            s.AvgMs = (float)(sum / list.Count);
            s.AvgFps = 1000f / s.AvgMs;
            s.MedianMs = Percentile(list, 0.5f);
            s.P95Ms = Percentile(list, 0.95f);
            s.P99Ms = Percentile(list, 0.99f);
            s.MaxMs = list[list.Count - 1];
            // 1 % low: promedio de FPS del 1 % de cuadros mas lentos
            int worstN = Math.Max(1, list.Count / 100);
            double worstSum = 0; for (int i = list.Count - worstN; i < list.Count; i++) worstSum += list[i];
            s.OnePercentLowFps = 1000f / (float)(worstSum / worstN);
            foreach (var m in list) if (m > s.MedianMs * 2f) s.Hitches++;
            return s;
        }

        /// <summary>Promedio simple (para contadores como draw calls o memoria).</summary>
        public static float Average(IReadOnlyList<float> values)
        {
            if (values.Count == 0) return 0f;
            double s = 0; foreach (var v in values) s += v;
            return (float)(s / values.Count);
        }
    }
}
