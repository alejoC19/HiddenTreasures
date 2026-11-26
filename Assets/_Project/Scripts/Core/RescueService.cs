using System;

namespace HuesoOxido.Core
{
    public enum RescueStatus { NoRadio, NoSignal, BlockedByWeather, Dispatched }

    /// <summary>Resultado de pedir asistencia por radio: si sale ayuda, cuanto tarda, cuanto cuesta y que se recupera.</summary>
    public struct RescueQuote
    {
        public RescueStatus Status;
        public float EtaHours;                // horas de juego hasta que llega la ayuda
        public int Fee;                       // costo del servicio de rescate
        public float RecoveredItemsFraction;  // fraccion de lo caido que el equipo de rescate trae de vuelta
        public CoordinatePrecision Precision;
        public string Reason;                 // explicacion para el jugador cuando no sale ayuda
    }

    public struct RescueContext
    {
        public float DistanceToCampMeters;
        public float RadioCoverage;           // 0..1 de la region (BiomeProfile.RadioCoverage)
        public float WeatherSeverity;         // 0 despejado .. 1 tormenta fuerte
        public bool Night;
        public bool Collapsed;                // true: el jugador ya se desmayo (hay objetos caidos)
    }

    /// <summary>
    /// La radio permite pedir asistencia a la base. No reduce "castigos": abre una OPCION con costo y condiciones.
    /// - Sin radio no hay llamada. Con radio hace falta cobertura; en regiones remotas puede no haber senal.
    /// - Las coordenadas importan: radio sola = ubicacion aproximada (tarda mas y no garantiza recuperar lo caido);
    ///   radio + GPS = ubicacion precisa (llega antes y recupera todo lo caido).
    /// - Una tormenta fuerte impide la salida del equipo de rescate hasta que amaine.
    /// - Pedir ayuda antes de colapsar (varado, sin combustible, lesionado) evita el desmayo y la perdida de carga.
    /// Logica pura y determinista; la region define la cobertura, el clima define el bloqueo.
    /// </summary>
    public static class RescueService
    {
        public const float StormBlocksAbove = 0.75f;

        public static RescueQuote Request(EquipmentEffects fx, RescueContext c)
        {
            var q = new RescueQuote { Precision = fx.Coordinates };
            if (!fx.CanCallForHelp) { q.Status = RescueStatus.NoRadio; q.Reason = "No llevás radio."; return q; }
            float coverage = c.RadioCoverage - (c.DistanceToCampMeters > 400f ? 0.1f : 0f);
            if (coverage < 0.35f) { q.Status = RescueStatus.NoSignal; q.Reason = "Sin señal en esta zona."; return q; }
            if (c.WeatherSeverity > StormBlocksAbove) { q.Status = RescueStatus.BlockedByWeather; q.Reason = "La tormenta impide que salga el rescate; reintentá cuando amaine."; return q; }

            float eta = 0.4f + c.DistanceToCampMeters / 1200f;                // ~10 min de juego + viaje
            if (fx.Coordinates == CoordinatePrecision.Approximate) eta *= 1.8f;  // hay que buscar en una zona amplia
            if (c.Night) eta *= 1.3f;
            eta *= 1f + c.WeatherSeverity * 0.8f;
            q.EtaHours = eta;
            q.Fee = 40 + (int)Math.Round(c.DistanceToCampMeters / 25f) + (c.Night ? 30 : 0);
            q.RecoveredItemsFraction = !c.Collapsed ? 1f : (fx.Coordinates == CoordinatePrecision.Precise ? 1f : 0.5f);
            q.Status = RescueStatus.Dispatched;
            return q;
        }
    }
}
