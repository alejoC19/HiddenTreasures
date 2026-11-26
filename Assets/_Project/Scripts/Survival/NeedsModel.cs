using System;

namespace HuesoOxido.Survival
{
    /// <summary>Lo que le pasa al cuerpo del explorador en un tramo de tiempo. Entrada pura (sin motor) del modelo de necesidades.</summary>
    public struct NeedsContext
    {
        public bool Moving, Sprinting, Working;     // Working: excavando o cargando algo pesado
        public float AmbientC;                      // temperatura del aire (°C)
        public bool Wet;                            // lluvia o agua
        public float Warmth;                        // 0..1 abrigo de la ropa (0 = ropa ligera)
        public float HeatProtection;                // 0..1 proteccion contra calor (sombrero, ropa clara)
        public float LoadFraction;                  // 0..1 peso transportado / capacidad
        public bool Sheltered;                      // carpa, vehiculo o refugio
    }

    /// <summary>Consecuencias de las necesidades sobre el juego. Multiplicadores neutros = 1.</summary>
    public struct NeedsEffects
    {
        public float SpeedMultiplier;
        public float DigMultiplier;
        public float EnergyDrainMultiplier;
        public bool Collapsing;                     // debe desmayarse / pedir refugio (no es muerte)
        public NeedsWarning Warnings;
        public static NeedsEffects Neutral => new NeedsEffects { SpeedMultiplier = 1f, DigMultiplier = 1f, EnergyDrainMultiplier = 1f };
    }

    [Flags]
    public enum NeedsWarning { None = 0, Thirsty = 1, Dehydrated = 2, Hungry = 4, Starving = 8, Cold = 16, Freezing = 32, Hot = 64, Overheated = 128 }

    /// <summary>
    /// Sed, hambre y temperatura corporal. Todo se mide en HORAS DE JUEGO (el reloj del juego decide cuanto dura una hora).
    /// Diseno: pocos numeros, consecuencias claras y graduales, sin muerte permanente ni microgestion constante:
    /// una botella y una racion alcanzan para una expedicion corta, y las condiciones extremas obligan a PLANIFICAR (ropa, agua),
    /// no a comer cada minuto.
    /// </summary>
    public sealed class NeedsModel
    {
        public const float NormalBodyC = 37f;
        // Tasas por hora de juego en condiciones normales (caminando, 15 °C).
        // Calibradas contra el reloj del juego (GameClock: 1 hora de juego = 50 s reales). Objetivo de diseno en el bosque, caminando:
        // aviso de sed a los ~17 min reales, colapso a los ~24 sin beber; hambre, mas lenta (~28 min al aviso).
        // Un litro de agua devuelve 0,7 de hidratacion: el equipo inicial (1 L y una racion) sostiene ~40 min caminando y se rellena en el campamento.
        public const float HydrationPerHour = 0.029f, SatietyPerHour = 0.0195f, HydrationPerLiter = 0.7f, SatietyPerRation = 0.5f;
        public const float WarningHydration = 0.30f, CriticalHydration = 0.10f, WarningSatiety = 0.25f, CriticalSatiety = 0.08f;
        public const float ColdBodyC = 35.5f, FreezingBodyC = 33.5f, HotBodyC = 38.2f, OverheatedBodyC = 39.5f;

        float _hydration = 1f, _satiety = 1f, _bodyC = NormalBodyC;

        public event Action Changed;
        public event Action Collapsed;

        public float Hydration => _hydration;
        public float Satiety => _satiety;
        public float BodyTemperatureC => _bodyC;

        /// <summary>Temperatura de equilibrio del cuerpo para un contexto (hacia donde tiende).</summary>
        public static float EquilibriumC(NeedsContext c)
        {
            float activity = c.Sprinting ? 1.2f : (c.Working ? 0.8f : (c.Moving ? 0.5f : 0f));
            float insulation = 0.25f + 0.75f * Clamp01(c.Warmth);
            if (c.Sheltered) insulation += 0.35f;
            float coldGap = Math.Max(0f, 20f - c.AmbientC);          // cuanto frio hace respecto de 20 °C
            float heatGap = Math.Max(0f, c.AmbientC - 26f);
            float eq = NormalBodyC - coldGap * 0.07f / insulation + activity * 0.45f;
            eq += heatGap * 0.11f * (1f - 0.6f * Clamp01(c.HeatProtection));
            if (c.Wet) eq -= 1.2f * (1f - 0.5f * Clamp01(c.Warmth));
            return Math.Max(27f, Math.Min(42f, eq));
        }

        public static float HydrationDrainPerHour(NeedsContext c)
        {
            float rate = HydrationPerHour;
            if (c.Sprinting) rate *= 2.4f; else if (c.Working) rate *= 1.7f; else if (c.Moving) rate *= 1.2f; else rate *= 0.6f;
            float heat = Math.Max(0f, c.AmbientC - 24f);
            rate *= 1f + heat * 0.07f * (1f - 0.4f * Clamp01(c.HeatProtection));
            rate *= 1f + 0.5f * Clamp01(c.LoadFraction);
            return rate;
        }

