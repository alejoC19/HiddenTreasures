using System;
using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Separa una malla en piezas segun una funcion de clasificacion por triangulo (p. ej. las 4 ruedas de un modelo
    /// que viene fusionado). Cada pieza se compacta (solo los vertices que usa) y se transforma con el mismo
    /// <c>toLocal</c>; el resultado de cada pieza puede recentrarse con <c>pieceOrigin</c>. Requiere malla legible.
    /// </summary>
    public static class MeshSplitter
    {
        /// <param name="classify">Devuelve -1 para la pieza base o 0..n-1 para la pieza extraida (recibe el centroide transformado).</param>
        /// <param name="pieceOrigin">Origen que se resta a los vertices de cada pieza extraida (indice 0..n-1).</param>
        public static Mesh[] Split(Mesh src, Matrix4x4 toLocal, Func<Vector3, int> classify, Vector3[] pieceOrigin, int pieces)
        {
            var verts = src.vertices; var nors = src.normals; var uvs = src.uv;
            var tans = src.tangents;
            bool hasN = nors != null && nors.Length == verts.Length, hasUv = uvs != null && uvs.Length == verts.Length, hasT = tans != null && tans.Length == verts.Length;
            var wp = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++) wp[i] = toLocal.MultiplyPoint3x4(verts[i]);

            int outCount = pieces + 1;                    // [0] = base, [1..n] = piezas
            var tris = new List<int>[outCount][];
            for (int o = 0; o < outCount; o++) { tris[o] = new List<int>[src.subMeshCount]; for (int s = 0; s < src.subMeshCount; s++) tris[o][s] = new List<int>(); }

            for (int s = 0; s < src.subMeshCount; s++)
            {
                var idx = src.GetTriangles(s);
                for (int t = 0; t < idx.Length; t += 3)
                {
                    Vector3 c = (wp[idx[t]] + wp[idx[t + 1]] + wp[idx[t + 2]]) / 3f;
                    int k = classify(c);
                    int o = k < 0 ? 0 : k + 1;
                    tris[o][s].Add(idx[t]); tris[o][s].Add(idx[t + 1]); tris[o][s].Add(idx[t + 2]);
                }
            }

            var result = new Mesh[outCount];
            for (int o = 0; o < outCount; o++)
            {
                var remap = new Dictionary<int, int>();
                var pv = new List<Vector3>(); var pn = new List<Vector3>(); var pu = new List<Vector2>(); var pt = new List<Vector4>();
                var sub = new List<int>[src.subMeshCount];
                Vector3 origin = o == 0 ? Vector3.zero : pieceOrigin[o - 1];
                for (int s = 0; s < src.subMeshCount; s++)
                {
                    sub[s] = new List<int>();
                    foreach (int vi in tris[o][s])
                    {
                        if (!remap.TryGetValue(vi, out int ni))
                        {
                            ni = pv.Count; remap[vi] = ni;
                            pv.Add(wp[vi] - origin);
                            if (hasN) pn.Add(toLocal.MultiplyVector(nors[vi]).normalized);
                            if (hasUv) pu.Add(uvs[vi]);
                            if (hasT) { var tg = tans[vi]; var d = toLocal.MultiplyVector(new Vector3(tg.x, tg.y, tg.z)).normalized; pt.Add(new Vector4(d.x, d.y, d.z, tg.w)); }
                        }
                        sub[s].Add(ni);
                    }
                }
                var m = new Mesh { name = src.name + (o == 0 ? "_body" : "_wheel" + (o - 1)) };
                if (pv.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(pv);
                if (hasN) m.SetNormals(pn);
                if (hasUv) m.SetUVs(0, pu);
                if (hasT) m.SetTangents(pt);
                m.subMeshCount = src.subMeshCount;
                for (int s = 0; s < src.subMeshCount; s++) m.SetTriangles(sub[s], s, false);
                m.RecalculateBounds();
                result[o] = m;
            }
            return result;
        }
    }
}
