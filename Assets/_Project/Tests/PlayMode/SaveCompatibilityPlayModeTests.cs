using System.IO;
using HuesoOxido.Core;
using NUnit.Framework;
using UnityEngine;

namespace HuesoOxido.Tests.PlayMode
{
    /// <summary>Guardado anterior + ida y vuelta con el serializador real de Unity (JsonUtility). Usa tools/fixtures/legacy_save_v1.json.</summary>
    public class SaveCompatibilityPlayModeTests
    {
        [Test]
        public void LegacySaveLoads_AndNewSaveRoundTrips()
        {
            string path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "tools", "fixtures", "legacy_save_v1.json"));
            Assert.IsTrue(File.Exists(path), "falta el guardado de prueba: " + path);
            bool ok = SaveCompatibilityCheck.Verify(File.ReadAllText(path), out string report);
            Assert.IsTrue(ok, report);
        }
    }
}
