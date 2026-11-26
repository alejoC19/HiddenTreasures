using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>Antorcha de las ruinas: luz que parpadea y se intensifica de noche, con llama de particulas.</summary>
    public sealed class TorchLight : MonoBehaviour
    {
        Light _light;
        float _seed;
        System.Func<float> _night;

        public void Setup(Light light, System.Func<float> night) { _light = light; _night = night; _seed = Random.value * 100f; }

        void Update()
        {
            if (_light == null) return;
            float n = Mathf.PerlinNoise(_seed, Time.time * 8f), n2 = Mathf.PerlinNoise(_seed + 17f, Time.time * 19f);
            float night = _night != null ? _night() : 1f;
            _light.intensity = (0.9f + n * 0.8f + n2 * 0.3f) * (0.35f + 1.4f * night);
            _light.color = Color.Lerp(new Color(1f, 0.48f, 0.18f), new Color(1f, 0.68f, 0.32f), n);
        }
    }
}
