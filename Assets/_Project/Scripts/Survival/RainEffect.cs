using UnityEngine;

namespace HuesoOxido.Survival
{
    /// <summary>Lluvia ligera: particulas alargadas que siguen a la camara cuando el clima es lluvia.</summary>
    public sealed class RainEffect : MonoBehaviour
    {
        ParticleSystem _ps;
        Transform _follow;
        WeatherModel _weather;

        public void Initialize(Transform follow, WeatherModel weather)
        {
            _follow = follow; _weather = weather;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f); // el emisor "Box" lanza hacia +Z: queda apuntando hacia abajo

            _ps = gameObject.AddComponent<ParticleSystem>();
            var main = _ps.main;
            main.loop = true;
            main.startLifetime = 0.9f;
            main.startSpeed = 26f;
            main.startSize = 0.04f;
            main.startColor = new Color(0.8f, 0.88f, 1f, 0.35f);
            main.maxParticles = 2500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = _ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(26f, 26f, 0.1f);

            var emission = _ps.emission;
            emission.rateOverTime = 0f;

            var r = _ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 3f;
            r.velocityScale = 0.04f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var shader = HuesoOxido.Core.MaterialUtil.ParticleShader();
            if (shader != null) r.sharedMaterial = new Material(shader) { color = new Color(0.85f, 0.9f, 1f, 0.4f) };
            _ps.Play();
        }

        void LateUpdate()
        {
            if (_ps == null || _follow == null) return;
            transform.position = _follow.position + Vector3.up * 14f;
            var emission = _ps.emission;
            emission.rateOverTime = _weather.Current == HuesoOxido.Core.WeatherKind.Rain ? 1800f : 0f;
        }
    }
}
