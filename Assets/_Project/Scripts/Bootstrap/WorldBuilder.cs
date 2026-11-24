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
        public const float Size = 320f;
        public const float MaxHeight = 36f;
        const int HeightRes = 257;
        const string ZoneId = "forest_ruins";

        readonly System.Random _rng;
        readonly int _seed;
        Terrain _terrain;
        TerrainData _td;
        Vector2 _camp = new Vector2(-70f, -70f);
        Vector2 _ruins = new Vector2(55f, 45f);
        readonly List<Vector2> _treePositions = new List<Vector2>();
        bool[,] _forestMask;
        const int MapRes = 256;

        public WorldBuilder(int seed) { _seed = seed; _rng = new System.Random(seed); }

        public WorldData Build(IReadOnlyList<IItemInfo> items, Transform parent)
        {
            var data = new WorldData();
            _forestMask = new bool[MapRes, MapRes];
            BuildTerrain(parent, data);
            ScatterTrees(parent);
            ScatterRocks(parent);
            BuildRuins(parent, data);
            var camp = CampBuilder.Build(parent, GroundAt(_camp), data);
            data.CampPosition = camp;
            data.PlayerSpawn = GroundAt(_camp + new Vector2(4f, 5f)) + Vector3.up * 0.2f;
            data.VehicleSpawn = GroundAt(_camp + new Vector2(11f, 2f)) + Vector3.up * 1.2f;
            data.RuinsCenter = GroundAt(_ruins);
            PlaceSites(parent, data, items);
            BuildLighting(parent, data);
            data.MapTexture = BuildMapTexture(data);
            data.MapBounds = new Rect(-Size * 0.5f, -Size * 0.5f, Size, Size);
            return data;
        }

        // ---------- Terreno ----------
        float Raw(float x, float z)
        {
            float ox = _seed * 0.37f, oz = _seed * 0.61f;
            float n = Mathf.PerlinNoise(ox + x * 3.1f, oz + z * 3.1f) * 0.6f
                    + Mathf.PerlinNoise(ox + x * 8f, oz + z * 8f) * 0.28f
                    + Mathf.PerlinNoise(ox + x * 21f, oz + z * 21f) * 0.07f;
            return 0.12f + n * 0.3f;
        }

        float HeightAt01(float nx, float nz, float campH, float ruinsH)
        {
            float h = Raw(nx, nz);
            float wx = (nx - 0.5f) * Size, wz = (nz - 0.5f) * Size;
            float dc = Vector2.Distance(new Vector2(wx, wz), _camp);
            float dr = Vector2.Distance(new Vector2(wx, wz), _ruins);
            h = Mathf.Lerp(campH, h, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(20f, 48f, dc)));
            h = Mathf.Lerp(ruinsH, h, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(26f, 52f, dr)));
            return h;
        }

        void BuildTerrain(Transform parent, WorldData data)
        {
            _td = new TerrainData { heightmapResolution = HeightRes };
            _td.size = new Vector3(Size, MaxHeight, Size);
            float campH = Raw((_camp.x / Size) + 0.5f, (_camp.y / Size) + 0.5f);
            float ruinsH = Raw((_ruins.x / Size) + 0.5f, (_ruins.y / Size) + 0.5f);
            var heights = new float[HeightRes, HeightRes];
            for (int z = 0; z < HeightRes; z++)
                for (int x = 0; x < HeightRes; x++)
                    heights[z, x] = HeightAt01(x / (HeightRes - 1f), z / (HeightRes - 1f), campH, ruinsH);
            _td.SetHeights(0, 0, heights);

            var grass = new TerrainLayer { diffuseTexture = ProceduralArt.NoiseTexture(256, new Color(0.17f, 0.25f, 0.10f), new Color(0.30f, 0.36f, 0.16f), 14f, _seed + 1, 1.4f), tileSize = new Vector2(7f, 7f) };
            var dirt = new TerrainLayer { diffuseTexture = ProceduralArt.NoiseTexture(256, new Color(0.26f, 0.19f, 0.12f), new Color(0.40f, 0.31f, 0.20f), 10f, _seed + 2, 1.3f), tileSize = new Vector2(5f, 5f) };
            _td.terrainLayers = new[] { grass, dirt };

            int ar = 256;
            _td.alphamapResolution = ar;
            var alpha = new float[ar, ar, 2];
            for (int y = 0; y < ar; y++)
            {
                for (int x = 0; x < ar; x++)
                {
                    float nx = x / (ar - 1f), ny = y / (ar - 1f);
                    float steep = _td.GetSteepness(nx, ny);
                    float wx = (nx - 0.5f) * Size, wz = (ny - 0.5f) * Size;
                    float d = Mathf.Min(Vector2.Distance(new Vector2(wx, wz), _camp) / 18f, Vector2.Distance(new Vector2(wx, wz), _ruins) / 24f);
                    float dirtW = Mathf.Clamp01((steep - 16f) / 14f) * 0.9f + (1f - Mathf.Clamp01(d)) * 0.7f
                                  + Mathf.PerlinNoise(nx * 18f + _seed, ny * 18f) * 0.18f;
                    dirtW = Mathf.Clamp01(dirtW);
                    alpha[y, x, 0] = 1f - dirtW;
                    alpha[y, x, 1] = dirtW;
                }
            }
            _td.SetAlphamaps(0, 0, alpha);

            var go = Terrain.CreateTerrainGameObject(_td);
            go.name = "Terrain";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-Size * 0.5f, 0f, -Size * 0.5f);
            _terrain = go.GetComponent<Terrain>();
            _terrain.drawInstanced = true;
            _terrain.heightmapPixelError = 8f;
            data.Terrain = _terrain;
        }

        public Vector3 GroundAt(Vector2 xz)
        {
            float y = _terrain.SampleHeight(new Vector3(xz.x, 0f, xz.y)) + _terrain.transform.position.y;
            return new Vector3(xz.x, y, xz.y);
        }

        float Slope(Vector2 xz)
        {
            return _td.GetSteepness((xz.x + Size * 0.5f) / Size, (xz.y + Size * 0.5f) / Size);
        }

        // ---------- Arboles ----------
        sealed class Chunk
        {
            public readonly List<CombineInstance> Trunks = new List<CombineInstance>();
            public readonly List<CombineInstance> Pine = new List<CombineInstance>();
            public readonly List<CombineInstance> Broad = new List<CombineInstance>();
            public readonly List<CombineInstance> Autumn = new List<CombineInstance>();
        }

        void ScatterTrees(Transform parent)
        {
            var root = new GameObject("Forest").transform;
            root.SetParent(parent, false);
            var cylinder = ProceduralArt.PrimitiveMesh(PrimitiveType.Cylinder);
            var sphere = ProceduralArt.PrimitiveMesh(PrimitiveType.Sphere);
            var cone = ProceduralArt.Cone(9);
            var chunks = new Dictionary<Vector2Int, Chunk>();
            var grid = new Dictionary<Vector2Int, Vector2>();
            const float cell = 4.2f;
            var colliders = new GameObject("TreeColliders").transform;
            colliders.SetParent(root, false);

            for (int attempt = 0; attempt < 3200 && _treePositions.Count < 560; attempt++)
            {
                var p = new Vector2(Rnd(-Size * 0.48f, Size * 0.48f), Rnd(-Size * 0.48f, Size * 0.48f));
                if (Vector2.Distance(p, _camp) < 24f) continue;
                if (Vector2.Distance(p, _ruins) < 12f) continue;
                if (Slope(p) > 26f) continue;
                // Mas denso lejos de las ruinas, claros ocasionales.
                float clearing = Mathf.PerlinNoise(p.x * 0.03f + _seed, p.y * 0.03f);
                if (clearing > 0.72f) continue;

                var cellKey = new Vector2Int(Mathf.FloorToInt(p.x / cell), Mathf.FloorToInt(p.y / cell));
                bool blocked = false;
                for (int dx = -1; dx <= 1 && !blocked; dx++)
                    for (int dz = -1; dz <= 1 && !blocked; dz++)
                        if (grid.TryGetValue(new Vector2Int(cellKey.x + dx, cellKey.y + dz), out var other) && Vector2.Distance(other, p) < cell)
                            blocked = true;
                if (blocked) continue;
                grid[cellKey] = p;
                _treePositions.Add(p);

                var g = GroundAt(p);
                float h = Rnd(7f, 13.5f);
                float yaw = Rnd(0f, 360f);
                var rot = Quaternion.Euler(0f, yaw, 0f);
                var chunkKey = new Vector2Int(Mathf.FloorToInt((p.x + Size * 0.5f) / 80f), Mathf.FloorToInt((p.y + Size * 0.5f) / 80f));
                if (!chunks.TryGetValue(chunkKey, out var ch)) { ch = new Chunk(); chunks[chunkKey] = ch; }

                float trunkR = h * 0.022f;
                ch.Trunks.Add(Instance(cylinder, Matrix4x4.TRS(g + Vector3.up * (h * 0.3f), rot, new Vector3(trunkR * 2f, h * 0.3f, trunkR * 2f))));
                bool pine = _rng.NextDouble() < 0.7;
                if (pine)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        float baseY = h * (0.22f + 0.21f * k);
                        float r = h * (0.30f - 0.07f * k) * Rnd(0.9f, 1.1f);
                        ch.Pine.Add(Instance(cone, Matrix4x4.TRS(g + Vector3.up * baseY, rot, new Vector3(r, h * 0.4f, r))));
                    }
                }
                else
                {
                    var list = _rng.NextDouble() < 0.18 ? ch.Autumn : ch.Broad;
                    float r = h * Rnd(0.3f, 0.38f);
                    list.Add(Instance(sphere, Matrix4x4.TRS(g + Vector3.up * (h * 0.72f), rot, new Vector3(r * 2f, r * 1.7f, r * 2f))));
                    list.Add(Instance(sphere, Matrix4x4.TRS(g + new Vector3(Rnd(-1f, 1f), h * 0.58f, Rnd(-1f, 1f)), rot, new Vector3(r * 1.3f, r * 1.1f, r * 1.3f))));
                }

                var col = new GameObject("T").AddComponent<CapsuleCollider>();
                col.transform.SetParent(colliders, false);
                col.transform.position = g + Vector3.up * 1.6f;
                col.radius = Mathf.Max(0.3f, trunkR * 1.3f);
                col.height = 3.2f;
                MarkForest(p, 2);
            }

            var bark = MaterialUtil.Lit(new Color(0.22f, 0.15f, 0.10f), 0.08f, 0f, ProceduralArt.NoiseTexture(64, new Color(0.15f, 0.1f, 0.07f), new Color(0.3f, 0.2f, 0.13f), 6f, _seed + 5, 1.6f));
            var pineMat = MaterialUtil.Lit(new Color(0.09f, 0.22f, 0.12f), 0.1f);
            var broadMat = MaterialUtil.Lit(new Color(0.22f, 0.33f, 0.11f), 0.1f);
            var autumnMat = MaterialUtil.Lit(new Color(0.62f, 0.38f, 0.10f), 0.1f);
            foreach (var kv in chunks)
            {
                Emit(root, "Trunks", kv.Value.Trunks, bark);
                Emit(root, "Pines", kv.Value.Pine, pineMat);
                Emit(root, "Broadleaf", kv.Value.Broad, broadMat);
                Emit(root, "Autumn", kv.Value.Autumn, autumnMat);
            }
        }

        static CombineInstance Instance(Mesh m, Matrix4x4 t) => new CombineInstance { mesh = m, transform = t };

        static void Emit(Transform parent, string name, List<CombineInstance> list, Material mat)
        {
            if (list.Count == 0) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(list.ToArray(), true, true);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
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
                    if (x >= 0 && y >= 0 && x < MapRes && y < MapRes) _forestMask[y, x] = true;
                }
        }

        // ---------- Rocas ----------
        void ScatterRocks(Transform parent)
        {
            var root = new GameObject("Rocks").transform;
            root.SetParent(parent, false);
            var rock = MaterialUtil.Lit(new Color(0.38f, 0.37f, 0.35f), 0.15f, 0f, ProceduralArt.NoiseTexture(64, new Color(0.3f, 0.3f, 0.29f), new Color(0.5f, 0.49f, 0.46f), 5f, _seed + 7, 1.5f));
            for (int i = 0; i < 90; i++)
            {
                var p = new Vector2(Rnd(-Size * 0.48f, Size * 0.48f), Rnd(-Size * 0.48f, Size * 0.48f));
                if (Vector2.Distance(p, _camp) < 16f) continue;
                var g = GroundAt(p);
                var go = MaterialUtil.Primitive(PrimitiveType.Sphere, "Rock", root, rock, true);
                float s = Rnd(0.6f, 2.2f);
                go.transform.position = g + Vector3.up * (s * 0.18f);
                go.transform.rotation = Quaternion.Euler(Rnd(0f, 25f), Rnd(0f, 360f), Rnd(0f, 25f));
                go.transform.localScale = new Vector3(s * Rnd(0.9f, 1.4f), s * 0.6f, s * Rnd(0.8f, 1.2f));
                go.isStatic = true;
            }
        }

        // ---------- Ruinas ----------
        void BuildRuins(Transform parent, WorldData data)
        {
            var root = new GameObject("Ruins").transform;
            root.SetParent(parent, false);
            var stone = MaterialUtil.Lit(new Color(0.5f, 0.48f, 0.43f), 0.12f, 0f, ProceduralArt.NoiseTexture(128, new Color(0.36f, 0.35f, 0.32f), new Color(0.6f, 0.58f, 0.52f), 7f, _seed + 9, 1.6f));
            var moss = MaterialUtil.Lit(new Color(0.22f, 0.32f, 0.14f), 0.05f);
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
                var col = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Column", root, stone, true);
                col.transform.position = pos + Vector3.up * (h * 0.5f + 0.3f);
                col.transform.localScale = new Vector3(0.9f, h * 0.5f, 0.9f);
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
            var go = MaterialUtil.Primitive(PrimitiveType.Cube, name, parent, mat, true);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = scale;
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
            foreach (var i in items) if (i.Id != "golden_torc" && i.Id != "expedition_journal") picks.Add(i);

            int n = 0;
            for (int attempt = 0; attempt < 900 && n < 28; attempt++)
            {
                bool nearRuins = _rng.NextDouble() < 0.6;
                Vector2 p = nearRuins
                    ? _ruins + new Vector2(Rnd(-34f, 34f), Rnd(-34f, 34f))
                    : new Vector2(Rnd(-Size * 0.42f, Size * 0.42f), Rnd(-Size * 0.42f, Size * 0.42f));
                if (Vector2.Distance(p, _camp) < 30f || Slope(p) > 24f) continue;
                if (nearRuins && Mathf.Abs(p.x - _ruins.x) < 9f && Mathf.Abs(p.y - _ruins.y) < 9f) continue;
                if (TooClose(placed, p, 5f) || NearTree(p, 1.6f)) continue;
                var info = LootTable.Pick(picks, _rng);
                Add(root, data, placed, p, info.Id, "forest_site_" + n.ToString("00"), 0.3f + (int)info.Rarity * 0.12f + Rnd(0f, 0.15f), _rng.NextDouble() < 0.5);
                n++;
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
            foreach (var l in Object.FindObjectsOfType<Light>())
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
            for (int y = 0; y < MapRes; y++)
            {
                for (int x = 0; x < MapRes; x++)
                {
                    float nx = x / (MapRes - 1f), ny = y / (MapRes - 1f);
                    float h = _td.GetInterpolatedHeight(nx, ny) / MaxHeight;
                    float hx = _td.GetInterpolatedHeight(Mathf.Min(1f, nx + 0.01f), ny) / MaxHeight;
                    float shade = Mathf.Clamp01(0.5f + (h - hx) * 28f);
                    Color baseC = Color.Lerp(new Color(0.20f, 0.30f, 0.16f), new Color(0.50f, 0.46f, 0.30f), Mathf.InverseLerp(0.1f, 0.5f, h));
                    if (_forestMask[y, x]) baseC = Color.Lerp(baseC, new Color(0.08f, 0.2f, 0.1f), 0.7f);
                    px[y * MapRes + x] = baseC * (0.65f + shade * 0.7f);
                    px[y * MapRes + x].a = 1f;
                }
            }
            tex.SetPixels(px);
            tex.Apply(false);
            return tex;
        }
    }
}
