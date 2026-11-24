using UnityEngine;
using HuesoOxido.Survival;

namespace HuesoOxido.Core
{
    /// <summary>Campamento base: mostrador, carpa, fogata, surtidor y museo. Placeholder con primitivas.</summary>
    public static class CampBuilder
    {
        public static Vector3 Build(Transform parent, Vector3 center, WorldData data, GameClock clock = null)
        {
            var root = new GameObject("Camp").transform;
            root.SetParent(parent, false);
            root.position = center;

            var wood = ArtMaterials.Textured("wood_planks", new Color(0.95f, 0.9f, 0.85f), 0.1f);
            var cloth = MaterialUtil.Lit(new Color(0.55f, 0.50f, 0.34f), 0.05f);
            var red = MaterialUtil.Lit(new Color(0.55f, 0.14f, 0.10f), 0.35f, 0.2f);
            var steel = MaterialUtil.Lit(new Color(0.45f, 0.47f, 0.5f), 0.5f, 0.6f);
            var stone = MaterialUtil.Lit(new Color(0.4f, 0.39f, 0.37f), 0.1f);

            // Mostrador / puesto de compra-venta.
            var shop = new GameObject("ShopStall").transform;
            shop.SetParent(root, false);
            shop.localPosition = new Vector3(0f, 0f, 7f);
            Part(PrimitiveType.Cube, "Counter", shop, wood, new Vector3(0f, 0.55f, 0f), new Vector3(3.2f, 1.1f, 1.1f), true);
            Part(PrimitiveType.Cube, "Awning", shop, cloth, new Vector3(0f, 2.5f, 0.2f), new Vector3(3.8f, 0.08f, 2.4f), false).transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            Part(PrimitiveType.Cylinder, "PostL", shop, wood, new Vector3(-1.8f, 1.25f, -0.9f), new Vector3(0.12f, 1.25f, 0.12f), false);
            Part(PrimitiveType.Cylinder, "PostR", shop, wood, new Vector3(1.8f, 1.25f, -0.9f), new Vector3(0.12f, 1.25f, 0.12f), false);
            Part(PrimitiveType.Cube, "Scale", shop, steel, new Vector3(-0.8f, 1.2f, 0f), new Vector3(0.5f, 0.2f, 0.4f), false);
            Part(PrimitiveType.Cube, "Crate", shop, wood, new Vector3(1.0f, 1.3f, 0f), new Vector3(0.6f, 0.4f, 0.5f), false);
            data.ShopPoint = Trigger(shop, "ShopTrigger", new Vector3(0f, 1.2f, -0.6f), new Vector3(4f, 2.4f, 3f));

            // Carpa de descanso.
            var tent = new GameObject("Tent").transform;
            tent.SetParent(root, false);
            tent.localPosition = new Vector3(-9f, 0f, 2f);
            Part(PrimitiveType.Cube, "RoofL", tent, cloth, new Vector3(-0.95f, 1.1f, 0f), new Vector3(2.4f, 0.06f, 3.4f), false).transform.localRotation = Quaternion.Euler(0f, 0f, 48f);
            Part(PrimitiveType.Cube, "RoofR", tent, cloth, new Vector3(0.95f, 1.1f, 0f), new Vector3(2.4f, 0.06f, 3.4f), false).transform.localRotation = Quaternion.Euler(0f, 0f, -48f);
            Part(PrimitiveType.Cube, "Floor", tent, wood, new Vector3(0f, 0.04f, 0f), new Vector3(3f, 0.05f, 3.4f), false);
            Part(PrimitiveType.Cube, "Bedroll", tent, red, new Vector3(0f, 0.15f, 0f), new Vector3(0.8f, 0.2f, 2.2f), false);
            data.TentPoint = Trigger(tent, "TentTrigger", new Vector3(0f, 1f, 0f), new Vector3(3.6f, 2.2f, 4f));

            // Fogata.
            var fire = new GameObject("Campfire").transform;
            fire.SetParent(root, false);
            fire.localPosition = new Vector3(0f, 0f, -1.5f);
            var pit = PropLibrary.Spawn("stone_fire_pit", fire, Vector3.zero, 0f, 1.25f, false, 0.15f);
            if (pit == null)
            {
                for (int i = 0; i < 9; i++)
                {
                    float a = i * Mathf.PI * 2f / 9f;
                    Part(PrimitiveType.Sphere, "Stone", fire, stone, new Vector3(Mathf.Cos(a) * 0.8f, 0.15f, Mathf.Sin(a) * 0.8f), Vector3.one * 0.32f, false);
                }
                for (int i = 0; i < 3; i++)
                {
                    var log = Part(PrimitiveType.Cylinder, "Log", fire, wood, new Vector3(0f, 0.22f, 0f), new Vector3(0.12f, 0.5f, 0.12f), false);
                    log.transform.localRotation = Quaternion.Euler(70f, i * 120f, 0f);
                }
            }
            else
            {
                // Troncos reales (malla con corteza) apoyados en el pozo.
                var barkMat = ArtMaterials.Textured("bark_pine", new Color(0.7f, 0.65f, 0.6f), 0.05f);
                for (int i = 0; i < 3; i++)
                {
                    var log = new GameObject("Log"); log.transform.SetParent(fire, false);
                    log.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                    log.transform.localRotation = Quaternion.Euler(72f, i * 120f, 0f);
                    log.AddComponent<MeshFilter>().sharedMesh = MeshKit.Trunk(0.9f, 0.07f, 0.06f, 8, 2, 1f, 0f, i);
                    log.AddComponent<MeshRenderer>().sharedMaterial = barkMat;
                }
            }
            var flame = CampfireFx.Create(fire, new Vector3(0f, 0.15f, 0f));
            var lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(fire, false);
            lightGo.transform.localPosition = new Vector3(0f, 1f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.6f, 0.28f);
            light.range = 16f;
            light.intensity = 2.4f;
            light.shadows = LightShadows.None;
            fire.gameObject.AddComponent<CampfireFlicker>().Setup(light, flame);

            // Surtidor de combustible.
            var pump = new GameObject("FuelPump").transform;
            pump.SetParent(root, false);
            pump.localPosition = new Vector3(13f, 0f, -2f);
            Part(PrimitiveType.Cube, "Body", pump, red, new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 1.8f, 0.6f), true);
            Part(PrimitiveType.Cylinder, "Hose", pump, steel, new Vector3(0.5f, 0.8f, 0f), new Vector3(0.08f, 0.5f, 0.08f), false);
            data.FuelPoint = Trigger(pump, "FuelTrigger", new Vector3(0f, 1f, 0f), new Vector3(2.4f, 2.2f, 2.4f));

