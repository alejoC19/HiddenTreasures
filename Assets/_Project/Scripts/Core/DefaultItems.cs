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
            const string real = "Tipo de hallazgo real y documentado en la arqueología y la paleontología europeas; la pieza concreta es genérica.";
            var l = new List<ItemDefinition>
            {
                ItemDefinition.Create("flint_arrowhead", "Punta de flecha de sílex",
                    "Punta tallada por presión en sílex. Típica de cazadores del Neolítico.", real, false,
                    Rarity.Common, FindCategory.Archaeology, 0.05f, 18),
                ItemDefinition.Create("pottery_shard", "Fragmento de cerámica",
                    "Borde de una vasija de arcilla cocida con decoración incisa.", real, false,
                    Rarity.Common, FindCategory.Archaeology, 0.4f, 12),
                ItemDefinition.Create("iron_nail", "Clavo de hierro forjado",
                    "Clavo de sección cuadrada, forjado a mano. Usado en construcciones antiguas.", real, false,
                    Rarity.Common, FindCategory.History, 0.1f, 8),
                ItemDefinition.Create("shark_tooth", "Diente de tiburón fósil",
                    "Diente triangular de tiburón, mineralizado. Común en sedimentos marinos antiguos.", real, false,
                    Rarity.Common, FindCategory.Fossil, 0.08f, 22),
                ItemDefinition.Create("bronze_fibula", "Fíbula de bronce",
                    "Broche para sujetar la ropa, de la Edad del Hierro.", real, false,
                    Rarity.Uncommon, FindCategory.Archaeology, 0.12f, 70),
                ItemDefinition.Create("ammonite", "Amonite fósil",
                    "Concha espiral de un cefalópodo extinto del Mesozoico.", real, false,
                    Rarity.Uncommon, FindCategory.Fossil, 2.5f, 85),
                ItemDefinition.Create("medieval_key", "Llave medieval de hierro",
                    "Llave de hierro con ojal circular y paleta simple.", real, false,
                    Rarity.Uncommon, FindCategory.History, 0.3f, 60),
                ItemDefinition.Create("roman_denarius", "Denario romano de plata",
                    "Moneda de plata del Imperio romano, acuñada en torno al siglo II.", real, false,
                    Rarity.Rare, FindCategory.History, 0.004f, 260),
                ItemDefinition.Create("bronze_axe", "Hacha de bronce",
                    "Hacha de talón de la Edad del Bronce, con filo desgastado.", real, false,
                    Rarity.Rare, FindCategory.Archaeology, 1.1f, 340),
                ItemDefinition.Create("expedition_journal", "Diario de la expedición Valdés",
                    "Cuaderno de cuero con anotaciones sobre una cámara oculta bajo las ruinas.",
                    "Ficción del juego: la expedición Valdés no existió.", true,
                    Rarity.Epic, FindCategory.History, 0.35f, 520),
                ItemDefinition.Create("trilobite_slab", "Losa con trilobites",
                    "Roca con varios trilobites del Paleozoico, un grupo real de artrópodos marinos extintos.", real, false,
                    Rarity.Epic, FindCategory.Fossil, 6.0f, 640),
                ItemDefinition.Create("golden_torc", "Torque de oro",
                    "Collar rígido de oro, símbolo de rango en culturas celtas.", real, false,
                    Rarity.Legendary, FindCategory.Archaeology, 0.9f, 1500),
            };
            return ItemCatalog.CreateFrom(l);
        }
    }
}
