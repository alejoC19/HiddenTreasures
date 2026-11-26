using System;

namespace HuesoOxido.Core
{
    public enum SoilKind { Soft, Hard, Rooty, Rocky }

    /// <summary>
    /// Que cambia en el juego segun lo que el explorador lleva equipado. Cada pieza tiene un efecto concreto que cambia una DECISION
    /// de exploracion (adonde ir, cuando, con que herramienta), no un simple numero. Logica pura; el juego solo consulta estos valores.
    /// </summary>
    public sealed class EquipmentEffects
    {
        // --- Cuerpo (alimenta NeedsContext) ---
        public float Warmth;                    // 0..1: ropa termica 1,0; ligera 0,2; ninguna 0,1
        public float HeatProtection;            // 0..1: ropa ligera 0,6; termica 0
        // --- Movimiento y orientacion ---
        public float NightVisionMeters;         // sin luz el explorador ve poco; con linterna alcanza para leer el terreno
        public float MaxClimbSlopeDegrees;      // pendiente maxima transitable a pie (cuerda la amplia)
        public bool CanNavigate;                // brujula o GPS: no se pierde la orientacion (la niebla y la noche penalizan menos)
        public bool HasGps;                     // marca posicion propia y la del vehiculo en el mapa
        // --- Seguridad ---
        public bool CanTreatInjury;             // botiquin
        public float CollapseLossFraction;      // parte de la mochila que se pierde al desmayarse (la radio la reduce: viene el rescate)
        public bool CanClearVegetation;         // machete: abre caminos en vegetacion densa
        public bool InsectProtection;           // repelente
        // --- Hallazgos ---
        public float CleanQuality;              // 0,2 a mano; 0,7 con cepillo de arqueologo
        public bool CanDocument;                // camara: documentar sitios y piezas (contratos de documentacion, reputacion)

        public static EquipmentEffects From(Loadout l)
        {
            var e = new EquipmentEffects
            {
                Warmth = 0.1f, HeatProtection = 0f, NightVisionMeters = 7f, MaxClimbSlopeDegrees = 32f, CollapseLossFraction = 0.4f, CleanQuality = 0.2f,
            };
            if (l == null) return e;
            if (l.Has(EquipmentKind.WarmClothing)) { e.Warmth = 1f; e.HeatProtection = 0f; }
            else if (l.Has(EquipmentKind.LightClothing)) { e.Warmth = 0.2f; e.HeatProtection = 0.6f; }
            if (l.Has(EquipmentKind.Flashlight)) e.NightVisionMeters = 24f;
            if (l.Has(EquipmentKind.Rope)) e.MaxClimbSlopeDegrees = 42f;
            e.CanNavigate = l.Has(EquipmentKind.Compass) || l.Has(EquipmentKind.Gps);
            e.HasGps = l.Has(EquipmentKind.Gps);
            e.CanTreatInjury = l.Has(EquipmentKind.FirstAid);
            if (l.Has(EquipmentKind.Radio)) e.CollapseLossFraction = 0.2f;
            e.CanClearVegetation = l.Has(EquipmentKind.Machete);
            e.InsectProtection = l.Has(EquipmentKind.InsectRepellent);
            if (l.Has(EquipmentKind.Brush)) e.CleanQuality = 0.7f;
            e.CanDocument = l.Has(EquipmentKind.Camera);
            return e;
        }

        /// <summary>Velocidad de excavacion segun herramienta y suelo: la pala gana en tierra blanda, el pico en suelo duro, con raices o con piedra.</summary>
        public static float DigMultiplier(Loadout l, SoilKind soil)
        {
            bool shovel = l != null && l.Has(EquipmentKind.Shovel), pick = l != null && l.Has(EquipmentKind.Pick);
            if (!shovel && !pick) return 0.3f;       // con las manos
            switch (soil)
            {
                case SoilKind.Soft: return pick ? 0.8f : 1.0f;
                case SoilKind.Hard: return pick ? 1.0f : 0.6f;
                case SoilKind.Rooty: return pick ? 1.0f : 0.5f;
                default: return pick ? 0.9f : 0.4f;   // Rocky
            }
        }

        /// <summary>Cuanto se ve a distancia segun la hora: de dia todo el alcance, de noche solo el de la luz propia.</summary>
        public float VisibilityMeters(float dayAmount, float dayRange) => NightVisionMeters + (Math.Max(dayRange, NightVisionMeters) - NightVisionMeters) * Math.Max(0f, Math.Min(1f, dayAmount));
    }
}
