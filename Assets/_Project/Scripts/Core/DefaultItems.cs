using System.Collections.Generic;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Catalogo inicial del MVP (bosque con ruinas). Los objetos "documentados" son tipos reales de hallazgo
    /// arqueologico/paleontologico; lo ficticio esta marcado con IsFiction y se explica en SourceNote.
    /// </summary>
    public static class DefaultItems
    {
        public static ItemCatalog CreateCatalog()
        {
            var defs = new List<ItemDefinition>();
            foreach (var sp in Specs())
                defs.Add(ItemDefinition.Create(sp.Id, sp.DisplayName, sp.Description, sp.SourceNote, sp.IsFiction, sp.Rarity, sp.Category, sp.WeightKg, sp.BaseValue));
            return ItemCatalog.CreateFrom(defs);
        }

        /// <summary>Datos puros (sin motor): se pueden testear fuera de Unity.</summary>
        public static List<ItemSpec> Specs()
        {
            const string real = "Tipo de hallazgo real y documentado en la arqueología y la paleontología europeas; la pieza concreta es genérica.";
            var l = new List<ItemSpec>
            {
                new ItemSpec("flint_arrowhead", "Punta de flecha de sílex",
                    "Punta tallada por presión en sílex. Típica de cazadores del Neolítico.", real, false,
                    Rarity.Common, FindCategory.Archaeology, 0.05f, 18),
                new ItemSpec("pottery_shard", "Fragmento de cerámica",
                    "Borde de una vasija de arcilla cocida con decoración incisa.", real, false,
                    Rarity.Common, FindCategory.Archaeology, 1.0f, 12),
                new ItemSpec("iron_nail", "Clavo de hierro forjado",
                    "Clavo de sección cuadrada, forjado a mano. Usado en construcciones antiguas.", real, false,
                    Rarity.Common, FindCategory.History, 0.3f, 8),
                new ItemSpec("shark_tooth", "Diente de tiburón fósil",
                    "Diente triangular de tiburón, mineralizado. Común en sedimentos marinos antiguos.", real, false,
                    Rarity.Common, FindCategory.Fossil, 0.15f, 22),
                new ItemSpec("bronze_fibula", "Fíbula de bronce",
                    "Broche para sujetar la ropa, de la Edad del Hierro.", real, false,
                    Rarity.Uncommon, FindCategory.Archaeology, 0.12f, 70),
                new ItemSpec("ammonite", "Amonite fósil",
                    "Concha espiral de un cefalópodo extinto del Mesozoico.", real, false,
                    Rarity.Uncommon, FindCategory.Fossil, 3.5f, 85),
                new ItemSpec("medieval_key", "Llave medieval de hierro",
                    "Llave de hierro con ojal circular y paleta simple.", real, false,
                    Rarity.Uncommon, FindCategory.History, 0.6f, 60),
                new ItemSpec("roman_denarius", "Denario romano de plata",
                    "Moneda de plata del Imperio romano, acuñada en torno al siglo II.", real, false,
                    Rarity.Rare, FindCategory.History, 0.004f, 260),
                new ItemSpec("bronze_axe", "Hacha de bronce",
                    "Hacha de talón de la Edad del Bronce, con filo desgastado.", real, false,
                    Rarity.Rare, FindCategory.Archaeology, 2.2f, 340),
                new ItemSpec("expedition_journal", "Diario de la expedición Valdés",
                    "Cuaderno de cuero con anotaciones sobre una cámara oculta bajo las ruinas.",
                    "Ficción del juego: la expedición Valdés no existió.", true,
                    Rarity.Epic, FindCategory.History, 0.35f, 520),
                new ItemSpec("trilobite_slab", "Losa con trilobites",
                    "Roca con varios trilobites del Paleozoico, un grupo real de artrópodos marinos extintos.", real, false,
                    Rarity.Epic, FindCategory.Fossil, 7.5f, 640),
                new ItemSpec("golden_torc", "Torque de oro",
                    "Collar rígido de oro, símbolo de rango en culturas celtas.", real, false,
                    Rarity.Legendary, FindCategory.Archaeology, 1.4f, 1500),
            };
            return l;
        }
    }
}
