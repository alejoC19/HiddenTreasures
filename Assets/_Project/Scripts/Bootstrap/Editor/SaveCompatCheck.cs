using System.IO;
using UnityEditor;
using UnityEngine;

namespace HuesoOxido.Core.EditorTools
{
    /// <summary>Lote: Unity -executeMethod HuesoOxido.Core.EditorTools.SaveCompatCheck.RunBatch (HT_SAVE_FIXTURE = ruta del guardado anterior). Menu: Hidden Treasures/Comprobar guardados.</summary>
    public static class SaveCompatCheck
    {
        [MenuItem("Hidden Treasures/Comprobar compatibilidad de guardados")]
        static void FromMenu()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "tools", "fixtures", "legacy_save_v1.json");
            bool ok = Run(path);
            EditorUtility.DisplayDialog("Compatibilidad de guardados", ok ? "Todo OK (ver la consola)." : "HAY FALLOS: ver la consola.", "OK");
        }

        public static void RunBatch()
        {
            string path = System.Environment.GetEnvironmentVariable("HT_SAVE_FIXTURE") ?? Path.Combine(Directory.GetCurrentDirectory(), "tools", "fixtures", "legacy_save_v1.json");
            bool ok = Run(path);
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool Run(string path)
        {
            if (!File.Exists(path)) { Debug.LogError("[SAVECOMPAT] no existe " + path); return false; }
            bool ok = SaveCompatibilityCheck.Verify(File.ReadAllText(path), out string report);
            Debug.Log("[SAVECOMPAT] " + (ok ? "OK" : "FALLO") + "\n" + report);
            return ok;
        }
    }
}
