using System;
using System.Collections.Generic;

namespace HuesoOxido.Core
{
    public enum EquipmentKind { Detector, Shovel, Pick, Brush, Flashlight, Gps, Compass, Rope, FirstAid, Radio, Camera, WarmClothing, LightClothing, InsectRepellent, Machete }

    /// <summary>Definicion de una pieza de equipo. Datos puros: cada region exige o recomienda tipos, no objetos concretos.</summary>
    public sealed class EquipmentDef
    {
        public string Id, Name, Description;
        public EquipmentKind Kind;
        public float WeightKg;
        public int Cost;
        /// <summary>Calidad de la pieza (1 = basica). Mejores piezas del mismo tipo reemplazan a las basicas.</summary>
        public int Tier = 1;
    }

    public static class DefaultEquipment
    {
        public static IReadOnlyList<EquipmentDef> All { get; } = new[]
        {
            new EquipmentDef { Id = "eq_detector_basic", Name = "Detector básico", Kind = EquipmentKind.Detector, WeightKg = 1.4f, Cost = 0, Description = "El detector de siempre." },
            new EquipmentDef { Id = "eq_shovel", Name = "Pala", Kind = EquipmentKind.Shovel, WeightKg = 1.8f, Cost = 0, Description = "Para excavar suelo blando." },
            new EquipmentDef { Id = "eq_pick", Name = "Pico", Kind = EquipmentKind.Pick, WeightKg = 2.4f, Cost = 60, Description = "Rompe suelo duro y raíces; más lento en tierra blanda." },
            new EquipmentDef { Id = "eq_brush", Name = "Cepillo de arqueólogo", Kind = EquipmentKind.Brush, WeightKg = 0.1f, Cost = 15, Description = "Limpia sin dañar las piezas." },
            new EquipmentDef { Id = "eq_flashlight", Name = "Linterna", Kind = EquipmentKind.Flashlight, WeightKg = 0.3f, Cost = 25, Description = "Imprescindible de noche y bajo tierra." },
            new EquipmentDef { Id = "eq_compass", Name = "Brújula", Kind = EquipmentKind.Compass, WeightKg = 0.1f, Cost = 12, Description = "Orientarse cuando falla todo lo demás." },
            new EquipmentDef { Id = "eq_gps", Name = "GPS", Kind = EquipmentKind.Gps, WeightKg = 0.3f, Cost = 140, Description = "Posición y rutas en regiones sin referencias." },
            new EquipmentDef { Id = "eq_rope", Name = "Cuerda", Kind = EquipmentKind.Rope, WeightKg = 1.2f, Cost = 30, Description = "Para desniveles y recuperaciones." },
            new EquipmentDef { Id = "eq_firstaid", Name = "Botiquín", Kind = EquipmentKind.FirstAid, WeightKg = 0.8f, Cost = 45, Description = "Trata lesiones leves." },
            new EquipmentDef { Id = "eq_radio", Name = "Radio", Kind = EquipmentKind.Radio, WeightKg = 0.6f, Cost = 90, Description = "Comunicación con la base." },
            new EquipmentDef { Id = "eq_camera", Name = "Cámara", Kind = EquipmentKind.Camera, WeightKg = 0.7f, Cost = 110, Description = "Documenta hallazgos y lugares." },
            new EquipmentDef { Id = "eq_clothes_light", Name = "Ropa ligera", Kind = EquipmentKind.LightClothing, WeightKg = 0.8f, Cost = 0, Description = "Fresca y práctica; no abriga." },
            new EquipmentDef { Id = "eq_clothes_warm", Name = "Ropa térmica", Kind = EquipmentKind.WarmClothing, WeightKg = 1.9f, Cost = 160, Description = "Abriga en frío extremo." },
            new EquipmentDef { Id = "eq_repellent", Name = "Repelente", Kind = EquipmentKind.InsectRepellent, WeightKg = 0.2f, Cost = 18, Description = "Protege de insectos." },
            new EquipmentDef { Id = "eq_machete", Name = "Machete", Kind = EquipmentKind.Machete, WeightKg = 0.9f, Cost = 55, Description = "Abre paso en vegetación densa." },
        };

        public static EquipmentDef Get(string id) { foreach (var e in All) if (e.Id == id) return e; return null; }
        public static EquipmentDef FirstOfKind(EquipmentKind k) { foreach (var e in All) if (e.Kind == k) return e; return null; }
    }

