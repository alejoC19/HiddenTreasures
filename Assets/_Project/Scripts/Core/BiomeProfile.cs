using System;
using System.Collections.Generic;

namespace HuesoOxido.Core
{
    /// <summary>Color lineal sencillo para que los perfiles de bioma sean logica pura (sin UnityEngine).</summary>
    public readonly struct Rgb
    {
        public readonly float R, G, B;
        public Rgb(float r, float g, float b) { R = r; G = g; B = b; }
    }

    /// <summary>Estilo de vegetacion de arboles: define que mallas y materiales usa el constructor del mundo.</summary>
    public enum TreeStyle { ForestMix, Cactus, SnowPine, SwampDead }

    /// <summary>
    /// Todo lo que cambia de un ecosistema a otro: relieve, texturas del suelo, vegetacion, atmosfera y permiso de excavacion.
    /// Datos puros: el mismo perfil + la misma semilla generan siempre el mismo mundo. El bosque conserva los valores originales.
    /// </summary>
    public sealed class BiomeProfile
    {
        public string Id, Name, Description;
        public string ZoneId, PermitId;
        public int PermitCost;
        /// <summary>Prefijo de los ids de sitios enterrados (para que cada bioma tenga los suyos en el guardado).</summary>
        public string SitePrefix;

        // ---- Relieve ----
        public float BaseHeight = 4f, Amplitude = 11f, Frequency = 3.1f;
        /// <summary>0..1: mezcla de ruido de cresta (aristas afiladas, canones).</summary>
        public float Ridge;
        /// <summary>Altura de cada escalon de meseta; 0 = sin terrazas.</summary>
        public float MesaStep;
        /// <summary>0..1: lo duro que es el borde de cada escalon.</summary>
        public float MesaSharpness = 0.6f;
        public float EdgeRise = 20f;

        // ---- Suelo (nombres en TextureLibrary, sin sufijo _diff/_nor) ----
        public string Ground0 = "forest_floor", Ground1 = "grass", GroundRoad = "road", GroundRuin = "red_earth", RockTexture = "rock";
        /// <summary>Peso de la capa secundaria (pasto, arena clara, nieve limpia) sobre la base.</summary>
        public float Ground1Weight = 1f;
        /// <summary>Tintes del shader de suelo (capa base y secundaria) y color base del macro: sin ellos todo sale verde de bosque.</summary>
        public Rgb GroundTint0 = new Rgb(0.92f, 1f, 0.82f), GroundTint1 = new Rgb(0.9f, 1f, 0.75f), MacroBase = new Rgb(0.92f, 1.10f, 0.85f);
        /// <summary>Fraccion del mapa cubierta de agua (0 = sin agua). El nivel se calcula del relieve sin ahogar el camino ni los campamentos.</summary>
        public float WaterFraction;
        /// <summary>Textura y tinte de la piedra de las ruinas (nombre en TextureLibrary).</summary>
        public string RuinTexture = "ruin_stone";
        public Rgb RuinTint = new Rgb(1.35f, 1.3f, 1.2f);

        // ---- Vegetacion ----
        public TreeStyle Trees = TreeStyle.ForestMix;
        public int MaxTrees = 1500;
        public float TreeScale = 1f;
        /// <summary>Multiplicador de la densidad del sotobosque (pasto, helechos, arbustos).</summary>
        public float UnderstoryDensity = 1f;
        /// <summary>Multiplicador de color del pasto y los arbustos (blanco = sin cambio).</summary>
        public Rgb VegTint = new Rgb(1f, 1f, 1f);
        public int SeedOffset;
        public int RockCount = 120;
        public float RockScale = 1f;

        // ---- Atmosfera ----
        public Rgb SkyDay = new Rgb(0.62f, 0.72f, 0.90f);
        public Rgb FogDay = new Rgb(0.62f, 0.70f, 0.76f);
        public Rgb FogDusk = new Rgb(0.85f, 0.58f, 0.40f);
        public Rgb AmbientGround = new Rgb(0.26f, 0.22f, 0.16f);
        public Rgb Skirt = new Rgb(0.10f, 0.17f, 0.09f);
        public float FogMultiplier = 1f;
        public float SunIntensity = 1f;
        public float Temperature = 12f;
        public float WindStrength = 0.4f;

