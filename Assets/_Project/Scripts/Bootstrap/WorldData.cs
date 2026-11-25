using UnityEngine;
using HuesoOxido.Detection;

namespace HuesoOxido.Core
{
    public sealed class WorldData
    {
        public HeightField Field;
        public RoadPath Road;
        public GameObject Ground;
        public InstancedVegetation Vegetation;

        public float HeightAt(float x, float z) => Field.HeightAt(x, z);
        public float HeightAt(Vector3 p) => Field.HeightAt(p.x, p.z);

        /// <summary>Terreno blando donde se marcan las pisadas: camino de tierra, explanada del campamento y suelo de las ruinas.</summary>
        public bool IsSoftGround(float x, float z)
        {
            if (Road != null && Road.DistanceTo(x, z) < 2.4f) return true;
            float dc = Mathf.Sqrt((x - CampPosition.x) * (x - CampPosition.x) + (z - CampPosition.z) * (z - CampPosition.z));
            if (dc < 13f) return true;
            float dr = Mathf.Sqrt((x - RuinsCenter.x) * (x - RuinsCenter.x) + (z - RuinsCenter.z) * (z - RuinsCenter.z));
            return dr < 9f;
        }

        /// <summary>Superficie bajo los pies: piedra en las ruinas, tierra en camino/campamento, pasto en el resto.</summary>
        public SurfaceKind SurfaceAt(float x, float z)
        {
            float dr = Mathf.Sqrt((x - RuinsCenter.x) * (x - RuinsCenter.x) + (z - RuinsCenter.z) * (z - RuinsCenter.z));
            if (dr < 8f) return SurfaceKind.Stone;
            return IsSoftGround(x, z) ? SurfaceKind.Dirt : SurfaceKind.Grass;
        }

        public Vector3 CampPosition;
        public Vector3 RuinsCenter;
        public Vector3 PlayerSpawn;
        public Vector3 VehicleSpawn;
        public SiteRegistry Sites = new SiteRegistry();
        public Texture2D MapTexture;
        public Rect MapBounds;
        public Light Sun, Moon;
        public Material Skybox;
        public Transform ShopPoint, TentPoint, FuelPoint, MuseumPoint;
    }
}
