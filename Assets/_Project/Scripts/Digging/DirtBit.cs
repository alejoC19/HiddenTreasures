using UnityEngine;
using HuesoOxido.Core;

namespace HuesoOxido.Digging
{
    /// <summary>Trozo de tierra con movimiento balistico simple (sin fisica). Feedback visual de cada golpe de pala.</summary>
    public sealed class DirtBit : MonoBehaviour
    {
        Vector3 _vel;
        float _life;

        public static void Burst(Vector3 origin, int count, Material mat)
        {
            for (int i = 0; i < count; i++)
            {
                var go = MaterialUtil.Primitive(PrimitiveType.Cube, "DirtBit", null, mat);
                float s = Random.Range(0.04f, 0.09f);
                go.transform.localScale = new Vector3(s, s, s);
                go.transform.position = origin + new Vector3(Random.Range(-0.2f, 0.2f), 0.05f, Random.Range(-0.2f, 0.2f));
                var bit = go.AddComponent<DirtBit>();
                bit._vel = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(2.2f, 3.6f), Random.Range(-1.2f, 1.2f));
                bit._life = Random.Range(0.6f, 1.0f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _vel += Vector3.down * 9.8f * dt;
            transform.position += _vel * dt;
            transform.Rotate(_vel * 90f * dt);
            _life -= dt;
            if (_life <= 0f) Destroy(gameObject);
        }
    }
}