        public static float SatietyDrainPerHour(NeedsContext c)
        {
            float rate = SatietyPerHour;
            if (c.Sprinting) rate *= 1.8f; else if (c.Working) rate *= 1.5f; else if (c.Moving) rate *= 1.15f; else rate *= 0.7f;
            if (c.AmbientC < 5f) rate *= 1f + (5f - c.AmbientC) * 0.03f;   // el frio consume
            return rate;
        }

        /// <summary>Convierte segundos reales en horas de juego segun el reloj del juego.</summary>
        public static float GameHoursFromSeconds(float realSeconds) => realSeconds * 24f / GameClock.RealSecondsPerDay;

        public void Tick(float gameHours, NeedsContext c)
        {
            if (gameHours <= 0f) return;
            bool wasCollapsing = Evaluate().Collapsing;
            _hydration = Clamp01(_hydration - HydrationDrainPerHour(c) * gameHours);
            _satiety = Clamp01(_satiety - SatietyDrainPerHour(c) * gameHours);
            // El cuerpo tiende al equilibrio con inercia (media vida de ~1,2 h): un chubasco corto no congela a nadie.
            float k = 1f - (float)Math.Exp(-gameHours / 1.2f);
            _bodyC += (EquilibriumC(c) - _bodyC) * k;
            Changed?.Invoke();
            if (!wasCollapsing && Evaluate().Collapsing) Collapsed?.Invoke();
        }

        public void DrinkLiters(float liters) => Drink(liters * HydrationPerLiter);
        public void EatRation() => Eat(SatietyPerRation);

        public void Drink(float amount) { _hydration = Clamp01(_hydration + Math.Max(0f, amount)); Changed?.Invoke(); }
        public void Eat(float amount) { _satiety = Clamp01(_satiety + Math.Max(0f, amount)); Changed?.Invoke(); }

        /// <summary>Calor externo (fogata, refugio calefaccionado): sube la temperatura corporal hacia lo normal.</summary>
        public void Warm(float degrees) { _bodyC = Math.Min(NormalBodyC, _bodyC + Math.Max(0f, degrees)); Changed?.Invoke(); }

        public void RestoreAll() { _hydration = 1f; _satiety = 1f; _bodyC = NormalBodyC; Changed?.Invoke(); }

        public NeedsEffects Evaluate()
        {
            var e = NeedsEffects.Neutral;
            if (_hydration < WarningHydration) { e.Warnings |= NeedsWarning.Thirsty; e.EnergyDrainMultiplier *= 1.25f; e.DigMultiplier *= 0.92f; }
            if (_hydration < CriticalHydration) { e.Warnings |= NeedsWarning.Dehydrated; e.EnergyDrainMultiplier *= 1.4f; e.SpeedMultiplier *= 0.88f; e.DigMultiplier *= 0.85f; }
            if (_satiety < WarningSatiety) { e.Warnings |= NeedsWarning.Hungry; e.DigMultiplier *= 0.92f; }
            if (_satiety < CriticalSatiety) { e.Warnings |= NeedsWarning.Starving; e.EnergyDrainMultiplier *= 1.3f; e.SpeedMultiplier *= 0.92f; }
            if (_bodyC < ColdBodyC) { e.Warnings |= NeedsWarning.Cold; e.DigMultiplier *= 0.88f; e.EnergyDrainMultiplier *= 1.15f; }
            if (_bodyC < FreezingBodyC) { e.Warnings |= NeedsWarning.Freezing; e.SpeedMultiplier *= 0.85f; e.EnergyDrainMultiplier *= 1.5f; }
            if (_bodyC > HotBodyC) { e.Warnings |= NeedsWarning.Hot; e.EnergyDrainMultiplier *= 1.2f; }
            if (_bodyC > OverheatedBodyC) { e.Warnings |= NeedsWarning.Overheated; e.SpeedMultiplier *= 0.88f; e.EnergyDrainMultiplier *= 1.5f; }
            // Colapso: cuerpo en extremo (no es muerte: el director lo trata como el desmayo actual, con perdida de parte de la carga).
            e.Collapsing = _hydration <= 0.001f || _bodyC < 32f || _bodyC > 41f;
            return e;
        }

        public (float hydration, float satiety, float bodyC) Snapshot() => (_hydration, _satiety, _bodyC);

        public void Restore(float hydration, float satiety, float bodyC)
        {
            _hydration = Clamp01(hydration); _satiety = Clamp01(satiety);
            _bodyC = Math.Max(28f, Math.Min(43f, bodyC <= 0f ? NormalBodyC : bodyC));
            Changed?.Invoke();
        }

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
