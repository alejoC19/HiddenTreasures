using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.Core
{
    /// <summary>Texturas PBR del juego (CC0, ver CREDITS_ASSETS.md) cargadas desde Resources. Si falta alguna, devuelve null y el llamador usa un respaldo.</summary>
    public static class TextureLibrary
    {
        static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string name)
        {
            if (Cache.TryGetValue(name, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>("Art/Textures/" + name);
            if (t == null) Debug.LogWarning("[Arte] Falta la textura 'Art/Textures/" + name + "'. Se usa un respaldo.");
            Cache[name] = t;
            return t;
        }
    }
}
