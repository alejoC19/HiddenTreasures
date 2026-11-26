using System;
using UnityEngine;

namespace HuesoOxido.Vehicle
{
    /// <summary>
    /// Luces del vehiculo: dos faros delanteros (focos) y dos luces traseras rojas (mas intensas al frenar). Se encienden cuando hay
    /// conductor y esta oscuro (noche, niebla densa o lluvia). Sin sombras para no costar rendimiento.
    /// </summary>
    public sealed class VehicleLights : MonoBehaviour
    {
        Light[] _head = new Light[2], _tail = new Light[2];
        Func<bool> _driver;
        Func<float> _darkness;
        Func<bool> _braking;
        float _level;

        /// <param name="darkness">0 = pleno dia, 1 = noche cerrada.</param>
        public void Initialize(Vector3[] front, Vector3[] rear, Func<bool> driverPresent, Func<float> darkness, Func<bool> braking)
        {
            _driver = driverPresent; _darkness = darkness; _braking = braking;
            for (int i = 0; i < 2; i++)
            {
                _head[i] = Make("Headlight" + i, LightType.Spot, front[i], Quaternion.Euler(4f, 0f, 0f), new Color(1f, 0.95f, 0.82f), 48f, 3.2f);
                _head[i].spotAngle = 64f; _head[i].innerSpotAngle = 30f;
                _tail[i] = Make("Taillight" + i, LightType.Point, rear[i], Quaternion.identity, new Color(1f, 0.08f, 0.04f), 5f, 0.6f);
            }
            Apply(0f);
        }

        Light Make(string name, LightType type, Vector3 localPos, Quaternion localRot, Color color, float range, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPos; go.transform.localRotation = localRot;
            var l = go.AddComponent<Light>();
            l.type = type; l.color = color; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.Auto;
            return l;
        }

        void Apply(float level)
        {
            bool on = level > 0.02f;
            for (int i = 0; i < 2; i++)
            {
                _head[i].enabled = on; _head[i].intensity = 3.2f * level;
                _tail[i].enabled = on || (_braking != null && _braking());
                float brake = _braking != null && _braking() ? 1.6f : 0.5f;
                _tail[i].intensity = 0.9f * brake * Mathf.Max(level, _braking != null && _braking() ? 0.5f : 0f);
            }
        }

        void Update()
        {
            float target = _driver != null && _driver() ? Mathf.Clamp01((_darkness != null ? _darkness() : 0f) * 1.6f) : 0f;
            _level = Mathf.MoveTowards(_level, target, Time.deltaTime * 3f);
            Apply(_level);
        }
    }
}
