using UnityEngine;
using HuesoOxido.Detection;

namespace HuesoOxido.Core
{
    public sealed class WorldData
    {
        public Terrain Terrain;
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
