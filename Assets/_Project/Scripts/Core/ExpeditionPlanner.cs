using System;
using System.Collections.Generic;

namespace HuesoOxido.Core
{
    public enum RequirementLevel { Required, Recommended }

    public sealed class ReadinessItem
    {
        public string Id, Text;
        public RequirementLevel Level;
        public bool Met;
    }

    /// <summary>Resultado de "¿estoy listo para salir?": lista de requisitos y recomendaciones con su estado, y una estimacion de riesgo.</summary>
    public sealed class ExpeditionReadiness
    {
        public readonly List<ReadinessItem> Items = new List<ReadinessItem>();
        public bool CanDepart { get { foreach (var i in Items) if (i.Level == RequirementLevel.Required && !i.Met) return false; return true; } }
        /// <summary>0 (sin riesgo extra) a 1 (muy mal preparado). Es una guia para el jugador, no un numero oculto del juego.</summary>
        public float Risk
        {
            get
            {
                float r = 0f;
                foreach (var i in Items) if (!i.Met) r += i.Level == RequirementLevel.Required ? 0.35f : 0.1f;
                return r > 1f ? 1f : r;
            }
        }
        public int Missing(RequirementLevel level) { int n = 0; foreach (var i in Items) if (i.Level == level && !i.Met) n++; return n; }
    }

    /// <summary>
    /// Deriva requisitos de una region a partir de su perfil (temperatura, etc.), sin tablas por region: una region nueva
    /// ya tiene requisitos coherentes al definir su clima. Logica pura.
    /// </summary>
    public static class ExpeditionPlanner
    {
        public static ExpeditionReadiness Evaluate(BiomeProfile region, Loadout load, float fuelFraction, float departureHour)
        {
            var r = new ExpeditionReadiness();
            void Add(string id, string text, RequirementLevel lvl, bool met) => r.Items.Add(new ReadinessItem { Id = id, Text = text, Level = lvl, Met = met });

            Add("detector", "Detector de metales equipado", RequirementLevel.Required, load.Has(EquipmentKind.Detector));
            Add("dig", "Pala o pico equipado", RequirementLevel.Required, load.Has(EquipmentKind.Shovel) || load.Has(EquipmentKind.Pick));
            Add("fuel_return", "Combustible para volver (≥ 25 %)", RequirementLevel.Required, fuelFraction >= 0.25f);
            Add("fuel_comfort", "Tanque con margen (≥ 50 %)", RequirementLevel.Recommended, fuelFraction >= 0.5f);

            float waterNeeded = region.Temperature > 28f ? 2f : 1f;
            Add("water", "Agua: " + waterNeeded.ToString("0.#") + " L o más", RequirementLevel.Required, load.WaterLiters + 1e-4f >= waterNeeded);
            Add("food", "Al menos una ración", RequirementLevel.Recommended, load.Rations >= 1);
            Add("water_long", "Agua ≥ 3 L si querés explorar más de una hora", RequirementLevel.Recommended, load.WaterLiters >= 3f);
            if (region.RadioCoverage < 0.6f) Add("radio", "Radio: la cobertura es baja y podés necesitar pedir ayuda", RequirementLevel.Recommended, load.Has(EquipmentKind.Radio));

            if (region.Temperature <= 5f)
            {
                Add("warm", "Ropa térmica (hace " + region.Temperature.ToString("0") + " °C)", RequirementLevel.Required, load.Has(EquipmentKind.WarmClothing));
                Add("firstaid_cold", "Botiquín", RequirementLevel.Recommended, load.Has(EquipmentKind.FirstAid));
            }
            else if (region.Temperature >= 30f)
            {
                Add("light", "Ropa ligera (hace " + region.Temperature.ToString("0") + " °C)", RequirementLevel.Recommended, load.Has(EquipmentKind.LightClothing));
                Add("water_extra", "Agua extra para el calor (≥ 3 L)", RequirementLevel.Recommended, load.WaterLiters >= 3f);
            }
            bool night = departureHour >= 18f || departureHour < 5f;
            Add("light_night", night ? "Linterna (salís de noche)" : "Linterna (por si anochece)", night ? RequirementLevel.Required : RequirementLevel.Recommended, load.Has(EquipmentKind.Flashlight));
            Add("navigation", "Brújula o GPS", RequirementLevel.Recommended, load.Has(EquipmentKind.Compass) || load.Has(EquipmentKind.Gps));
            return r;
        }
    }

    public enum ExpeditionPhase { None, Preparing, InProgress, Returned }

    public sealed class ExpeditionSummary
    {
        public string RegionId;
        public int Finds, TotalValue;
        public float Hours;
        public float RiskAtStart;
        public bool Success => Finds > 0;
    }

    /// <summary>Estado explicito de la expedicion en curso. Un solo lugar que responde "¿estoy en una salida y como va?".</summary>
    public sealed class ExpeditionLog
    {
        public ExpeditionPhase Phase { get; private set; } = ExpeditionPhase.None;
        public string RegionId { get; private set; }
        public int Finds { get; private set; }
        public int TotalValue { get; private set; }
        public float StartHour { get; private set; }
        public float RiskAtStart { get; private set; }
        public event Action<ExpeditionPhase> PhaseChanged;

        public void BeginPreparing(string regionId) { RegionId = regionId; Phase = ExpeditionPhase.Preparing; Finds = 0; TotalValue = 0; PhaseChanged?.Invoke(Phase); }

        /// <summary>Sale de viaje. Falla si la preparacion no cumple los requisitos obligatorios.</summary>
        public bool Depart(ExpeditionReadiness readiness, float absoluteHour)
        {
            if (Phase != ExpeditionPhase.Preparing || readiness == null || !readiness.CanDepart) return false;
            StartHour = absoluteHour; RiskAtStart = readiness.Risk; Phase = ExpeditionPhase.InProgress;
            PhaseChanged?.Invoke(Phase);
            return true;
        }

        public void RecordFind(int value) { if (Phase != ExpeditionPhase.InProgress) return; Finds++; TotalValue += Math.Max(0, value); }

        public ExpeditionSummary Return(float absoluteHour)
        {
            if (Phase != ExpeditionPhase.InProgress) return null;
            Phase = ExpeditionPhase.Returned;
            var s = new ExpeditionSummary { RegionId = RegionId, Finds = Finds, TotalValue = TotalValue, Hours = Math.Max(0f, absoluteHour - StartHour), RiskAtStart = RiskAtStart };
            PhaseChanged?.Invoke(Phase);
            return s;
        }

        public void Reset() { Phase = ExpeditionPhase.None; RegionId = null; Finds = 0; TotalValue = 0; PhaseChanged?.Invoke(Phase); }
    }
}
