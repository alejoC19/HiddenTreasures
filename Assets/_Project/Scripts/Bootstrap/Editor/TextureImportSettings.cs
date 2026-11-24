using UnityEditor;
using UnityEngine;

namespace HuesoOxido.Core.EditorTools
{
    /// <summary>
    /// Ajustes de importacion de las texturas de Resources/Art/Textures segun el sufijo del archivo:
    /// *_nor = mapa de normales, *_diff y el resto = color (sRGB). Asi no hay que configurarlas a mano.
    /// </summary>
    public sealed class TextureImportSettings : AssetPostprocessor
    {
        /// <summary>Los modelos de Resources/Art/Models se leen en tiempo de ejecucion (separar ruedas de la malla): deben ser legibles.</summary>
        void OnPreprocessModel()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/Models/")) return;
            var importer = assetImporter as ModelImporter;
            if (importer == null) return;
            importer.isReadable = true;
            importer.importCameras = false;
            importer.importLights = false;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool normal = name.EndsWith("_nor") || name.Contains("_nor_");
            bool linear = normal || name.Contains("_rough") || name.Contains("_arm");
            ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            ti.sRGBTexture = !linear;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 8;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Trilinear;
            ti.maxTextureSize = 2048;
            ti.isReadable = false;
            ti.textureCompression = TextureImporterCompression.Compressed;
        }
    }
}
