using System.IO;
using HuesoOxido.SaveSystem;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class SaveFileStoreTests
    {
        string _dir;

        [SetUp] public void Setup() { _dir = Path.Combine(Path.GetTempPath(), "ht_save_" + System.Guid.NewGuid().ToString("N")); }
        [TearDown] public void Teardown() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

        [Test]
        public void Write_Then_Read_RoundTrips_AndKeepsBackup()
        {
            var s = new SaveFileStore(Path.Combine(_dir, "save.json"));
            Assert.IsFalse(s.Exists);
            Assert.IsNull(s.Read());
            s.Write("uno");
            s.Write("dos");
            Assert.AreEqual("dos", s.Read());
            Assert.AreEqual("uno", s.ReadBackup());
        }

        [Test]
        public void Read_FallsBackToBackupWhenMainIsMissing()
        {
            var s = new SaveFileStore(Path.Combine(_dir, "save.json"));
            s.Write("a");
            s.Write("b");
            File.Delete(Path.Combine(_dir, "save.json"));
            Assert.AreEqual("a", s.Read());
        }

        [Test]
        public void Delete_RemovesEverything()
        {
            var s = new SaveFileStore(Path.Combine(_dir, "save.json"));
            s.Write("a"); s.Write("b");
            s.Delete();
            Assert.IsNull(s.Read());
        }
    }
}
