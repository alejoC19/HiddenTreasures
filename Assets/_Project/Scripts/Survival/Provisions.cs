using System;
using HuesoOxido.Core;

namespace HuesoOxido.Survival
{
    /// <summary>Autonomia estimada con las provisiones actuales. Sirve para PLANIFICAR en la pantalla de preparacion; no es una cuenta regresiva visible.</summary>
    public struct Autonomy
    {
        public float WaterMinutes, FoodMinutes;
        public float LimitingMinutes => Math.Min(WaterMinutes, FoodMinutes);
        public bool WaterIsLimiting => WaterMinutes <= FoodMinutes;
    }

    /// <summary>
    /// Provisiones: autonomia estimada y consumo automatico. Principio de diseno: con provisiones y preparacion el jugador explora
    /// mientras quiera; el limite lo pone el PESO que decide cargar (1 L = 1 kg) y las fuentes de agua del mapa, no un reloj.
    /// </summary>
    public static class Provisions
    {
        /// <summary>Minutos reales caminando (el ritmo de exploracion) que sostienen el agua y la comida llevadas, en el clima de la region.</summary>
        public static Autonomy Estimate(BiomeProfile region, Loadout load, NeedsModel current = null)
        {
            var fx = EquipmentEffects.From(load);
            var ctx = new NeedsContext { Moving = true, AmbientC = region.Temperature, Warmth = fx.Warmth, HeatProtection = fx.HeatProtection };
            float hydr = current != null ? current.Hydration : 1f, sat = current != null ? current.Satiety : 1f;
            float waterUnits = hydr + load.WaterLiters * NeedsModel.HydrationPerLiter;
            float foodUnits = sat + load.Rations * NeedsModel.SatietyPerRation;
            float hydrPerMin = NeedsModel.HydrationDrainPerHour(ctx) * NeedsModel.GameHoursFromSeconds(60f);
            float satPerMin = NeedsModel.SatietyDrainPerHour(ctx) * NeedsModel.GameHoursFromSeconds(60f);
            return new Autonomy { WaterMinutes = hydrPerMin > 0f ? waterUnits / hydrPerMin : float.MaxValue, FoodMinutes = satPerMin > 0f ? foodUnits / satPerMin : float.MaxValue };
        }

        public const float AutoDrinkBelow = 0.38f, AutoEatBelow = 0.32f;

        /// <summary>
        /// Consumo automatico (opcion activada por defecto): si el jugador lleva agua o raciones, bebe/come solo al cruzar el aviso.
        /// Quien se preparo no tiene que atender las necesidades; quien no llevo nada recibe el aviso suave y debe buscar una fuente o volver.
        /// Devuelve que consumio, para un mensaje breve ("Bebiste un poco de agua").
        /// </summary>
        public static (bool drank, bool ate) AutoConsume(NeedsModel needs, Loadout load)
        {
            bool drank = false, ate = false;
            if (needs.Hydration < AutoDrinkBelow && load.TakeWater(0.5f)) { needs.DrinkLiters(0.5f); drank = true; }
            if (needs.Satiety < AutoEatBelow && load.TakeRation()) { needs.EatRation(); ate = true; }
            return (drank, ate);
        }
    }
}