    /// <summary>
    /// Lo que el explorador posee y lleva puesto/consigo para una salida, mas las provisiones.
    /// Reglas: un solo detector, una sola herramienta de excavar y una sola ropa equipados a la vez; el resto es independiente.
    /// </summary>
    public sealed class Loadout
    {
        readonly HashSet<string> _owned = new HashSet<string>();
        readonly HashSet<string> _equipped = new HashSet<string>();

        public event Action Changed;
        /// <summary>Agua maxima que se puede llevar (cada litro pesa 1 kg: llevar mas agua permite explorar mas, pero cansa).</summary>
        public const float WaterCapacityLiters = 4f;
        public float WaterLiters { get; private set; }
        public int Rations { get; private set; }

        public IEnumerable<string> Owned => _owned;
        public IEnumerable<string> Equipped => _equipped;
        public bool Owns(string id) => _owned.Contains(id);
        public bool IsEquipped(string id) => _equipped.Contains(id);

        public static bool Exclusive(EquipmentKind k, out int group)
        {
            switch (k)
            {
                case EquipmentKind.Detector: group = 1; return true;
                case EquipmentKind.Shovel: case EquipmentKind.Pick: group = 2; return true;
                case EquipmentKind.WarmClothing: case EquipmentKind.LightClothing: group = 3; return true;
                default: group = 0; return false;
            }
        }

        public void Grant(string id)
        {
            if (DefaultEquipment.Get(id) == null) return;
            _owned.Add(id); Changed?.Invoke();
        }

        /// <summary>Equipa una pieza poseida; si es exclusiva, desequipa la otra del mismo grupo.</summary>
        public bool Equip(string id)
        {
            var def = DefaultEquipment.Get(id);
            if (def == null || !_owned.Contains(id)) return false;
            if (Exclusive(def.Kind, out int g))
                foreach (var other in new List<string>(_equipped)) { var od = DefaultEquipment.Get(other); if (od != null && Exclusive(od.Kind, out int og) && og == g) _equipped.Remove(other); }
            _equipped.Add(id); Changed?.Invoke();
            return true;
        }

        public bool Unequip(string id) { bool r = _equipped.Remove(id); if (r) Changed?.Invoke(); return r; }

        public bool Has(EquipmentKind k)
        {
            foreach (var id in _equipped) { var d = DefaultEquipment.Get(id); if (d != null && d.Kind == k) return true; }
            return false;
        }

        public float EquipmentWeightKg()
        {
            float w = 0f;
            foreach (var id in _equipped) { var d = DefaultEquipment.Get(id); if (d != null) w += d.WeightKg; }
            return w + WaterLiters + Rations * 0.35f;
        }

        public void AddWater(float liters) { WaterLiters = Math.Max(0f, Math.Min(WaterCapacityLiters, WaterLiters + liters)); Changed?.Invoke(); }

        /// <summary>Llena las botellas en el campamento o en una fuente (manantial, arroyo): el agua no es un recurso que se compre.</summary>
        public void RefillWater(float toLiters = WaterCapacityLiters) { WaterLiters = Math.Max(WaterLiters, Math.Min(WaterCapacityLiters, toLiters)); Changed?.Invoke(); }
        public void AddRations(int n) { Rations = Math.Max(0, Rations + n); Changed?.Invoke(); }
        public bool TakeWater(float liters) { if (WaterLiters + 1e-4f < liters) return false; WaterLiters -= liters; Changed?.Invoke(); return true; }
        public bool TakeRation() { if (Rations <= 0) return false; Rations--; Changed?.Invoke(); return true; }

        public (List<string> owned, List<string> equipped, float water, int rations) Snapshot() => (new List<string>(_owned), new List<string>(_equipped), WaterLiters, Rations);

        public void Restore(IEnumerable<string> owned, IEnumerable<string> equipped, float water, int rations)
        {
            _owned.Clear(); _equipped.Clear();
            if (owned != null) foreach (var o in owned) if (DefaultEquipment.Get(o) != null) _owned.Add(o);
            if (equipped != null) foreach (var e in equipped) if (_owned.Contains(e)) _equipped.Add(e);
            WaterLiters = Math.Max(0f, Math.Min(WaterCapacityLiters, water)); Rations = Math.Max(0, rations);
            Changed?.Invoke();
        }

        /// <summary>Equipo con el que empieza una partida nueva: lo basico para el bosque, nada que sobre.</summary>
        public static Loadout Starter()
        {
            var l = new Loadout();
            foreach (var id in new[] { "eq_detector_basic", "eq_shovel", "eq_clothes_light" }) { l.Grant(id); l.Equip(id); }
            l.AddWater(1f); l.AddRations(1);
            return l;
        }
    }
}
