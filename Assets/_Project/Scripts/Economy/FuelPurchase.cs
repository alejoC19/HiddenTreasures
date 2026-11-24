using System;
using HuesoOxido.Core;

namespace HuesoOxido.Economy
{
    public static class FuelPurchase
    {
        public const int PricePerLiter = 1;

        /// <summary>Carga hasta llenar o hasta donde alcance el dinero. Devuelve (litros, costo).</summary>
        public static (float liters, int cost) Buy(FuelTank tank, Wallet wallet)
        {
            float needed = tank.CapacityLiters - tank.Level;
            if (needed <= 0.05f) return (0f, 0);
            int affordableLiters = Math.Min((int)Math.Ceiling(needed), wallet.Money / PricePerLiter);
            if (affordableLiters <= 0) return (0f, 0);
            int cost = affordableLiters * PricePerLiter;
            if (!wallet.TrySpend(cost)) return (0f, 0);
            float added = tank.Refill(affordableLiters);
            return (added, cost);
        }

        public static int CostToFill(FuelTank tank) => (int)Math.Ceiling(Math.Max(0f, tank.CapacityLiters - tank.Level)) * PricePerLiter;
    }
}