            // Museo (vitrina abierta).
            var museum = new GameObject("Museum").transform;
            museum.SetParent(root, false);
            museum.localPosition = new Vector3(-3f, 0f, 14f);
            Part(PrimitiveType.Cube, "Roof", museum, cloth, new Vector3(0f, 3f, -0.6f), new Vector3(7f, 0.1f, 4.2f), false);
            for (int i = 0; i < 4; i++)
                Part(PrimitiveType.Cylinder, "Post", museum, wood, new Vector3(i < 2 ? -3.3f : 3.3f, 1.5f, i % 2 == 0 ? 1.2f : -2.4f), new Vector3(0.12f, 1.5f, 0.12f), false);
            data.MuseumPoint = Trigger(museum, "MuseumTrigger", new Vector3(0f, 1.2f, -0.6f), new Vector3(7f, 2.6f, 4.2f));

            // Props reales (Poly Haven, CC0).
            PropLibrary.Spawn("wooden_crate_01", root, new Vector3(2.6f, 0f, 6.9f), 12f, 1f, true);
            PropLibrary.Spawn("wooden_crate_02", root, new Vector3(3.9f, 0f, 7.2f), -20f, 1f, true);
            PropLibrary.Spawn("wooden_crate_01", root, new Vector3(3.2f, 0.62f, 7.0f), 35f, 0.85f, false);
            PropLibrary.Spawn("barrel_03", root, new Vector3(-2.9f, 0f, 7.4f), 0f, 1f, true);
            PropLibrary.Spawn("barrel_03", root, new Vector3(-3.9f, 0f, 6.8f), 40f, 1f, true);
            PropLibrary.Spawn("wooden_bucket_01", root, new Vector3(-1.5f, 0f, 5.4f), 25f, 1f, false);
            PropLibrary.Spawn("wooden_ladder_02", root, new Vector3(-5.2f, 0f, 14.6f), 90f, 1f, false);
            var lamp = PropLibrary.Spawn("vintage_oil_lamp", shop, new Vector3(-0.8f, 1.1f, 0.05f), 20f, 1f, false, 0.4f);
            var lantern = PropLibrary.Spawn("wooden_lantern_01", tent, new Vector3(1.9f, 0f, 1.3f), 0f, 1f, false, 0.35f);
            AddNightLight(lamp != null ? lamp.transform : shop, new Vector3(0f, 0.25f, 0f), new Color(1f, 0.72f, 0.38f), 1.6f, 9f, clock);
            AddNightLight(lantern != null ? lantern.transform : tent, new Vector3(0f, 0.3f, 0f), new Color(1f, 0.70f, 0.35f), 1.8f, 10f, clock);
            AddNightLight(root, new Vector3(13f, 2.4f, -2f), new Color(1f, 0.85f, 0.6f), 1.3f, 10f, clock);

            // Cartel de entrada.
            Part(PrimitiveType.Cube, "Sign", root, wood, new Vector3(0f, 1.2f, 16f), new Vector3(2.2f, 0.9f, 0.1f), false);
            Part(PrimitiveType.Cylinder, "SignPost", root, wood, new Vector3(0f, 0.6f, 16f), new Vector3(0.1f, 0.6f, 0.1f), false);
            return center;
        }

        static void AddNightLight(Transform parent, Vector3 localPos, Color color, float intensity, float range, GameClock clock)
        {
            var go = new GameObject("NightLight");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color; l.range = range; l.intensity = 0f; l.shadows = LightShadows.None;
            if (clock != null) go.AddComponent<NightLight>().Initialize(l, clock, intensity);
            else l.intensity = 0f;
        }

        static GameObject Part(PrimitiveType t, string name, Transform parent, Material mat, Vector3 pos, Vector3 scale, bool collider)
        {
            var go = MaterialUtil.Primitive(t, name, parent, mat, collider);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.isStatic = true;
            return go;
        }

        static Transform Trigger(Transform parent, string name, Vector3 localPos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            return go.transform;
        }
    }
}
