using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Ruinas con mamposteria por hileras de bloques, arco de dovelas, columnas con base y capitel, fustes caidos y antorchas.
    /// Mismo trazado para todos los ecosistemas; cambian la piedra y el tinte (perfil de bioma).
    /// </summary>
    public static class RuinsBuilder
    {
        public static void Build(Transform root, Vector3 c, System.Func<Vector2, Vector3> groundAt, Material stone, Material moss, System.Random rng, System.Func<float> night)
        {
            float Rnd(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            Vector3 G(float dx, float dz) => groundAt(new Vector2(c.x + dx, c.z + dz));

            // Losas de la plaza (el centro queda abierto: ahi esta el hallazgo mas valioso).
            for (int i = 0; i < 18; i++)
            {
                float a = i * Mathf.PI * 2f / 18f + Rnd(-0.12f, 0.12f), r = Rnd(4.4f, 7.4f);
                var g = G(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                Block(root, "Flagstone", stone, g + Vector3.up * 0.05f, new Vector3(Rnd(1.5f, 2.3f), 0.16f, Rnd(1.4f, 2.1f)), Rnd(0f, 360f), false);
            }

            // Muros perimetrales: hileras de bloques que se vuelven mas bajas y rotas hacia los extremos.
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < 6; i++)
                {
                    if (rng.NextDouble() < 0.22) continue;
                    float t = (i - 2.5f) * 2.6f;
                    Vector3 off; float yaw;
                    switch (side)
                    {
                        case 0: off = new Vector3(t, 0f, 8f); yaw = 0f; break;
                        case 1: off = new Vector3(t, 0f, -8f); yaw = 0f; break;
                        case 2: off = new Vector3(8f, 0f, t); yaw = 90f; break;
                        default: off = new Vector3(-8f, 0f, t); yaw = 90f; break;
                    }
                    if (side == 1 && Mathf.Abs(t) < 3.4f) continue; // entrada
                    int courses = Mathf.Clamp(Mathf.RoundToInt(Rnd(1.5f, 7.5f) * (1f - Mathf.Abs(t) / 14f)), 1, 7);
                    Wall(root, stone, moss, G(off.x, off.z), yaw + Rnd(-3f, 3f), courses, rng);
                }
            }

            // Arco de entrada: dos pilares de hileras + semicirculo de dovelas.
            Arch(root, stone, G(0f, -8f), rng);

            // Columnas: base, fuste acanalado, capitel; algunas truncadas con tambores caidos.
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f + 0.3f;
                var pos = G(Mathf.Cos(a) * 4.3f, Mathf.Sin(a) * 4.3f);
                bool broken = rng.NextDouble() < 0.5;
                Column(root, stone, pos, broken ? Rnd(1.4f, 2.8f) : Rnd(3.6f, 4.4f), !broken, rng);
                if (broken)
                {
                    var d = new GameObject("FallenDrum");
                    d.transform.SetParent(root, false);
                    d.transform.position = pos + new Vector3(Rnd(-1.8f, 1.8f), 0.45f, Rnd(-1.8f, 1.8f));
                    d.transform.rotation = Quaternion.Euler(0f, Rnd(0f, 360f), 90f);
                    float len = Rnd(0.9f, 1.8f);
                    d.AddComponent<MeshFilter>().sharedMesh = MeshKit.Trunk(len, 0.5f, 0.5f, 12, 1, 2.2f, 0f, i + 40);
                    d.AddComponent<MeshRenderer>().sharedMaterial = stone;
                    var cc = d.AddComponent<CapsuleCollider>(); cc.radius = 0.5f; cc.height = len; cc.center = new Vector3(0f, len * 0.5f, 0f);
                    d.isStatic = true;
                }
            }

            // Bloques caidos y escombro suelto.
            for (int i = 0; i < 26; i++)
            {
                float a = Rnd(0f, Mathf.PI * 2f), r = Rnd(5f, 14f);
                var g = G(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
                var b = Block(root, "Fallen", i % 4 == 0 ? moss : stone, g + Vector3.up * 0.3f, new Vector3(Rnd(0.7f, 1.7f), Rnd(0.45f, 0.9f), Rnd(0.7f, 1.4f)), Rnd(0f, 360f), true);
                b.transform.rotation *= Quaternion.Euler(Rnd(-9f, 9f), 0f, Rnd(-9f, 9f));
            }

            // Antorchas en las esquinas de la plaza.
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * 0.25f + i * Mathf.PI * 0.5f;
                Torch(root, G(Mathf.Cos(a) * 7.4f, Mathf.Sin(a) * 7.4f), night);
            }
        }

        static GameObject Block(Transform parent, string name, Material mat, Vector3 pos, Vector3 scale, float yaw, bool collider)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.AddComponent<MeshFilter>().sharedMesh = MeshKit.Box(scale, 2.2f);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<BoxCollider>().size = scale;
            go.isStatic = true;
            return go;
        }

        /// <summary>Muro de mamposteria: hileras de bloques con juntas desfasadas; las hileras altas pierden bloques.</summary>
        static void Wall(Transform root, Material stone, Material moss, Vector3 baseP, float yaw, int courses, System.Random rng)
        {
            float Rnd(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var holder = new GameObject("Wall").transform;
            holder.SetParent(root, false);
            holder.SetPositionAndRotation(baseP, Quaternion.Euler(0f, yaw, 0f));
            const float ch = 0.46f, depth = 0.85f, len = 2.6f;
            for (int k = 0; k < courses; k++)
            {
                float x = -len * 0.5f + (k % 2 == 0 ? 0f : 0.45f);
                while (x < len * 0.5f - 0.2f)
                {
                    float bl = Rnd(0.7f, 1.2f);
                    bl = Mathf.Min(bl, len * 0.5f - x);
                    bool missing = k >= 2 && rng.NextDouble() < 0.18 * k * 0.5f;
                    if (!missing && bl > 0.25f)
                    {
                        var mat = rng.NextDouble() < 0.12 ? moss : stone;
                        var b = Block(holder, "B", mat, Vector3.zero, new Vector3(bl - 0.03f, ch - 0.02f, depth + Rnd(-0.06f, 0.06f)), 0f, false);
                        b.transform.localPosition = new Vector3(x + bl * 0.5f, ch * 0.5f + k * ch - 0.1f, Rnd(-0.05f, 0.05f));
                        b.transform.localRotation = Quaternion.Euler(Rnd(-1.5f, 1.5f), Rnd(-2.5f, 2.5f), Rnd(-1.5f, 1.5f));
                    }
                    x += bl;
                }
            }
            var col = holder.gameObject.AddComponent<BoxCollider>();
            float h = courses * ch;
            col.center = new Vector3(0f, h * 0.5f - 0.1f, 0f); col.size = new Vector3(len, h, depth);
            holder.gameObject.isStatic = true;
        }

        static void Column(Transform root, Material stone, Vector3 pos, float height, bool capital, System.Random rng)
        {
            var go = new GameObject("Column");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var baseB = Block(go.transform, "Base", stone, Vector3.zero, new Vector3(1.4f, 0.4f, 1.4f), 0f, false);
            baseB.transform.localPosition = new Vector3(0f, 0.2f - 0.05f, 0f);
            var shaft = new GameObject("Shaft"); shaft.transform.SetParent(go.transform, false);
            shaft.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            shaft.AddComponent<MeshFilter>().sharedMesh = MeshKit.Trunk(height, 0.58f, 0.46f, 16, 3, 2.2f, 0f, rng.Next(1000));
            shaft.AddComponent<MeshRenderer>().sharedMaterial = stone;
            if (capital)
            {
                var cap = Block(go.transform, "Capital", stone, Vector3.zero, new Vector3(1.3f, 0.35f, 1.3f), 0f, false);
                cap.transform.localPosition = new Vector3(0f, 0.35f + height + 0.15f, 0f);
                var abacus = Block(go.transform, "Abacus", stone, Vector3.zero, new Vector3(1.5f, 0.18f, 1.5f), 0f, false);
                abacus.transform.localPosition = new Vector3(0f, 0.35f + height + 0.42f, 0f);
            }
            var cc = go.AddComponent<CapsuleCollider>();
            cc.radius = 0.6f; cc.height = height + 0.4f; cc.center = new Vector3(0f, (height + 0.4f) * 0.5f, 0f);
            go.isStatic = true;
        }

        static void Arch(Transform root, Material stone, Vector3 center, System.Random rng)
        {
            float Rnd(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var holder = new GameObject("Arch").transform;
            holder.SetParent(root, false);
            holder.position = center;
            const float span = 2.3f;   // radio interior
            // Pilares de hileras.
            foreach (float s in new[] { -1f, 1f })
                for (int k = 0; k < 7; k++)
                {
                    var b = Block(holder, "Pier", stone, Vector3.zero, new Vector3(1.0f, 0.5f, 1.1f), 0f, false);
                    b.transform.localPosition = new Vector3(s * (span + 0.5f), 0.25f + k * 0.5f - 0.1f, 0f);
                    b.transform.localRotation = Quaternion.Euler(0f, Rnd(-2f, 2f), 0f);
                }
            // Dovelas: semicirculo de bloques orientados radialmente.
            const int n = 11;
            float springY = 7 * 0.5f - 0.1f;
            for (int i = 0; i < n; i++)
            {
                if (i == n - 1 && rng.NextDouble() < 0.5) continue;   // una dovela perdida
                float a = Mathf.PI * (i + 0.5f) / n;
                var v = Block(holder, "Voussoir", stone, Vector3.zero, new Vector3(0.7f, 0.7f, 1.1f), 0f, false);
                v.transform.localPosition = new Vector3(-Mathf.Cos(a) * (span + 0.4f), springY + Mathf.Sin(a) * (span + 0.4f), 0f);
                v.transform.localRotation = Quaternion.Euler(0f, 0f, 90f - a * Mathf.Rad2Deg);
            }
            var cl = new GameObject("ArchColL").AddComponent<BoxCollider>(); cl.transform.SetParent(holder, false);
            cl.transform.localPosition = new Vector3(-(span + 0.5f), 1.75f, 0f); cl.size = new Vector3(1.1f, 3.6f, 1.2f);
            var cr = new GameObject("ArchColR").AddComponent<BoxCollider>(); cr.transform.SetParent(holder, false);
            cr.transform.localPosition = new Vector3(span + 0.5f, 1.75f, 0f); cr.size = new Vector3(1.1f, 3.6f, 1.2f);
            holder.gameObject.isStatic = true;
        }

        static void Torch(Transform root, Vector3 pos, System.Func<float> night)
        {
            var go = new GameObject("Torch");
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            var wood = ArtMaterials.Textured("bark_pine", new Color(0.55f, 0.5f, 0.45f), 0.05f);
            var pole = new GameObject("Pole"); pole.transform.SetParent(go.transform, false);
            pole.AddComponent<MeshFilter>().sharedMesh = MeshKit.Trunk(1.9f, 0.07f, 0.055f, 8, 2, 1f, 0f, 7);
            pole.AddComponent<MeshRenderer>().sharedMaterial = wood;
            var bowl = new GameObject("Bowl"); bowl.transform.SetParent(go.transform, false);
            bowl.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            bowl.AddComponent<MeshFilter>().sharedMesh = MeshKit.Trunk(0.2f, 0.1f, 0.2f, 10, 1, 1f, 0f, 3);
            bowl.AddComponent<MeshRenderer>().sharedMaterial = MaterialUtil.Lit(new Color(0.12f, 0.1f, 0.09f), 0.3f, 0.6f);
            var cc = go.AddComponent<CapsuleCollider>(); cc.radius = 0.12f; cc.height = 2f; cc.center = new Vector3(0f, 1f, 0f);
            var fx = CampfireFx.Create(go.transform, new Vector3(0f, 2.0f, 0f));
            if (fx != null) fx.localScale = Vector3.one * 0.3f;
            var lightGo = new GameObject("TorchLight"); lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point; light.range = 10f; light.intensity = 1.5f; light.shadows = LightShadows.None;
            light.color = new Color(1f, 0.55f, 0.22f);
            go.AddComponent<TorchLight>().Setup(light, night);
        }
    }
}
