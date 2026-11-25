using System;

namespace HuesoOxido.Core
{
    public enum SurfaceKind { Grass, Dirt, Stone }

    /// <summary>
    /// Audio procedural (PCM mono 22 050 Hz, muestras en -1..1) sin archivos: ambiente, pisadas por superficie, pala, cepillo,
    /// "clink" de reliquia, moneda y bip del detector. Logica pura y determinista por semilla (testeable fuera de Unity).
    /// </summary>
    public static class ProceduralSounds
    {
        public const int Rate = 22050;

        // ---------------------------------------------------------------- utilidades

        static float Noise(Random r) => (float)(r.NextDouble() * 2.0 - 1.0);

        static void Normalize(float[] d, float peak)
        {
            float max = 0f;
            for (int i = 0; i < d.Length; i++) max = Math.Max(max, Math.Abs(d[i]));
            if (max < 1e-6f) return;
            float k = peak / max;
            for (int i = 0; i < d.Length; i++) d[i] *= k;
        }

        static float[] Alloc(float seconds) => new float[Math.Max(1, (int)(seconds * Rate))];

        /// <summary>Ventana con ataque y caida suaves para evitar chasquidos.</summary>
        static float Fade(int i, int n, int attack, int release)
        {
            float a = attack <= 0 ? 1f : Math.Min(1f, i / (float)attack);
            float r = release <= 0 ? 1f : Math.Min(1f, (n - 1 - i) / (float)release);
            return a * r;
        }

        // ---------------------------------------------------------------- detector

        /// <summary>Frecuencia del bip (Hz) segun la senal 0..1: mas cerca, mas agudo.</summary>
        public static float DetectorFrequency(float signal)
        {
            float s = Math.Max(0f, Math.Min(1f, signal));
            return 520f + (1500f - 520f) * (float)Math.Pow(s, 0.9);
        }

        /// <summary>Segundos entre bips: mas cerca, mas rapido.</summary>
        public static float DetectorInterval(float signal)
        {
            float s = Math.Max(0f, Math.Min(1f, signal));
            return 1.1f + (0.07f - 1.1f) * (float)Math.Pow(s, 0.8);
        }

        /// <summary>Bip con timbre segun el objetivo: grave y opaco (comun), con armonicos (valioso) y con brillo y vibrato (reliquia).</summary>
        public static float[] Beep(float frequency, DetectorTone tone, float seconds = 0.12f)
        {
            float h2, h3, vib;
            switch (tone)
            {
                case DetectorTone.Mid: h2 = 0.35f; h3 = 0f; vib = 0f; break;
                case DetectorTone.High: h2 = 0.5f; h3 = 0.3f; vib = 0f; break;
                case DetectorTone.Relic: h2 = 0.55f; h3 = 0.4f; vib = 0.012f; break;
                default: h2 = 0f; h3 = 0f; vib = 0f; break;
            }
            var d = Alloc(seconds);
            float norm = 1f / (1f + h2 + h3);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                double ph = 2.0 * Math.PI * frequency * t * (1.0 + vib * Math.Sin(2.0 * Math.PI * 28.0 * t));
                float v = (float)(Math.Sin(ph) + h2 * Math.Sin(2.0 * ph) + h3 * Math.Sin(3.0 * ph)) * norm;
                d[i] = v * Fade(i, d.Length, 120, 600) * 0.8f;
            }
            return d;
        }

