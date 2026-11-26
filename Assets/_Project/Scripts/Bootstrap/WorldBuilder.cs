using System.Collections.Generic;
using UnityEngine;
using HuesoOxido.Detection;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Arma el bosque con ruinas del MVP con geometria placeholder y un diseño fijo (misma semilla = mismo mundo).
    /// Reemplazar por arte definitivo segun ASSETS_TODO.md.
    /// </summary>
    public sealed class WorldBuilder
    {
        public const float Size = HeightField.Size;
        readonly BiomeProfile _bp;
        string ZoneId => _bp.ZoneId;

        readonly System.Random _rng;
        readonly int _seed;
        readonly PerlinNoise _noise;
        HeightField _field;
        Vector2 _camp = new Vector2(WorldLayout.CampX, WorldLayout.CampZ);
        Vector2 _ruins = new Vector2(WorldLayout.RuinsX, WorldLayout.RuinsZ);
        readonly List<Vector2> _treePositions = new List<Vector2>();
        InstancedVegetation _veg;
        int MaxTrees => _bp.MaxTrees;
        float[,] _forestMask;
        const int MapRes = 256;

        public WorldBuilder(int seed, BiomeProfile biome = null) { _bp = biome ?? Biomes.Forest; _seed = seed; _rng = new System.Random(seed); _noise = new PerlinNoise(seed); }

        public BiomeProfile Biome => _bp;
        static Color C(Rgb c) => new Color(c.R, c.G, c.B);

        public WorldData Build(IReadOnlyList<IItemInfo> items, Transform parent, HuesoOxido.Survival.GameClock clock = null)
        {
            var data = new WorldData();
            _night = clock != null ? (System.Func<float>)(() => HuesoOxido.Survival.NightLight.NightFactor(clock.Hour)) : null;
            _forestMask = new float[MapRes, MapRes];
            data.Road = WorldLayout.CreateRoad(_noise);
            _field = HeightField.Generate(_seed, WorldLayout.CampX, WorldLayout.CampZ, WorldLayout.RuinsX, WorldLayout.RuinsZ, data.Road, _bp);
            data.Field = _field;
            data.Ground = GroundBuilder.BuildMesh(_field, parent);
            BuildBounds(parent);
            BuildSkirt(parent);
            BuildWater(parent, data);
            _veg = new GameObject("Vegetation").AddComponent<InstancedVegetation>();
            data.Vegetation = _veg;
            _veg.transform.SetParent(parent, false);
            ScatterTrees(parent);
            ScatterRocks(parent);
            new VegetationBuilder(_field, _forestMask, _noise, _seed, _bp) { WaterLevel = _waterLevel }.Build(parent, _veg);
            _veg.Finish();
            Debug.Log("[VEG] instancias GPU totales=" + _veg.InstanceCount + " en " + _veg.LayerCount + " capas");
            BuildRuins(parent, data);
            var camp = CampBuilder.Build(parent, GroundAt(_camp), data, clock);
            data.CampPosition = camp;
            data.PlayerSpawn = GroundAt(_camp + new Vector2(4f, 5f)) + Vector3.up * 0.2f;
            data.VehicleSpawn = GroundAt(_camp + new Vector2(11f, 2f)) + Vector3.up * 1.2f;
            data.RuinsCenter = GroundAt(_ruins);
            PlaceSites(parent, data, items);
            BuildLighting(parent, data);
            GroundBuilder.ApplyMaterial(data.Ground, _field, _forestMask, _noise, _bp);
            data.Biome = _bp;
            data.MapTexture = BuildMapTexture(data);
            data.MapBounds = new Rect(-Size * 0.5f, -Size * 0.5f, Size, Size);
            return data;
        }

        /// <summary>Plano enorme verde oscuro bajo el borde: desde las alturas no se ve el vacio fuera del mundo.</summary>
        void BuildSkirt(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "Skirt";
            MaterialUtil.Dispose(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, 1f, 0f);
            go.transform.localScale = new Vector3(400f, 1f, 400f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = MaterialUtil.Lit(C(_bp.Skirt), 0.02f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        float _waterLevel = float.NegativeInfinity;
        System.Func<float> _night;

        /// <summary>
        /// Charcas y canales (pantano): un plano de agua oscuro y brillante a una altura calculada del relieve, siempre por debajo del
        /// camino, el campamento y las ruinas. Sin colision: se puede vadear.
        /// </summary>
        void BuildWater(Transform parent, WorldData data)
        {
            if (_bp.WaterFraction <= 0f) return;
            var all = new List<float>();
            for (int z = 0; z < HeightField.Nodes; z += 2)
                for (int x = 0; x < HeightField.Nodes; x += 2) all.Add(_field.H[z, x]);
            all.Sort();
            float level = all[Mathf.Clamp((int)(all.Count * _bp.WaterFraction), 0, all.Count - 1)];
            float cap = Mathf.Min(_field.HeightAt(_camp.x, _camp.y), _field.HeightAt(_ruins.x, _ruins.y)) - 0.5f;
            if (_field.Road != null)
                for (int i = 0; i < _field.Road.Count; i++) cap = Mathf.Min(cap, _field.HeightAt(_field.Road.X(i), _field.Road.Z(i)) - 0.12f);
            _waterLevel = Mathf.Min(level, cap);
            data.WaterLevel = _waterLevel;
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "Water";
            MaterialUtil.Dispose(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0f, _waterLevel, 0f);
            go.transform.localScale = new Vector3(Size / 10f, 1f, Size / 10f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = MaterialUtil.Lit(new Color(0.05f, 0.08f, 0.06f), 0.92f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Debug.Log("[VEG] agua a y=" + _waterLevel.ToString("0.00"));
        }

        bool Underwater(Vector2 p) => _field.HeightAt(p.x, p.y) < _waterLevel + 0.25f;

        /// <summary>Muros invisibles en el borde: el jugador y el vehiculo no pueden salir del mundo.</summary>
        void BuildBounds(Transform parent)
        {
            var root = new GameObject("WorldBounds").transform;
            root.SetParent(parent, false);
            float h = 90f, t = 6f, e = Size * 0.5f - 3f;
            foreach (var (pos, scale) in new[] {
                (new Vector3(0f, h * 0.5f, e + t * 0.5f), new Vector3(Size + 2f * t, h, t)),
                (new Vector3(0f, h * 0.5f, -e - t * 0.5f), new Vector3(Size + 2f * t, h, t)),
                (new Vector3(e + t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, Size + 2f * t)),
                (new Vector3(-e - t * 0.5f, h * 0.5f, 0f), new Vector3(t, h, Size + 2f * t)) })
            {
                var w = new GameObject("Wall").AddComponent<BoxCollider>();
                w.transform.SetParent(root, false);
                w.transform.localPosition = pos; w.size = scale;
            }
        }

        // ---------- Suelo ----------
        public Vector3 GroundAt(Vector2 xz) => new Vector3(xz.x, _field.HeightAt(xz.x, xz.y), xz.y);

        float Slope(Vector2 xz) => _field.SlopeDegrees(xz.x, xz.y);

        float RoadDistance(Vector2 xz) => _field.Road != null ? _field.Road.DistanceTo(xz.x, xz.y) : 99f;

        // ---------- Arboles ----------
        /// <summary>Un conjunto de variantes de arbol listo para instanciar: cada una con capas de tronco y copa.</summary>
        sealed class TreeKit
        {
            public readonly List<TreeVariant> Variants = new List<TreeVariant>();
            public readonly List<InstancedVegetation.Layer> Trunks = new List<InstancedVegetation.Layer>();
            public readonly List<InstancedVegetation.Layer[]> Crowns = new List<InstancedVegetation.Layer[]>();
        }

        const float TreeDist = 260f, LodDist = 85f, ShadowDist = 55f;

        InstancedVegetation.Layer TreeLayer(string name, Mesh mesh, Mesh lod, Material mat, float dist = TreeDist)
        {
            var l = _veg.AddLayer(name, mesh, mat, dist, true, true);
            l.LodMesh = lod; l.LodDistance = LodDist; l.ShadowDistance = ShadowDist;
            return l;
        }

        /// <summary>Agrega una familia de arboles: tronco + (opcional) copas con varios materiales.</summary>
        void AddFamily(TreeKit kit, string tag, int count, System.Func<int, float, TreeVariant> make, System.Func<int, TreeVariant> makeLod, Material trunkMat, Material[] crownMats)
        {
            for (int i = 0; i < count; i++)
            {
                var full = make(i, 1f); var lod = makeLod(i);
                kit.Variants.Add(full);
                kit.Trunks.Add(TreeLayer(tag + "_trunk_" + i, full.Trunk, lod.Trunk, trunkMat));
                var crowns = new InstancedVegetation.Layer[crownMats == null ? 0 : crownMats.Length];
                for (int k = 0; k < crowns.Length; k++) crowns[k] = TreeLayer(tag + "_crown" + k + "_" + i, full.Foliage, lod.Foliage, crownMats[k]);
                kit.Crowns.Add(crowns);
            }
        }

        Texture2D SnowDusted(Texture2D src, float amount)
        {
            var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, true) { name = src.name + "_snow", wrapMode = src.wrapMode };
            var px = src.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                float lum = (px[i].r + px[i].g + px[i].b) / 765f;
                float k = Mathf.Clamp01(amount * (0.7f + lum));
                px[i].r = (byte)Mathf.Lerp(px[i].r, 238f, k); px[i].g = (byte)Mathf.Lerp(px[i].g, 244f, k); px[i].b = (byte)Mathf.Lerp(px[i].b, 252f, k);
            }
            copy.SetPixels32(px); copy.Apply(true);
            return copy;
        }

        void ScatterTrees(Transform parent)
        {
            var root = new GameObject("Forest").transform;
            root.SetParent(parent, false);
            var forest = new TreeKit();     // conjunto A: coniferas / cactus / pinos nevados / arboles muertos
            var broad = new TreeKit();      // conjunto B: latifoliados (bosque) o sauces (pantano)

            switch (_bp.Trees)
            {
                case TreeStyle.Cactus:
                {
                    var cactusMat = MaterialUtil.Lit(new Color(0.30f, 0.44f, 0.22f), 0.22f);
                    var deadMat = ArtMaterials.Textured("bark_dead", new Color(1f, 0.95f, 0.9f), 0.05f);
                    AddFamily(forest, "cactus", 6, (i, d) => TreeFactory.Cactus(_seed + i * 13, 3.4f + i * 0.55f, d), i => TreeFactory.Cactus(_seed + i * 13, 3.4f + i * 0.55f, 0.5f), cactusMat, null);
                    AddFamily(broad, "dead", 4, (i, d) => TreeFactory.DeadTree(_seed + 80 + i * 17, 5.2f + i * 0.9f, d), i => TreeFactory.DeadTree(_seed + 80 + i * 17, 5.2f + i * 0.9f, 0.5f), deadMat, null);
                    break;
                }
                case TreeStyle.SwampDead:
                {
                    var deadMat = ArtMaterials.Textured("bark_dead", new Color(0.8f, 0.82f, 0.75f), 0.05f);
                    AddFamily(forest, "dead", 6, (i, d) => TreeFactory.DeadTree(_seed + i * 29, 6.5f + i * 0.9f, d), i => TreeFactory.DeadTree(_seed + i * 29, 6.5f + i * 0.9f, 0.5f), deadMat, null);
                    var leaf = FoliageTextures.LeafCluster(_seed + 3);
                    var willowMats = new[] { ArtMaterials.Foliage(leaf, new Color(0.55f, 0.72f, 0.40f), "swA"), ArtMaterials.Foliage(leaf, new Color(0.70f, 0.78f, 0.38f), "swB") };
                    AddFamily(broad, "willow", 3, (i, d) => TreeFactory.Broadleaf(_seed + 500 + i * 17, 7.5f + i * 0.9f, d), i => TreeFactory.Broadleaf(_seed + 500 + i * 17, 7.5f + i * 0.9f, 0.35f),
                        ArtMaterials.Textured("bark_dead", new Color(0.7f, 0.75f, 0.7f), 0.08f), willowMats);
                    break;
                }
                case TreeStyle.SnowPine:
                {
                    var needle = new[] { SnowDusted(FoliageTextures.Conifer(_seed + 1), 0.9f), SnowDusted(FoliageTextures.Conifer(_seed + 2), 0.75f) };
                    var mats = new[] { ArtMaterials.Foliage(needle[0], new Color(0.95f, 1f, 1f), "snA"), ArtMaterials.Foliage(needle[1], new Color(0.9f, 0.98f, 1f), "snB") };
                    AddFamily(forest, "snowpine", 5, (i, d) => TreeFactory.Conifer(_seed + i * 31, 10.5f + i * 1.0f, d), i => TreeFactory.Conifer(_seed + i * 31, 10.5f + i * 1.0f, 0.35f),
                        ArtMaterials.Textured("bark_pine", new Color(0.7f, 0.72f, 0.75f), 0.08f), mats);
                    var deadMat = ArtMaterials.Textured("bark_dead", new Color(0.85f, 0.88f, 0.95f), 0.06f);
                    AddFamily(broad, "dead", 3, (i, d) => TreeFactory.DeadTree(_seed + 90 + i * 19, 6f + i, d), i => TreeFactory.DeadTree(_seed + 90 + i * 19, 6f + i, 0.5f), deadMat, null);
                    break;
                }
                default:
                {
                    var needleTex = new[] { FoliageTextures.Conifer(_seed + 1), FoliageTextures.Conifer(_seed + 2) };
                    var leafTex = new[] { FoliageTextures.LeafCluster(_seed + 3), FoliageTextures.LeafCluster(_seed + 4) };
                    var conMats = new[] { ArtMaterials.Foliage(needleTex[0], new Color(0.82f, 0.95f, 0.8f), "cA"), ArtMaterials.Foliage(needleTex[1], new Color(0.95f, 1f, 0.8f), "cB") };
                    var broadMats = new[] {
                        ArtMaterials.Foliage(leafTex[0], new Color(0.9f, 1f, 0.8f), "bG"),
                        ArtMaterials.Foliage(leafTex[1], new Color(0.95f, 1f, 0.62f), "bY"),
                        ArtMaterials.Foliage(leafTex[0], new Color(1f, 0.62f, 0.32f), "bA") };
                    AddFamily(forest, "pine", 5, (i, d) => TreeFactory.Conifer(_seed + i * 31, 9.5f + i * 0.9f, d), i => TreeFactory.Conifer(_seed + i * 31, 9.5f + i * 0.9f, 0.35f),
                        ArtMaterials.Textured("bark_pine", new Color(0.85f, 0.8f, 0.75f), 0.08f), conMats);
                    AddFamily(broad, "broad", 3, (i, d) => TreeFactory.Broadleaf(_seed + 500 + i * 17, 7.5f + i * 0.9f, d), i => TreeFactory.Broadleaf(_seed + 500 + i * 17, 7.5f + i * 0.9f, 0.35f),
                        ArtMaterials.Textured("bark_broadleaf", new Color(0.8f, 0.78f, 0.72f), 0.08f), broadMats);
                    break;
                }
            }
            Debug.Log("[VEG] estilo de arboles=" + _bp.Trees + " variantes=" + forest.Variants.Count + "+" + broad.Variants.Count);

            // Reparto: cuanta proporcion va al conjunto A y como se eligen las capas de copa.
            double shareA;
            switch (_bp.Trees)
            {
                case TreeStyle.Cactus: shareA = 0.78; break;
                case TreeStyle.SwampDead: shareA = 0.78; break;
                case TreeStyle.SnowPine: shareA = 0.9; break;
                default: shareA = 0.68; break;
            }

            var grid = new Dictionary<Vector2Int, Vector2>();
            const float cell = 3.6f;
            var colliders = new GameObject("TreeColliders").transform;
            colliders.SetParent(root, false);
            bool forestLike = _bp.Trees != TreeStyle.Cactus;

            for (int attempt = 0; attempt < 16000 && _treePositions.Count < MaxTrees; attempt++)
            {
                var p = new Vector2(Rnd(-Size * 0.495f, Size * 0.495f), Rnd(-Size * 0.495f, Size * 0.495f));
                if (Vector2.Distance(p, _camp) < 24f) continue;
                if (Vector2.Distance(p, _ruins) < 12f) continue;
                if (Underwater(p)) continue;
                float edge = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) / (Size * 0.5f);
                if (RoadDistance(p) < 4.8f) continue;
                if (edge < 0.8f && Slope(p) > (forestLike ? 26f : 14f)) continue;
                if (!forestLike && edge > 0.86f) continue;     // el desierto remata en paredes de roca, no en arboles
                float clearing = _noise.Noise(p.x * 0.03f + _seed, p.y * 0.03f);
                if (edge < 0.85f && clearing > (forestLike ? 0.84f : 0.6f)) continue;
                float minDist = !forestLike ? 9f : (edge > 0.78f ? 2.7f : 3.5f);

                var cellKey = new Vector2Int(Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell));
                bool blocked = false;
                int rad = forestLike ? 1 : 3;
                for (int dx = -rad; dx <= rad && !blocked; dx++)
                    for (int dz = -rad; dz <= rad && !blocked; dz++)
                        if (grid.TryGetValue(new Vector2Int(cellKey.x + dx, cellKey.y + dz), out var other) && Vector2.Distance(other, p) < minDist)
                            blocked = true;
                if (blocked) continue;
                grid[cellKey] = p;
                _treePositions.Add(p);

                var g = GroundAt(p);
                float scale = Rnd(0.8f, 1.3f) * (edge > 0.85f ? Rnd(1.05f, 1.35f) : 1f) * _bp.TreeScale;
                var rot = Quaternion.Euler(0f, Rnd(0f, 360f), 0f);
                var m = Matrix4x4.TRS(g - Vector3.up * 0.15f, rot, Vector3.one * scale);

                bool useA = _rng.NextDouble() < shareA;
                var kit = useA ? forest : broad;
                int vi = _rng.Next(kit.Variants.Count);
                var v = kit.Variants[vi];
                _veg.Add(kit.Trunks[vi], m);
                var crowns = kit.Crowns[vi];
                if (crowns.Length > 0)
                {
                    int ci;
                    double r = _rng.NextDouble();
                    if (crowns.Length == 3) ci = r < 0.78 ? 0 : (r < 0.94 ? 1 : 2);   // verde, amarillo, otono
                    else ci = r < 0.5 ? 0 : 1;
                    _veg.Add(crowns[ci], m);
                }

                var col = new GameObject("T").AddComponent<CapsuleCollider>();
                col.transform.SetParent(colliders, false);
                col.transform.position = g + Vector3.up * 1.6f;
                col.radius = Mathf.Max(0.3f, v.TrunkRadius * scale * (_bp.Trees == TreeStyle.Cactus && useA ? 1.0f : 1.2f));
                col.height = 3.2f;
                MarkForest(p, forestLike ? 2 : 1);
            }
            Debug.Log("[VEG] arboles=" + _treePositions.Count);
        }

        float Rnd(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        void MarkForest(Vector2 p, int radius)
        {
            int cx = Mathf.RoundToInt((p.x + Size * 0.5f) / Size * (MapRes - 1));
            int cy = Mathf.RoundToInt((p.y + Size * 0.5f) / Size * (MapRes - 1));
            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x >= 0 && y >= 0 && x < MapRes && y < MapRes) _forestMask[y, x] = 1f;
                }
        }

        // ---------- Rocas ----------
        void ScatterRocks(Transform parent)
        {
            var root = new GameObject("Rocks").transform;
            root.SetParent(parent, false);
            var rockMat = ArtMaterials.Textured(_bp.RockTexture, new Color(0.9f, 0.9f, 0.85f), 0.2f);
            var variants = new Mesh[7];
            for (int i = 0; i < variants.Length; i++)
                variants[i] = MeshKit.Rock(_seed + 70 + i, 1f, new Vector3(Rnd(0.9f, 1.5f), Rnd(0.55f, 0.95f), Rnd(0.8f, 1.3f)));
            int placed = 0;
            for (int i = 0; i < 400 + _bp.RockCount * 3 && placed < _bp.RockCount; i++)
            {
                Vector2 p;
                bool cluster = _rng.NextDouble() < 0.35;
                if (cluster) p = _ruins + new Vector2(Rnd(-30f, 30f), Rnd(-30f, 30f));
                else p = new Vector2(Rnd(-Size * 0.46f, Size * 0.46f), Rnd(-Size * 0.46f, Size * 0.46f));
                if (Vector2.Distance(p, _camp) < 16f || RoadDistance(p) < 3f || Underwater(p)) continue;
                if (Vector2.Distance(p, _ruins) < 9f) continue;
                var g = GroundAt(p);
                float s = Rnd(0.45f, 2.0f) * _bp.RockScale * (_bp.Ridge > 0.4f && _rng.NextDouble() < 0.25 ? Rnd(1.6f, 2.8f) : 1f);
                var go = new GameObject("Rock");
                go.transform.SetParent(root, false);
                go.transform.position = g + Vector3.up * (s * 0.08f);
                go.transform.rotation = Quaternion.Euler(Rnd(-6f, 6f), Rnd(0f, 360f), Rnd(-6f, 6f));
                go.transform.localScale = Vector3.one * s;
                var mesh = variants[_rng.Next(variants.Length)];
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = rockMat;
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
                go.isStatic = true;
                placed++;
            }
        }

        // ---------- Ruinas ----------
        void BuildRuins(Transform parent, WorldData data)
        {
            var root = new GameObject("Ruins").transform;
            root.SetParent(parent, false);
            var stone = ArtMaterials.Textured(_bp.RuinTexture, C(_bp.RuinTint), 0.10f);
            var moss = _bp.IsDefault ? ArtMaterials.Textured("ruin_blocks", new Color(0.82f, 0.88f, 0.78f), 0.08f) : ArtMaterials.Textured(_bp.RuinTexture, C(_bp.RuinTint) * 0.75f, 0.08f);
            RuinsBuilder.Build(root, GroundAt(_ruins), GroundAt, stone, moss, _rng, _night);
        }

        static GameObject Block(Transform parent, string name, Material mat, Vector3 pos, Vector3 scale, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var mesh = MeshKit.Box(scale, 4.2f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<BoxCollider>().size = scale;
            go.isStatic = true;
            return go;
        }

        // ---------- Sitios enterrados ----------
        void PlaceSites(Transform parent, WorldData data, IReadOnlyList<IItemInfo> items)
        {
            var root = new GameObject("BuriedSites").transform;
            root.SetParent(parent, false);
            var placed = new List<Vector2>();

            // Hallazgos clave con ubicacion fija (diseño de nivel).
            var picks = new List<IItemInfo>();
            foreach (var i in items) if (i.Id != "golden_torc" && i.Id != "expedition_journal" && !i.Id.StartsWith(QuestManager.RelicPrefix, System.StringComparison.Ordinal)) picks.Add(i);
            if (_bp.IsDefault)
            {
                Add(root, data, placed, _ruins + new Vector2(0f, 0.5f), "golden_torc", "forest_site_torc", 1.1f, true);
                Add(root, data, placed, _ruins + new Vector2(-3.2f, -10.5f), "expedition_journal", "forest_site_journal", 0.7f, true);
            }
            else
            {
                // Cada ecosistema tiene dos hallazgos fijos en sus ruinas (el mas valioso, en el centro).
                Add(root, data, placed, _ruins + new Vector2(0f, 0.5f), LootTable.Pick(picks, _rng).Id, _bp.SitePrefix + "ruin_a", 1.1f, true);
                Add(root, data, placed, _ruins + new Vector2(-3.2f, -10.5f), LootTable.Pick(picks, _rng).Id, _bp.SitePrefix + "ruin_b", 0.7f, true);
            }

            int n = 0;
            for (int attempt = 0; attempt < 900 && n < 28; attempt++)
            {
                bool nearRuins = _rng.NextDouble() < 0.6;
                Vector2 p = nearRuins
                    ? _ruins + new Vector2(Rnd(-34f, 34f), Rnd(-34f, 34f))
                    : new Vector2(Rnd(-Size * 0.42f, Size * 0.42f), Rnd(-Size * 0.42f, Size * 0.42f));
                if (Vector2.Distance(p, _camp) < 30f || Slope(p) > 24f || RoadDistance(p) < 2.5f || Underwater(p)) continue;
                if (nearRuins && Mathf.Abs(p.x - _ruins.x) < 9f && Mathf.Abs(p.y - _ruins.y) < 9f) continue;
                if (TooClose(placed, p, 5f) || NearTree(p, 1.6f)) continue;
                var info = LootTable.Pick(picks, _rng);
                Add(root, data, placed, p, info.Id, _bp.SitePrefix + n.ToString("00"), 0.3f + (int)info.Rarity * 0.12f + Rnd(0f, 0.15f), _rng.NextDouble() < 0.5);
                n++;
            }
            if (_bp.IsDefault) PlaceRelics(root, data, placed, items);   // las reliquias de la mision principal solo estan en el bosque
        }

        /// <summary>
        /// Las reliquias de la mision principal: un sitio por reliquia, repartidos al azar por todo el mapa (alturas del terreno
        /// reales), lejos del campamento, del camino y entre si, en suelo llano.
        /// </summary>
        void PlaceRelics(Transform root, WorldData data, List<Vector2> placed, IReadOnlyList<IItemInfo> items)
        {
            var relicPositions = new List<Vector2>();
            int index = 0;
            foreach (var info in items)
            {
                if (!info.Id.StartsWith(QuestManager.RelicPrefix, System.StringComparison.Ordinal)) continue;
                for (int attempt = 0; attempt < 1500; attempt++)
                {
                    var p = new Vector2(Rnd(-Size * 0.4f, Size * 0.4f), Rnd(-Size * 0.4f, Size * 0.4f));
                    if (Vector2.Distance(p, _camp) < 55f || Vector2.Distance(p, _ruins) < 14f) continue;
                    if (Slope(p) > 20f || RoadDistance(p) < 3f) continue;
                    if (TooClose(relicPositions, p, 55f) || TooClose(placed, p, 6f) || NearTree(p, 1.8f)) continue;
                    Add(root, data, placed, p, info.Id, "relic_site_" + index.ToString("00"), 0.6f, false);
                    relicPositions.Add(p);
                    break;
                }
                index++;
            }
        }

        bool TooClose(List<Vector2> l, Vector2 p, float d) { foreach (var q in l) if (Vector2.Distance(q, p) < d) return true; return false; }
        bool NearTree(Vector2 p, float d) { foreach (var q in _treePositions) if (Vector2.Distance(q, p) < d) return true; return false; }

        void Add(Transform root, WorldData data, List<Vector2> placed, Vector2 xz, string itemId, string siteId, float depth, bool hint)
        {
            var go = new GameObject(siteId);
            go.transform.SetParent(root, false);
            go.transform.position = GroundAt(xz);
            var site = go.AddComponent<BuriedSite>();
            float seconds = 2.8f + depth * 3.2f;
            site.Setup(siteId, ZoneId, itemId, depth, seconds);
            if (hint) site.ShowHint();
            data.Sites.Add(site);
            placed.Add(xz);
        }

        // ---------- Iluminacion ----------
        void BuildLighting(Transform parent, WorldData data)
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional && l.transform.parent == null) l.gameObject.SetActive(false);

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.9f;
            data.Sun = sun;

            var moonGo = new GameObject("Moon");
            moonGo.transform.SetParent(parent, false);
            var moon = moonGo.AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.shadows = LightShadows.None;
            data.Moon = moon;

            var shader = Shader.Find("Skybox/Procedural");
            if (shader != null) data.Skybox = new Material(shader) { name = "ProceduralSky" };
        }

        // ---------- Mapa ----------
        Texture2D BuildMapTexture(WorldData data)
        {
            var tex = new Texture2D(MapRes, MapRes, TextureFormat.RGBA32, false);
            var px = new Color[MapRes * MapRes];
            float cell = Size / (MapRes - 1);
            for (int y = 0; y < MapRes; y++)
            {
                for (int x = 0; x < MapRes; x++)
                {
                    float wx = -Size * 0.5f + x * cell, wz = -Size * 0.5f + y * cell;
                    float h = _field.HeightAt(wx, wz);
                    float shade = Mathf.Clamp01(0.55f + (_field.HeightAt(wx - cell, wz - cell) - h) * 0.10f);
                    float hn = Mathf.InverseLerp(3f, 24f, h);
                    Color c = Color.Lerp(C(_bp.MapLow), C(_bp.MapHigh), hn);
                    c = Color.Lerp(c, C(_bp.MapVegetation), Mathf.Clamp01(_forestMask[y, x]) * 0.65f);
                    if (_field.Road != null && _field.Road.DistanceTo(wx, wz) < 2.2f) c = new Color(0.78f, 0.66f, 0.45f);
                    float dr = Mathf.Sqrt((wx - _ruins.x) * (wx - _ruins.x) + (wz - _ruins.y) * (wz - _ruins.y));
                    if (dr < 13f) c = Color.Lerp(c, new Color(0.66f, 0.38f, 0.26f), 0.7f);
                    c = c * (0.6f + shade * 0.8f);
                    c.a = 1f;
                    px[y * MapRes + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply(false);
            return tex;
        }
    }
}