        // ---- Mapa ----
        public Rgb MapLow = new Rgb(0.24f, 0.34f, 0.18f), MapHigh = new Rgb(0.58f, 0.52f, 0.36f), MapVegetation = new Rgb(0.10f, 0.22f, 0.12f);

        public bool IsDefault => Id == Biomes.ForestId;
    }

    /// <summary>Registro de ecosistemas disponibles.</summary>
    public static class Biomes
    {
        public const string ForestId = "forest_ruins", DesertId = "desert_canyon", SnowId = "snow_pines", SwampId = "swamp_mire";

        public static readonly BiomeProfile Forest = new BiomeProfile
        {
            Id = ForestId, Name = "Bosque de las Ruinas", ZoneId = "forest_ruins", PermitId = "permit_forest_ruins", PermitCost = 40,
            SitePrefix = "forest_site_",
            Description = "Bosque denso de coníferas con ruinas cubiertas de musgo. Terreno tranquilo para empezar."
        };

        public static readonly BiomeProfile Desert = new BiomeProfile
        {
            Id = DesertId, Name = "Cañón Rojo", ZoneId = "desert_canyon", PermitId = "permit_desert_canyon", PermitCost = 180,
            SitePrefix = "desert_site_", SeedOffset = 7919,
            Description = "Mesetas de arenisca, cactus y un templo medio enterrado en la arena. Calor seco y mucho polvo.",
            BaseHeight = 6f, Amplitude = 17f, Frequency = 2.4f, Ridge = 0.55f, MesaStep = 6.5f, MesaSharpness = 0.82f, EdgeRise = 26f,
            Ground0 = "desert_sand", Ground1 = "desert_dry", GroundRoad = "desert_road", GroundRuin = "desert_ruin", RockTexture = "desert_rock", RuinTexture = "desert_ruin", RuinTint = new Rgb(1.25f, 1.15f, 1.0f),
            Ground1Weight = 0.9f, GroundTint0 = new Rgb(1.0f, 0.97f, 0.92f), GroundTint1 = new Rgb(1.0f, 0.97f, 0.9f), MacroBase = new Rgb(1.08f, 1.0f, 0.88f),
            Trees = TreeStyle.Cactus, MaxTrees = 230, TreeScale = 1f, UnderstoryDensity = 0.35f, VegTint = new Rgb(1.55f, 1.0f, 0.42f), RockCount = 200, RockScale = 1.5f,
            SkyDay = new Rgb(0.46f, 0.66f, 0.95f), FogDay = new Rgb(0.86f, 0.72f, 0.56f), FogDusk = new Rgb(0.95f, 0.50f, 0.28f),
            AmbientGround = new Rgb(0.42f, 0.27f, 0.18f), Skirt = new Rgb(0.46f, 0.26f, 0.15f),
            FogMultiplier = 0.55f, SunIntensity = 1.12f, Temperature = 34f, WindStrength = 0.8f,
            MapLow = new Rgb(0.62f, 0.40f, 0.24f), MapHigh = new Rgb(0.84f, 0.62f, 0.38f), MapVegetation = new Rgb(0.36f, 0.42f, 0.22f)
        };

