using UnityEngine;
using HuesoOxido.Detection;

namespace HuesoOxido.Core
{
    public sealed class WorldData
    {
        public HeightField Field;
        public RoadPath Road;
        public GameObject Ground;

        public float HeightAt(float x, float z) => Field.HeightAt(x, z);
        public float HeightAt(Vector3 p) => Field.HeightAt(p.x, p.z);

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
