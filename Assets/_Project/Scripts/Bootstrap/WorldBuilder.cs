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
        const string ZoneId = "forest_ruins";

        readonly System.Random _rng;
        readonly int _seed;
        readonly PerlinNoise _noise;
        HeightField _field;
        Vector2 _camp = new Vector2(WorldLayout.CampX, WorldLayout.CampZ);
        Vector2 _ruins = new Vector2(WorldLayout.RuinsX, WorldLayout.RuinsZ);
        readonly List<Vector2> _treePositions = new List<Vector2>();
        InstancedVegetation _veg;
        public const int MaxTrees = 1500;
        float[,] _forestMask;
        const int MapRes = 256;

        public WorldBuilder(int seed) { _seed = seed; _rng = new System.Random(seed); _noise = new PerlinNoise(seed); }

        public WorldData Build(IReadOnlyList<IItemInfo> items, Transform parent, HuesoOxido.Survival.GameClock clock = null)
        {
            var data = new WorldData();
            _forestMask = new float[MapRes, MapRes];
            data.Road = WorldLayout.CreateRoad(_noise);
            _field = HeightField.Generate(_seed, WorldLayout.CampX, WorldLayout.CampZ, WorldLayout.RuinsX, WorldLayout.RuinsZ, data.Road);
            data.Field = _field;
            data.Ground = GroundBuilder.BuildMesh(_field, parent);
            BuildBounds(parent);
            BuildSkirt(parent);
            _veg = new GameObject("Vegetation").AddComponent<InstancedVegetation>();
            data.Vegetation = _veg;
            _veg.transform.SetParent(parent, false);
            ScatterTrees(parent);
            ScatterRocks(parent);
            new VegetationBuilder(_field, _forestMask, _noise, _seed).Build(parent, _veg);
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
            GroundBuilder.ApplyMaterial(data.Ground, _field, _forestMask, _noise);
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
            r.sharedMaterial = MaterialUtil.Lit(new Color(0.10f, 0.17f, 0.09f), 0.02f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

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
        void ScatterTrees(Transform parent)
        {
            var root = new GameObject("Forest").transform;
            root.SetParent(parent, false);

            var conifers = new TreeVariant[5];
            for (int i = 0; i < conifers.Length; i++) conifers[i] = TreeFactory.Conifer(_seed + i * 31, 9.5f + i * 0.9f);
            var broads = new TreeVariant[3];
            for (int i = 0; i < broads.Length; i++) broads[i] = TreeFactory.Broadleaf(_seed + 500 + i * 17, 7.5f + i * 0.9f);
            // Versiones simples para lejos (LOD): mismas formas con ~35 % de la geometria.
            var conifersLod = new TreeVariant[conifers.Length];
            for (int i = 0; i < conifers.Length; i++) conifersLod[i] = TreeFactory.Conifer(_seed + i * 31, 9.5f + i * 0.9f, 0.35f);
            var broadsLod = new TreeVariant[broads.Length];
            for (int i = 0; i < broads.Length; i++) broadsLod[i] = TreeFactory.Broadleaf(_seed + 500 + i * 17, 7.5f + i * 0.9f, 0.35f);

            Debug.Log("[VEG] triangulos por arbol: conifera " + (conifers[0].Foliage.triangles.Length + conifers[0].Trunk.triangles.Length) / 3 + " -> LOD " + (conifersLod[0].Foliage.triangles.Length + conifersLod[0].Trunk.triangles.Length) / 3
                      + " | latifoliado " + (broads[0].Foliage.triangles.Length + broads[0].Trunk.triangles.Length) / 3 + " -> LOD " + (broadsLod[0].Foliage.triangles.Length + broadsLod[0].Trunk.triangles.Length) / 3);
            var needleTex = new[] { FoliageTextures.Conifer(_seed + 1), FoliageTextures.Conifer(_seed + 2) };
            var leafTex = new[] { FoliageTextures.LeafCluster(_seed + 3), FoliageTextures.LeafCluster(_seed + 4) };
            var mats = new Dictionary<string, Material>
            {
                { "bark_pine", ArtMaterials.Textured("bark_pine", new Color(0.85f, 0.8f, 0.75f), 0.08f) },
                { "bark_broad", ArtMaterials.Textured("bark_broadleaf", new Color(0.8f, 0.78f, 0.72f), 0.08f) },
                { "conifer_a", ArtMaterials.Foliage(needleTex[0], new Color(0.82f, 0.95f, 0.8f), "cA") },
                { "conifer_b", ArtMaterials.Foliage(needleTex[1], new Color(0.95f, 1f, 0.8f), "cB") },
                { "broad_green", ArtMaterials.Foliage(leafTex[0], new Color(0.9f, 1f, 0.8f), "bG") },
                { "broad_yellow", ArtMaterials.Foliage(leafTex[1], new Color(0.95f, 1f, 0.62f), "bY") },
                { "broad_autumn", ArtMaterials.Foliage(leafTex[0], new Color(1f, 0.62f, 0.32f), "bA") },
            };

            // Una capa instanciada por (malla, material): troncos y copas de cada variante.
            var treeLayers = new Dictionary<string, InstancedVegetation.Layer>();
            const float treeDist = 260f, lodDist = 85f, shadowDist = 55f;
            InstancedVegetation.Layer TreeLayer(string name, Mesh mesh, Mesh lod, Material mat)
            {
                var l = _veg.AddLayer(name, mesh, mat, treeDist, true, true);
                l.LodMesh = lod; l.LodDistance = lodDist; l.ShadowDistance = shadowDist;
                return l;
            }
            for (int i = 0; i < conifers.Length; i++)
            {
                treeLayers["pt" + i] = TreeLayer("pine_trunk_" + i, conifers[i].Trunk, conifersLod[i].Trunk, mats["bark_pine"]);
                treeLayers["pa" + i] = TreeLayer("pine_a_" + i, conifers[i].Foliage, conifersLod[i].Foliage, mats["conifer_a"]);
                treeLayers["pb" + i] = TreeLayer("pine_b_" + i, conifers[i].Foliage, conifersLod[i].Foliage, mats["conifer_b"]);
            }
            for (int i = 0; i < broads.Length; i++)
            {
                treeLayers["bt" + i] = TreeLayer("broad_trunk_" + i, broads[i].Trunk, broadsLod[i].Trunk, mats["bark_broad"]);
                treeLayers["bg" + i] = TreeLayer("broad_green_" + i, broads[i].Foliage, broadsLod[i].Foliage, mats["broad_green"]);
                treeLayers["by" + i] = TreeLayer("broad_yellow_" + i, broads[i].Foliage, broadsLod[i].Foliage, mats["broad_yellow"]);
                treeLayers["ba" + i] = TreeLayer("broad_autumn_" + i, broads[i].Foliage, broadsLod[i].Foliage, mats["broad_autumn"]);
            }

            var grid = new Dictionary<Vector2Int, Vector2>();
            const float cell = 3.6f;
            var colliders = new GameObject("TreeColliders").transform;
            colliders.SetParent(root, false);

            for (int attempt = 0; attempt < 16000 && _treePositions.Count < MaxTrees; attempt++)
            {
                var p = new Vector2(Rnd(-Size * 0.495f, Size * 0.495f), Rnd(-Size * 0.495f, Size * 0.495f));
                if (Vector2.Distance(p, _camp) < 24f) continue;
                if (Vector2.Distance(p, _ruins) < 12f) continue;
                float edge = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) / (Size * 0.5f);
                if (RoadDistance(p) < 4.8f) continue;
                if (edge < 0.8f && Slope(p) > 26f) continue;
                float clearing = _noise.Noise(p.x * 0.03f + _seed, p.y * 0.03f);
                // Claros mas escasos y pequeños: el bosque es denso; en el borde del mapa es una muralla de arboles.
                if (edge < 0.85f && clearing > 0.84f) continue;
                float minDist = edge > 0.78f ? 2.7f : 3.5f;

                var cellKey = new Vector2Int(Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell));
                bool blocked = false;
                for (int dx = -1; dx <= 1 && !blocked; dx++)
                    for (int dz = -1; dz <= 1 && !blocked; dz++)
                        if (grid.TryGetValue(new Vector2Int(cellKey.x + dx, cellKey.y + dz), out var other) && Vector2.Distance(other, p) < minDist)
                            blocked = true;
                if (blocked) continue;
                grid[cellKey] = p;
                _treePositions.Add(p);

                var g = GroundAt(p);
                float scale = Rnd(0.8f, 1.3f) * (edge > 0.85f ? Rnd(1.05f, 1.35f) : 1f);
                var rot = Quaternion.Euler(0f, Rnd(0f, 360f), 0f);
                var m = Matrix4x4.TRS(g - Vector3.up * 0.15f, rot, Vector3.one * scale);

                TreeVariant v;
                if (_rng.NextDouble() < 0.68)
                {
                    int vi = _rng.Next(conifers.Length);
                    v = conifers[vi];
                    _veg.Add(treeLayers["pt" + vi], m);
                    _veg.Add(treeLayers[(_rng.NextDouble() < 0.5 ? "pa" : "pb") + vi], m);
                }
                else
                {
                    int vi = _rng.Next(broads.Length);
                    v = broads[vi];
                    double r = _rng.NextDouble();
                    _veg.Add(treeLayers["bt" + vi], m);
                    _veg.Add(treeLayers[(r < 0.78 ? "bg" : (r < 0.94 ? "by" : "ba")) + vi], m);
                }

                var col = new GameObject("T").AddComponent<CapsuleCollider>();
                col.transform.SetParent(colliders, false);
                col.transform.position = g + Vector3.up * 1.6f;
                col.radius = Mathf.Max(0.3f, v.TrunkRadius * scale * 1.2f);
                col.height = 3.2f;
                MarkForest(p, 2);
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
            var rockMat = ArtMaterials.Textured("rock", new Color(0.9f, 0.9f, 0.85f), 0.2f);
            var variants = new Mesh[7];
            for (int i = 0; i < variants.Length; i++)
                variants[i] = MeshKit.Rock(_seed + 70 + i, 1f, new Vector3(Rnd(0.9f, 1.5f), Rnd(0.55f, 0.95f), Rnd(0.8f, 1.3f)));
            int placed = 0;
            for (int i = 0; i < 400 && placed < 120; i++)
            {
                Vector2 p;
                bool cluster = _rng.NextDouble() < 0.35;
                if (cluster) p = _ruins + new Vector2(Rnd(-30f, 30f), Rnd(-30f, 30f));
                else p = new Vector2(Rnd(-Size * 0.46f, Size * 0.46f), Rnd(-Size * 0.46f, Size * 0.46f));
                if (Vector2.Distance(p, _camp) < 16f || RoadDistance(p) < 3f) continue;
                if (Vector2.Distance(p, _ruins) < 9f) continue;
                var g = GroundAt(p);
                float s = Rnd(0.45f, 2.0f);
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
            var stone = ArtMaterials.Textured("ruin_stone", new Color(1.35f, 1.3f, 1.2f), 0.10f);
            var moss = ArtMaterials.Textured("ruin_blocks", new Color(0.82f, 0.88f, 0.78f), 0.08f);
            var c = GroundAt(_ruins);

            // Losas de piedra alrededor de la plaza; el centro queda abierto (ahi esta el hallazgo mas valioso).
            for (int i = 0; i < 16; i++)
            {
                float a = i * Mathf.PI * 2f / 16f + Rnd(-0.15f, 0.15f);
                float r = Rnd(4.6f, 7.2f);
                var g = GroundAt(new Vector2(c.x + Mathf.Cos(a) * r, c.z + Mathf.Sin(a) * r));
                Block(root, "Flagstone", stone, g + Vector3.up * 0.04f, new Vector3(Rnd(1.6f, 2.4f), 0.14f, Rnd(1.4f, 2.1f)), Rnd(0f, 360f));
            }

            // Muros perimetrales en ruinas: tramos con huecos y alturas dispares.
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (_rng.NextDouble() < 0.28) continue;
                    float t = (i - 2.5f) * 2.6f;
                    float h = Rnd(0.8f, 3.4f);
                    Vector3 off; float yaw;
                    switch (side)
                    {
                        case 0: off = new Vector3(t, 0f, 8f); yaw = 0f; break;
                        case 1: off = new Vector3(t, 0f, -8f); yaw = 0f; break;
                        case 2: off = new Vector3(8f, 0f, t); yaw = 90f; break;
                        default: off = new Vector3(-8f, 0f, t); yaw = 90f; break;
                    }
                    if (side == 1 && Mathf.Abs(t) < 3.2f) continue; // entrada
                    Block(root, "Wall", stone, c + off + Vector3.up * (h * 0.5f), new Vector3(2.5f, h, 0.9f), yaw + Rnd(-3f, 3f));
                }
            }

            // Columnas y arco.
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                var pos = c + new Vector3(Mathf.Cos(a) * 4.2f, 0f, Mathf.Sin(a) * 4.2f);
                float h = _rng.NextDouble() < 0.5 ? Rnd(2.5f, 4f) : Rnd(0.8f, 1.6f);
                var col = new GameObject("Column");
                col.transform.SetParent(root, false);
                col.transform.position = pos + Vector3.up * 0.3f;
                var cm = MeshKit.Trunk(h, 0.55f, 0.48f, 14, 3, 4.2f, 0f, i);
                col.AddComponent<MeshFilter>().sharedMesh = cm;
                col.AddComponent<MeshRenderer>().sharedMaterial = stone;
                var cc = col.AddComponent<CapsuleCollider>(); cc.radius = 0.55f; cc.height = h; cc.center = new Vector3(0f, h * 0.5f, 0f);
                col.isStatic = true;
            }
            Block(root, "ArchL", stone, c + new Vector3(-1.6f, 2.4f, -8f), new Vector3(0.9f, 4.8f, 0.9f), 0f);
            Block(root, "ArchR", stone, c + new Vector3(1.6f, 2.4f, -8f), new Vector3(0.9f, 4.8f, 0.9f), 0f);
            Block(root, "ArchTop", stone, c + new Vector3(0.3f, 5.0f, -8f), new Vector3(4.2f, 0.7f, 1f), 4f);

            // Bloques caidos y musgo.
            for (int i = 0; i < 22; i++)
            {
                float a = Rnd(0f, Mathf.PI * 2f), r = Rnd(5f, 14f);
                var p = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                var g = GroundAt(new Vector2(p.x, p.z));
                var b = Block(root, "Fallen", i % 4 == 0 ? moss : stone, g + Vector3.up * 0.35f, new Vector3(Rnd(0.8f, 1.8f), Rnd(0.5f, 0.9f), Rnd(0.8f, 1.5f)), Rnd(0f, 360f));
                b.transform.rotation *= Quaternion.Euler(Rnd(-8f, 8f), 0f, Rnd(-8f, 8f));
            }
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
            Add(root, data, placed, _ruins + new Vector2(0f, 0.5f), "golden_torc", "forest_site_torc", 1.1f, true);
            Add(root, data, placed, _ruins + new Vector2(-3.2f, -10.5f), "expedition_journal", "forest_site_journal", 0.7f, true);

            var picks = new List<IItemInfo>();
            foreach (var i in items) if (i.Id != "golden_torc" && i.Id != "expedition_journal" && !i.Id.StartsWith(QuestManager.RelicPrefix, System.StringComparison.Ordinal)) picks.Add(i);

            int n = 0;
            for (int attempt = 0; attempt < 900 && n < 28; attempt++)
            {
                bool nearRuins = _rng.NextDouble() < 0.6;
                Vector2 p = nearRuins
                    ? _ruins + new Vector2(Rnd(-34f, 34f), Rnd(-34f, 34f))
                    : new Vector2(Rnd(-Size * 0.42f, Size * 0.42f), Rnd(-Size * 0.42f, Size * 0.42f));
                if (Vector2.Distance(p, _camp) < 30f || Slope(p) > 24f || RoadDistance(p) < 2.5f) continue;
                if (nearRuins && Mathf.Abs(p.x - _ruins.x) < 9f && Mathf.Abs(p.y - _ruins.y) < 9f) continue;
                if (TooClose(placed, p, 5f) || NearTree(p, 1.6f)) continue;
                var info = LootTable.Pick(picks, _rng);
                Add(root, data, placed, p, info.Id, "forest_site_" + n.ToString("00"), 0.3f + (int)info.Rarity * 0.12f + Rnd(0f, 0.15f), _rng.NextDouble() < 0.5);
                n++;
            }
            PlaceRelics(root, data, placed, items);
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
                    Color c = Color.Lerp(new Color(0.24f, 0.34f, 0.18f), new Color(0.58f, 0.52f, 0.36f), hn);
                    c = Color.Lerp(c, new Color(0.10f, 0.22f, 0.12f), Mathf.Clamp01(_forestMask[y, x]) * 0.65f);
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
