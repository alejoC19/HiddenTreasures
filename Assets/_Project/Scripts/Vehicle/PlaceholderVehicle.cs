using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Vehicle
{
    /// <summary>Carroceria placeholder (camioneta de expedicion) armada con primitivas.</summary>
    public static class PlaceholderVehicle
    {
        public const float WheelRadius = 0.38f;

        /// <summary>Crea el cuerpo y devuelve los 4 pivotes de rueda (delanteras primero). Agrega el BoxCollider.</summary>
        public static Transform[] Build(Transform root, Vector3[] mounts)
        {
            var paint = MaterialUtil.Lit(new Color(0.34f, 0.40f, 0.24f), 0.45f, 0.3f);
            var dark = MaterialUtil.Lit(new Color(0.08f, 0.08f, 0.09f), 0.2f);
            var glass = MaterialUtil.Lit(new Color(0.18f, 0.26f, 0.32f), 0.9f, 0.1f);
            var rack = MaterialUtil.Lit(new Color(0.15f, 0.15f, 0.16f), 0.3f, 0.6f);
            var lamp = MaterialUtil.Emissive(new Color(1f, 0.93f, 0.7f));

            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.55f, 0f);
            box.size = new Vector3(1.9f, 0.9f, 3.9f);

            Part(PrimitiveType.Cube, "Chassis", root, paint, new Vector3(0f, 0.5f, 0f), new Vector3(1.8f, 0.55f, 3.8f));
            Part(PrimitiveType.Cube, "Cabin", root, paint, new Vector3(0f, 1.05f, 0.55f), new Vector3(1.7f, 0.6f, 1.7f));
            Part(PrimitiveType.Cube, "Windshield", root, glass, new Vector3(0f, 1.07f, 1.41f), new Vector3(1.55f, 0.45f, 0.04f));
            Part(PrimitiveType.Cube, "SideGlassL", root, glass, new Vector3(-0.86f, 1.07f, 0.55f), new Vector3(0.04f, 0.4f, 1.3f));
            Part(PrimitiveType.Cube, "SideGlassR", root, glass, new Vector3(0.86f, 1.07f, 0.55f), new Vector3(0.04f, 0.4f, 1.3f));
            Part(PrimitiveType.Cube, "Bed", root, dark, new Vector3(0f, 0.82f, -1.25f), new Vector3(1.65f, 0.08f, 1.3f));
            Part(PrimitiveType.Cube, "Roof", root, rack, new Vector3(0f, 1.38f, 0.55f), new Vector3(1.5f, 0.05f, 1.4f));
            Part(PrimitiveType.Cube, "Bumper", root, dark, new Vector3(0f, 0.4f, 1.95f), new Vector3(1.8f, 0.18f, 0.12f));
            Part(PrimitiveType.Sphere, "HeadlightL", root, lamp, new Vector3(-0.6f, 0.65f, 1.9f), new Vector3(0.22f, 0.16f, 0.1f));
            Part(PrimitiveType.Sphere, "HeadlightR", root, lamp, new Vector3(0.6f, 0.65f, 1.9f), new Vector3(0.22f, 0.16f, 0.1f));

            var wheels = new Transform[mounts.Length];
            for (int i = 0; i < mounts.Length; i++)
            {
                var pivot = new GameObject("Wheel" + i).transform;
                pivot.SetParent(root, false);
                pivot.localPosition = mounts[i] + Vector3.down * 0.45f;
                var tire = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Tire", pivot, dark);
                tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                tire.transform.localScale = new Vector3(WheelRadius * 2f, 0.14f, WheelRadius * 2f);
                var hub = MaterialUtil.Primitive(PrimitiveType.Cylinder, "Hub", pivot, rack);
                hub.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                hub.transform.localScale = new Vector3(0.3f, 0.16f, 0.3f);
                wheels[i] = pivot;
            }
            return wheels;
        }

        static GameObject Part(PrimitiveType t, string name, Transform parent, Material m, Vector3 pos, Vector3 scale)
        {
            var go = MaterialUtil.Primitive(t, name, parent, m);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            return go;
        }
    }
}
