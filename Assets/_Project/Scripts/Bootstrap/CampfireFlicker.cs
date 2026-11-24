using UnityEngine;

namespace HuesoOxido.Core
{
    public sealed class CampfireFlicker : MonoBehaviour
    {
        Light _light;
        Transform _flame;
        float _seed;

        public void Setup(Light light, Transform flame) { _light = light; _flame = flame; _seed = Random.value * 100f; }

        void Update()
        {
            float n = Mathf.PerlinNoise(_seed, Time.time * 7f);
            _light.intensity = 2.0f + n * 1.0f;
            float s = 0.85f + n * 0.4f;
            _flame.localScale = new Vector3(s, 0.8f + n * 0.6f, s);
        }
    }
}