        public static readonly BiomeProfile Snow = new BiomeProfile
        {
            Id = SnowId, Name = "Pinar Nevado", ZoneId = "snow_pines", PermitId = "permit_snow_pines", PermitCost = 320,
            SitePrefix = "snow_site_", SeedOffset = 15838,
            Description = "Pinos cargados de nieve, ruinas congeladas y ventiscas. Hace frío: el combustible rinde menos.",
            BaseHeight = 5f, Amplitude = 14f, Frequency = 2.7f, Ridge = 0.3f, EdgeRise = 24f,
            Ground0 = "snow_a", Ground1 = "snow_b", GroundRoad = "snow_road", GroundRuin = "snow_ruin", RockTexture = "snow_rock", RuinTexture = "snow_ruin", RuinTint = new Rgb(1.2f, 1.22f, 1.3f),
            Ground1Weight = 1f, GroundTint0 = new Rgb(1.0f, 1.0f, 1.03f), GroundTint1 = new Rgb(1.0f, 1.0f, 1.0f), MacroBase = new Rgb(0.98f, 1.0f, 1.06f),
            Trees = TreeStyle.SnowPine, MaxTrees = 1100, TreeScale = 1.05f, UnderstoryDensity = 0.12f, VegTint = new Rgb(0.55f, 0.62f, 0.66f), RockCount = 130, RockScale = 1.2f,
            SkyDay = new Rgb(0.60f, 0.72f, 0.88f), FogDay = new Rgb(0.80f, 0.86f, 0.92f), FogDusk = new Rgb(0.78f, 0.62f, 0.64f),
            AmbientGround = new Rgb(0.34f, 0.38f, 0.46f), Skirt = new Rgb(0.80f, 0.84f, 0.90f),
            FogMultiplier = 1.0f, SunIntensity = 0.95f, Temperature = -8f, WindStrength = 1.0f,
            MapLow = new Rgb(0.78f, 0.84f, 0.90f), MapHigh = new Rgb(0.95f, 0.97f, 1.00f), MapVegetation = new Rgb(0.28f, 0.40f, 0.36f)
        };

        public static readonly BiomeProfile Swamp = new BiomeProfile
        {
            Id = SwampId, Name = "Pantano de la Niebla", ZoneId = "swamp_mire", PermitId = "permit_swamp_mire", PermitCost = 520,
            SitePrefix = "swamp_site_", SeedOffset = 23757,
            Description = "Barro, árboles muertos y niebla espesa casi todo el día. Tesoros hundidos que nadie ha visto.",
            BaseHeight = 2.2f, Amplitude = 6f, Frequency = 3.6f, Ridge = 0f, EdgeRise = 14f,
            Ground0 = "swamp_mud", Ground1 = "swamp_moss", GroundRoad = "swamp_road", GroundRuin = "swamp_earth", RockTexture = "rock", RuinTexture = "snow_ruin", RuinTint = new Rgb(0.75f, 0.9f, 0.7f),
            Ground1Weight = 1f, GroundTint0 = new Rgb(1.1f, 1.05f, 0.95f), GroundTint1 = new Rgb(0.7f, 0.85f, 0.55f), MacroBase = new Rgb(0.9f, 1.05f, 0.8f), WaterFraction = 0.22f,
            Trees = TreeStyle.SwampDead, MaxTrees = 620, TreeScale = 1f, UnderstoryDensity = 0.8f, VegTint = new Rgb(0.72f, 0.85f, 0.55f), RockCount = 60, RockScale = 0.9f,
            SkyDay = new Rgb(0.52f, 0.60f, 0.56f), FogDay = new Rgb(0.52f, 0.60f, 0.52f), FogDusk = new Rgb(0.60f, 0.52f, 0.38f),
            AmbientGround = new Rgb(0.16f, 0.18f, 0.12f), Skirt = new Rgb(0.12f, 0.15f, 0.09f),
            FogMultiplier = 2.6f, SunIntensity = 0.8f, Temperature = 20f, WindStrength = 0.2f,
            MapLow = new Rgb(0.22f, 0.28f, 0.16f), MapHigh = new Rgb(0.40f, 0.44f, 0.26f), MapVegetation = new Rgb(0.10f, 0.20f, 0.10f)
        };

        public static IReadOnlyList<BiomeProfile> All { get; } = new[] { Forest, Desert, Snow, Swamp };

        /// <summary>Busca por id (de bioma o de zona); si no existe devuelve el bosque.</summary>
        public static BiomeProfile ById(string id)
        {
            if (!string.IsNullOrEmpty(id))
                foreach (var b in All) if (b.Id == id || b.ZoneId == id) return b;
            return Forest;
        }

        public static BiomeProfile ByPermit(string permitId)
        {
            foreach (var b in All) if (b.PermitId == permitId) return b;
            return null;
        }
    }
}
