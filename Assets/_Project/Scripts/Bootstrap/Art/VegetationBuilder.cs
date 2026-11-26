using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Sotobosque: pasto, helechos, arbustos, flores, troncos caidos y tocones. Todo en mallas combinadas por zonas
    /// (pocos draw calls) y con texturas de follaje pintadas por codigo.
    /// </summary>
    public sealed class VegetationBuilder
    {
        readonly HeightField _field;
        readonly float[,] _forest;
        readonly PerlinNoise _noise;
        readonly System.Random _rng;
        readonly int _seed;
        InstancedVegetation _veg;
        readonly Dictionary<string, InstancedVegetation.Layer> _layers = new Dictionary<string, InstancedVegetation.Layer>();

        readonly BiomeProfile _bp;
        /// <summary>Altura del agua del bioma (la fija el constructor del mundo antes de plantar).</summary>
        public float WaterLevel = float.NegativeInfinity;

        public VegetationBuilder(HeightField field, float[,] forestMask, PerlinNoise noise, int seed, BiomeProfile profile = null)
        {
            _bp = profile ?? Biomes.Forest;
            _field = field; _forest = forestMask; _noise = noise; _seed = seed; _rng = new System.Random(seed + 4242);
        }

        float Rnd(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        float Forest(float x, float z)
        {
            int n = _forest.GetLength(0);
            int ix = Mathf.Clamp(Mathf.RoundToInt((x + HeightField.Half) / HeightField.Size * (n - 1)), 0, n - 1);
            int iz = Mathf.Clamp(Mathf.RoundToInt((z + HeightField.Half) / HeightField.Size * (n - 1)), 0, n - 1);
            return _forest[iz, ix];
        }

        void Add(string key, Vector3 pos, Quaternion rot, Vector3 scale)
            => _veg.Add(_layers[key], Matrix4x4.TRS(pos, rot, scale));

        /// <summary>Cruz de cuadrados verticales (n planos) con la base en y=0 y UV completas.</summary>
        static Mesh CrossQuads(int planes, float width, float height, float bendTop)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int p = 0; p < planes; p++)
            {
                float a = p * Mathf.PI / planes;
                Vector3 side = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (width * 0.5f);
                Vector3 nrm = new Vector3(0f, 0.55f, 0f);
                int b = v.Count;
                for (int row = 0; row < 3; row++)
                {
                    float k = row * 0.5f;
                    Vector3 c = new Vector3(0f, height * k, 0f) + new Vector3(Mathf.Sin(a), 0f, -Mathf.Cos(a)) * (bendTop * k * k);
                    float w = row == 2 ? 0.8f : 1f;
                    v.Add(c - side * w); v.Add(c + side * w);
                    n.Add((Vector3.up * 0.7f + new Vector3(-side.z, 0f, side.x).normalized * 0.3f).normalized); n.Add((Vector3.up * 0.7f + new Vector3(side.z, 0f, -side.x).normalized * 0.3f).normalized);
                    uv.Add(new Vector2(0f, k)); uv.Add(new Vector2(1f, k));
                }
                for (int r = 0; r < 2; r++) { int i = b + r * 2; t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i + 1); t.Add(i + 2); t.Add(i + 3); }
            }
            var m = new Mesh { name = "CrossQuads" };
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        bool Allowed(float x, float z, float maxSlope, float roadMin)
        {
            if (Mathf.Abs(x) > HeightField.Half - 6f || Mathf.Abs(z) > HeightField.Half - 6f) return false;
            if (_field.Road != null && _field.Road.DistanceTo(x, z) < roadMin) return false;
            float dc = Mathf.Sqrt((x - WorldLayout.CampX) * (x - WorldLayout.CampX) + (z - WorldLayout.CampZ) * (z - WorldLayout.CampZ));
            if (dc < 13f) return false;
            float dr = Mathf.Sqrt((x - WorldLayout.RuinsX) * (x - WorldLayout.RuinsX) + (z - WorldLayout.RuinsZ) * (z - WorldLayout.RuinsZ));
            if (dr < 7f) return false;
            if (_bp.WaterFraction > 0f && _field.HeightAt(x, z) < WaterLevel + 0.2f) return false;
            return _field.SlopeDegrees(x, z) < maxSlope;
        }

        /// <summary>Cantidades por especie (densidad del bosque salvaje).</summary>
        public const int GrassCount = 26000, TallGrassCount = 9000, FernCount = 6500, BushCount = 1600, FlowerCount = 2600;

        public void Build(Transform parent, InstancedVegetation veg)
        {
            _veg = veg;
            var root = new GameObject("Undergrowth").transform;
            root.SetParent(parent, false);

            float dens = _bp.UnderstoryDensity;
            var vt = new Color(_bp.VegTint.R, _bp.VegTint.G, _bp.VegTint.B);
            int nGrass = (int)(GrassCount * dens), nTall = (int)(TallGrassCount * dens), nFern = _bp.IsDefault ? FernCount : (int)(FernCount * dens * 0.5f), nBush = (int)(BushCount * dens), nFlower = _bp.IsDefault ? FlowerCount : (int)(FlowerCount * dens * 0.4f);
            var grassMesh = CrossQuads(3, 1.25f, 0.95f, 0.18f);
            var tallMesh = CrossQuads(3, 1.0f, 1.9f, 0.45f);
            var fernMesh = CrossQuads(2, 1.7f, 1.0f, 0.25f);
            var bushMesh = CrossQuads(4, 1.7f, 1.4f, 0f);
            var flowerMesh = CrossQuads(2, 0.34f, 0.55f, 0.04f);

            var grassTex = FoliageTextures.Grass(_seed + 11);
            var fernTex = FoliageTextures.Fern(_seed + 12);
            var leafTex = FoliageTextures.LeafCluster(_seed + 13);
            var flowerTex = FoliageTextures.Flower();
            void Layer(string key, Mesh mesh, Material mat, float dist) => _layers[key] = _veg.AddLayer(key, mesh, mat, dist, false);
            Layer("grass_a", grassMesh, ArtMaterials.Foliage(grassTex, new Color(0.85f, 1f, 0.75f) * vt, "gA"), 60f);
            Layer("grass_b", grassMesh, ArtMaterials.Foliage(grassTex, new Color(1f, 0.95f, 0.65f) * vt, "gB"), 60f);
            Layer("tall_a", tallMesh, ArtMaterials.Foliage(grassTex, new Color(0.62f, 0.78f, 0.5f) * vt, "tA"), 75f);
            Layer("tall_b", tallMesh, ArtMaterials.Foliage(grassTex, new Color(0.78f, 0.78f, 0.45f) * vt, "tB"), 75f);
            Layer("fern", fernMesh, ArtMaterials.Foliage(fernTex, new Color(0.85f, 1f, 0.8f) * vt, "fern"), 85f);
            Layer("bush", bushMesh, ArtMaterials.Foliage(leafTex, new Color(0.62f, 0.8f, 0.55f) * vt, "bush"), 120f);
            Layer("flower_white", flowerMesh, ArtMaterials.Foliage(flowerTex, new Color(1f, 1f, 1f) * vt, "fW"), 40f);
            Layer("flower_yellow", flowerMesh, ArtMaterials.Foliage(flowerTex, new Color(1f, 0.86f, 0.25f) * vt, "fY"), 40f);
            Layer("flower_violet", flowerMesh, ArtMaterials.Foliage(flowerTex, new Color(0.78f, 0.6f, 1f) * vt, "fV"), 40f);

            int grass = 0;
            for (int i = 0; i < 130000 && grass < nGrass; i++)
            {
                float x = Rnd(-HeightField.Half, HeightField.Half), z = Rnd(-HeightField.Half, HeightField.Half);
                if (!Allowed(x, z, 34f, 1.6f)) continue;
                float f = Forest(x, z);
                float dn = _noise.Fbm(x * 0.05f + 3f, z * 0.05f + 9f, 2);
                if (_rng.NextDouble() > 0.5 + dn * 0.8 - f * 0.25) continue;
                float s = Rnd(0.7f, 1.5f);
                Add(_rng.NextDouble() < 0.7 ? "grass_a" : "grass_b", new Vector3(x, _field.HeightAt(x, z) - 0.03f, z), Quaternion.Euler(0f, Rnd(0f, 360f), 0f), new Vector3(s, s * Rnd(0.8f, 1.3f), s));
                grass++;
            }
            // Pasto alto: mas denso bajo los arboles y en manchones, casi nunca sobre el camino.
            int tall = 0;
            for (int i = 0; i < 90000 && tall < nTall; i++)
            {
                float x = Rnd(-HeightField.Half, HeightField.Half), z = Rnd(-HeightField.Half, HeightField.Half);
                if (!Allowed(x, z, 30f, 2.4f)) continue;
                float patch = _noise.Fbm(x * 0.035f + 41f, z * 0.035f + 17f, 3);
                if (_rng.NextDouble() > 0.1 + patch * 0.9 + Forest(x, z) * 0.35 - 0.35) continue;
                float s = Rnd(0.8f, 1.45f);
                Add(_rng.NextDouble() < 0.6 ? "tall_a" : "tall_b", new Vector3(x, _field.HeightAt(x, z) - 0.03f, z), Quaternion.Euler(0f, Rnd(0f, 360f), 0f), new Vector3(s, s * Rnd(0.85f, 1.3f), s));
                tall++;
            }
            int ferns = 0;
            for (int i = 0; i < 70000 && ferns < nFern; i++)
            {
                float x = Rnd(-HeightField.Half, HeightField.Half), z = Rnd(-HeightField.Half, HeightField.Half);
                if (!Allowed(x, z, 32f, 2.4f)) continue;
                if (_rng.NextDouble() > 0.2 + Forest(x, z) * 0.95) continue;
                float s = Rnd(0.8f, 1.6f);
                Add("fern", new Vector3(x, _field.HeightAt(x, z) - 0.02f, z), Quaternion.Euler(0f, Rnd(0f, 360f), 0f), new Vector3(s, s, s));
                ferns++;
            }
            int bushes = 0;
            for (int i = 0; i < 30000 && bushes < nBush; i++)
            {
                float x = Rnd(-HeightField.Half, HeightField.Half), z = Rnd(-HeightField.Half, HeightField.Half);
                if (!Allowed(x, z, 28f, 3.0f)) continue;
                if (_rng.NextDouble() > 0.35 + Forest(x, z) * 0.7) continue;
                float s = Rnd(0.8f, 1.9f);
                Add("bush", new Vector3(x, _field.HeightAt(x, z) + 0.45f * s, z), Quaternion.Euler(0f, Rnd(0f, 360f), 0f), new Vector3(s, s, s));
                bushes++;
            }
            int flowers = 0;
            for (int i = 0; i < 60000 && flowers < nFlower; i++)
            {
                float x = Rnd(-HeightField.Half, HeightField.Half), z = Rnd(-HeightField.Half, HeightField.Half);
                if (!Allowed(x, z, 26f, 1.4f)) continue;
                if (_noise.Fbm(x * 0.04f + 70f, z * 0.04f + 30f, 2) < 0.52f) continue; // en manchones
                string key = _rng.NextDouble() < 0.55 ? "flower_white" : (_rng.NextDouble() < 0.7 ? "flower_yellow" : "flower_violet");
                float s = Rnd(0.8f, 1.4f);
                Add(key, new Vector3(x, _field.HeightAt(x, z) - 0.02f, z), Quaternion.Euler(0f, Rnd(0f, 360f), 0f), new Vector3(s, s, s));
                flowers++;
            }
            Debug.Log("[VEG] pasto=" + grass + " pasto alto=" + tall + " helechos=" + ferns + " arbustos=" + bushes + " flores=" + flowers);

            BuildLogs(root);
        }

        void BuildLogs(Transform root)
        {
            var bark = ArtMaterials.Textured("bark_pine", new Color(0.8f, 0.76f, 0.7f), 0.06f);
            for (int i = 0, placed = 0; i < 400 && placed < 42; i++)
            {
                float x = Rnd(-HeightField.Half + 12f, HeightField.Half - 12f), z = Rnd(-HeightField.Half + 12f, HeightField.Half - 12f);
                if (!Allowed(x, z, 18f, 3.5f)) continue;
                bool log = _rng.NextDouble() < 0.6;
                float h = log ? Rnd(2.5f, 5.5f) : Rnd(0.35f, 0.9f), r = log ? Rnd(0.18f, 0.34f) : Rnd(0.22f, 0.4f);
                var go = new GameObject(log ? "FallenLog" : "Stump");
                go.transform.SetParent(root, false);
                float y = _field.HeightAt(x, z);
                go.transform.position = new Vector3(x, y + (log ? r * 0.7f : 0f), z);
                go.transform.rotation = log ? Quaternion.Euler(0f, Rnd(0f, 360f), 0f) * Quaternion.Euler(0f, 0f, 90f) : Quaternion.Euler(Rnd(-3f, 3f), Rnd(0f, 360f), Rnd(-3f, 3f));
                var mesh = MeshKit.Trunk(h, r, r * (log ? 0.82f : 0.9f), 10, log ? 5 : 2, 1.4f, log ? 0.04f * h : 0f, i);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = bark;
                var cc = go.AddComponent<CapsuleCollider>();
                cc.radius = r; cc.height = h; cc.center = new Vector3(0f, h * 0.5f, 0f);
                if (log) cc.direction = 1;
                go.isStatic = true;
                placed++;
            }
        }
    }
}
