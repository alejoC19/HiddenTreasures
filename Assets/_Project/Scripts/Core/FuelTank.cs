using System;

namespace HuesoOxido.Core
{
    public sealed class FuelTank
    {
        public FuelTank(float capacityLiters, float startLevel = -1f)
        {
            CapacityLiters = capacityLiters;
            Level = startLevel < 0f ? capacityLiters : Math.Min(startLevel, capacityLiters);
        }

        public float CapacityLiters { get; private set; }
        public float Level { get; private set; }
        public float Fraction => CapacityLiters <= 0f ? 0f : Level / CapacityLiters;
        public bool IsEmpty => Level <= 0.001f;
        public event Action Changed;

        public void SetCapacity(float liters)
        {
            CapacityLiters = Math.Max(1f, liters);
            Level = Math.Min(Level, CapacityLiters);
            Changed?.Invoke();
        }

        public void SetLevel(float liters) { Level = Math.Max(0f, Math.Min(CapacityLiters, liters)); Changed?.Invoke(); }

        public float Consume(float liters)
        {
            float used = Math.Min(Level, Math.Max(0f, liters));
            Level -= used;
            Changed?.Invoke();
            return used;
        }

        /// <summary>Agrega combustible y devuelve cuanto entro realmente.</summary>
        public float Refill(float liters)
        {
            float added = Math.Min(CapacityLiters - Level, Math.Max(0f, liters));
            Level += added;
            if (added > 0f) Changed?.Invoke();
            return added;
        }
    }
}
