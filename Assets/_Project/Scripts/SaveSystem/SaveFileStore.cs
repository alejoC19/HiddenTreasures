using System;
using System.IO;

namespace HuesoOxido.SaveSystem
{
    /// <summary>Lectura/escritura de texto con escritura atomica y copia de respaldo (.bak). Sin dependencias del motor.</summary>
    public sealed class SaveFileStore
    {
        readonly string _path;
        public SaveFileStore(string path) { _path = path; }

        public string Path => _path;
        public bool Exists => File.Exists(_path);

        public void Write(string text)
        {
            string dir = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string tmp = _path + ".tmp";
            File.WriteAllText(tmp, text);
            if (File.Exists(_path)) File.Copy(_path, _path + ".bak", true);
            if (File.Exists(_path)) File.Delete(_path);
            File.Move(tmp, _path);
        }

        /// <summary>Lee el archivo; si esta corrupto/ausente intenta con el respaldo. Devuelve null si no hay nada.</summary>
        public string Read()
        {
            if (File.Exists(_path)) return File.ReadAllText(_path);
            if (File.Exists(_path + ".bak")) return File.ReadAllText(_path + ".bak");
            return null;
        }

        public string ReadBackup() => File.Exists(_path + ".bak") ? File.ReadAllText(_path + ".bak") : null;

        public void Delete()
        {
            if (File.Exists(_path)) File.Delete(_path);
            if (File.Exists(_path + ".bak")) File.Delete(_path + ".bak");
            if (File.Exists(_path + ".tmp")) File.Delete(_path + ".tmp");
        }
    }
}