        public static float[] Beep(float frequency, float seconds = 0.12f)
        {
            var d = Alloc(seconds);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                d[i] = (float)Math.Sin(2.0 * Math.PI * frequency * t) * Fade(i, d.Length, 120, 600) * 0.8f;
            }
            return d;
        }

        // ---------------------------------------------------------------- ambiente

        /// <summary>Viento entre pinos: ruido grave filtrado con oleadas lentas. Se repite sin saltos (bucle).</summary>
        public static float[] Wind(float seconds, int seed)
        {
            var r = new Random(seed);
            int n = Math.Max(1, (int)(seconds * Rate));
            int fade = Rate / 2;
            var raw = new float[n + fade];
            float lp1 = 0f, lp2 = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                lp1 += (Noise(r) - lp1) * 0.025f;          // cuerpo grave
                lp2 += (Noise(r) - lp2) * 0.12f;           // silbido entre las agujas
                raw[i] = lp1 * 5.5f + (lp2 - lp1) * 0.9f;
            }
            var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float v = raw[i];
                if (i < fade) { float w = i / (float)fade; v = raw[i] * w + raw[n + i] * (1f - w); }   // empalme del bucle
                float swell = 0.62f + 0.38f * (float)Math.Sin(2.0 * Math.PI * 3.0 * i / n);            // 3 oleadas por ciclo
                d[i] = v * swell;
            }
            Normalize(d, 0.9f);
            return d;
        }

        /// <summary>Trino de pajaro: 2 a 4 silabas con barrido de frecuencia y vibrato.</summary>
        public static float[] Bird(int seed)
        {
            var r = new Random(seed);
            var d = Alloc(1.1f);
            int pos = (int)(0.03f * Rate);
            int syllables = 2 + r.Next(3);
            float baseF = 2600f + (float)r.NextDouble() * 1800f;
            for (int s = 0; s < syllables && pos < d.Length - 200; s++)
            {
                int len = (int)((0.07f + (float)r.NextDouble() * 0.07f) * Rate);
                float f0 = baseF * (0.9f + (float)r.NextDouble() * 0.5f), f1 = f0 * (r.NextDouble() < 0.5 ? 1.35f : 0.72f);
                double phase = 0;
                for (int i = 0; i < len && pos + i < d.Length; i++)
                {
                    float k = i / (float)len;
                    float f = f0 + (f1 - f0) * k + (float)Math.Sin(i * 0.05) * 90f;
                    phase += 2.0 * Math.PI * f / Rate;
                    float env = (float)Math.Sin(Math.PI * k);
                    d[pos + i] += (float)Math.Sin(phase) * env * 0.7f;
                }
                pos += len + (int)((0.04f + (float)r.NextDouble() * 0.08f) * Rate);
            }
            Normalize(d, 0.8f);
            return d;
        }

        /// <summary>Grillo: trenes de 3 pulsos cortos sobre una portadora aguda.</summary>
        public static float[] Cricket(int seed)
        {
            var r = new Random(seed);
            var d = Alloc(0.8f);
            float carrier = 4100f + (float)r.NextDouble() * 500f;
            for (int group = 0; group < 2; group++)
                for (int p = 0; p < 3; p++)
                {
                    int start = (int)((0.04f + group * 0.34f + p * 0.065f) * Rate);
                    int len = (int)(0.028f * Rate);
                    for (int i = 0; i < len && start + i < d.Length; i++)
                    {
                        float k = i / (float)len;
                        d[start + i] += (float)Math.Sin(2.0 * Math.PI * carrier * (start + i) / Rate) * (float)Math.Sin(Math.PI * k) * 0.8f;
                    }
                }
            Normalize(d, 0.7f);
            return d;
        }

        // ---------------------------------------------------------------- acciones

        /// <summary>Pisada: tierra (golpe sordo), pasto (roce suave) o piedra (clic seco).</summary>
        public static float[] Footstep(SurfaceKind surface, int seed)
        {
            var r = new Random(seed);
            float vary = 0.9f + (float)r.NextDouble() * 0.2f;
            float[] d;
            float lp = 0f;
            switch (surface)
            {
                case SurfaceKind.Stone:
                    d = Alloc(0.14f);
                    for (int i = 0; i < d.Length; i++)
                    {
                        float t = i / (float)Rate;
                        float click = Noise(r) * (float)Math.Exp(-t / 0.004f);
                        float ring = (float)Math.Sin(2.0 * Math.PI * 1750f * vary * t) * (float)Math.Exp(-t / 0.03f) * 0.5f
                                   + (float)Math.Sin(2.0 * Math.PI * 640f * vary * t) * (float)Math.Exp(-t / 0.05f) * 0.5f;
                        d[i] = click * 0.8f + ring;
                    }
                    break;
                case SurfaceKind.Grass:
                    d = Alloc(0.24f);
                    for (int i = 0; i < d.Length; i++)
                    {
                        float t = i / (float)Rate;
                        float x = Noise(r);
                        lp += (x - lp) * 0.3f;
                        float hiss = x - lp;                                   // pasa-altos: roce de hojas
                        float env = (float)Math.Min(1.0, t / 0.02) * (float)Math.Exp(-t / 0.07);
                        d[i] = hiss * env;
                    }
                    break;
                default:                                                      // Dirt
                    d = Alloc(0.2f);
                    for (int i = 0; i < d.Length; i++)
                    {
                        float t = i / (float)Rate;
                        lp += (Noise(r) - lp) * 0.13f;
                        float thud = (float)Math.Sin(2.0 * Math.PI * 85f * vary * t) * (float)Math.Exp(-t / 0.05f);
                        d[i] = lp * 3.2f * (float)Math.Exp(-t / 0.045f) + thud * 0.7f;
                    }
                    break;
            }
            Normalize(d, 0.85f);
            return d;
        }

        /// <summary>Pala entrando en la tierra: raspado granuloso (el golpe sordo lo pone la excavacion).</summary>
        public static float[] Shovel(int seed)
        {
            var r = new Random(seed);
            var d = Alloc(0.3f);
            float lp = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float x = Noise(r);
                lp += (x - lp) * 0.22f;
                float grit = x - lp * 0.6f;
                float crackle = r.NextDouble() < 0.012 ? Noise(r) * 1.8f : 0f;      // granos sueltos
                float env = (float)Math.Min(1.0, t / 0.03) * (float)Math.Exp(-t / 0.11);
                d[i] = (grit * 0.7f + crackle) * env;
            }
            Normalize(d, 0.8f);
            return d;
        }

        /// <summary>Barrido de cepillo: ruido de banda media con tres pasadas (modulacion ~9 Hz).</summary>
        public static float[] Brush(int seed)
        {
            var r = new Random(seed);
            var d = Alloc(0.5f);
            float lpA = 0f, lpB = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float x = Noise(r);
                lpA += (x - lpA) * 0.5f;
                lpB += (x - lpB) * 0.08f;
                float band = lpA - lpB;                                             // banda media/alta
                float stroke = 0.55f + 0.45f * (float)Math.Sin(2.0 * Math.PI * 4.5 * t);   // dos pasadas
                d[i] = band * stroke * Fade(i, d.Length, 1200, 3500);
            }
            Normalize(d, 0.7f);
            return d;
        }

        static float[] Metal(float baseHz, float seconds, int seed, float brightness)
        {
            var r = new Random(seed);
            var d = Alloc(seconds);
            float[] ratio = { 1f, 2.76f, 5.4f, 8.93f };
            float[] amp = { 1f, 0.6f * brightness, 0.35f * brightness, 0.2f * brightness };
            float[] decay = { 0.55f, 0.3f, 0.16f, 0.08f };
            float detune = 0.98f + (float)r.NextDouble() * 0.04f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float v = i < 70 ? Noise(r) * 0.6f * (1f - i / 70f) : 0f;           // transitorio del golpe
                for (int p = 0; p < 4; p++)
                    v += (float)Math.Sin(2.0 * Math.PI * baseHz * detune * ratio[p] * t) * amp[p] * (float)Math.Exp(-t / decay[p]);
                d[i] = v;
            }
            return d;
        }

        /// <summary>"Clink" metalico al descubrir una reliquia (campana corta con armonicos inarmonicos).</summary>
        public static float[] Clink(int seed)
        {
            var d = Metal(1180f, 1.2f, seed, 1f);
            Normalize(d, 0.85f);
            return d;
        }

        /// <summary>Moneda al vender: dos tintineos agudos seguidos.</summary>
        public static float[] Coin(int seed)
        {
            var a = Metal(2100f, 0.5f, seed, 0.8f);
            var b = Metal(2800f, 0.5f, seed + 1, 0.8f);
            var d = Alloc(0.62f);
            for (int i = 0; i < a.Length && i < d.Length; i++) d[i] += a[i] * 0.8f;
            int off = (int)(0.07f * Rate);
            for (int i = 0; i < b.Length && off + i < d.Length; i++) d[off + i] += b[i];
            Normalize(d, 0.8f);
            return d;
        }
    }
}
