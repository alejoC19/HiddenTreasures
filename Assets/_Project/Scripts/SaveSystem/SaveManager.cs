using System;
using System.Collections.Generic;
using UnityEngine;

namespace HuesoOxido.SaveSystem
{
    /// <summary>Un sistema aporta su porcion de la partida a los DTOs y la restaura desde ellos.</summary>
    public interface ISaveParticipant
    {
        void Capture(SaveData data);
        void Restore(SaveData data);
    }

    /// <summary>Unico punto de guardado/carga. Serializa DTOs a JSON en la carpeta persistente del juego.</summary>
    public sealed class SaveManager : MonoBehaviour
    {
        readonly List<ISaveParticipant> _participants = new List<ISaveParticipant>();
        SaveFileStore _store;

        public event Action<string> Message;

        public bool HasSave => _store != null && _store.Exists;
        public string FilePath => _store?.Path;

        void Awake()
        {
            _store = new SaveFileStore(System.IO.Path.Combine(Application.persistentDataPath, "hidden_treasures_save.json"));
        }

        public void Register(ISaveParticipant p) { if (!_participants.Contains(p)) _participants.Add(p); }

        public bool Save()
        {
            try
            {
                var data = new SaveData { SavedAtUtc = DateTime.UtcNow.ToString("o") };
                foreach (var p in _participants) p.Capture(data);
                _store.Write(JsonUtility.ToJson(data, true));
                Message?.Invoke("Partida guardada");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[SaveManager] No se pudo guardar: " + e);
                Message?.Invoke("Error al guardar la partida");
                return false;
            }
        }

        public bool Load()
        {
            SaveData data = null;
            try
            {
                string json = _store.Read();
                if (!string.IsNullOrEmpty(json)) data = SaveMigrator.Migrate(JsonUtility.FromJson<SaveData>(json));
                if (data == null)
                {
                    string bak = _store.ReadBackup();
                    if (!string.IsNullOrEmpty(bak)) data = SaveMigrator.Migrate(JsonUtility.FromJson<SaveData>(bak));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SaveManager] Guardado ilegible: " + e.Message);
            }

            if (data == null) { Message?.Invoke("No hay partida guardada"); return false; }
            foreach (var p in _participants) p.Restore(data);
            Message?.Invoke("Partida cargada");
            return true;
        }

        public void DeleteSave() => _store.Delete();

        /// <summary>Ecosistema del guardado (sin restaurar nada). Vacio si no hay guardado o es de una version vieja (bosque).</summary>
        public string PeekBiomeId()
        {
            try
            {
                string json = _store.Read();
                if (string.IsNullOrEmpty(json)) return null;
                var d = JsonUtility.FromJson<SaveData>(json);
                return d != null ? d.BiomeId : null;
            }
            catch (Exception) { return null; }
        }
    }

    /// <summary>Acciones de sesion que la UI puede pedir sin conocer el guardado ni el mundo.</summary>
    public interface ISessionActions
    {
        bool HasSave { get; }
        void SaveGame();
        void LoadGame();
        void NewGame();
        string CurrentBiomeId { get; }
        void TravelTo(string biomeId);
    }
}
