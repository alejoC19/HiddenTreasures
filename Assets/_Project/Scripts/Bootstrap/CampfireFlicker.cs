using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>Parpadeo de la luz del fuego y variacion de la tasa de llamas segun ruido.</summary>
    public sealed class CampfireFlicker : MonoBehaviour
    {
        Light _light;
        ParticleSystem _flames;
        float _seed;

        public void Setup(Light light, Transform fireFx)
        {
            _light = light;
            _seed = Random.value * 100f;
            if (fireFx != null)
            {
                var t = fireFx.Find("Flames");
                if (t != null) _flames = t.GetComponent<ParticleSystem>();
            }
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(_seed, Time.time * 7f);
            float n2 = Mathf.PerlinNoise(_seed + 31f, Time.time * 17f);
            if (_light != null)
            {
                _light.intensity = 1.9f + n * 1.0f + n2 * 0.4f;
                _light.color = Color.Lerp(new Color(1f, 0.5f, 0.2f), new Color(1f, 0.68f, 0.3f), n);
            }
            if (_flames != null) { var em = _flames.emission; em.rateOverTime = 18f + n * 10f; }
        }
    }
}
