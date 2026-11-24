using System;

namespace HuesoOxido.Core
{
    /// <summary>Definicion de una mision de recoleccion: reunir <see cref="Target"/> hallazgos que cumplan <see cref="Matches"/>.</summary>
    public sealed class QuestDefinition
    {
        public string Id;
        public string Title;
        public string Description;
        public string CounterLabel;       // "Reliquias" -> "Reliquias: 1/3"
        public int Target;
        public int RewardMoney;
        public Func<string, bool> Matches;
    }

    /// <summary>
    /// Mision actual del jugador. Logica pura (sin motor): se alimenta con ReportFind(idDelObjeto) cuando se desentierra una pieza,
    /// avisa por eventos y se guarda como (id, progreso, completada).
    /// </summary>
    public sealed class QuestManager
    {
        public const string RelicPrefix = "relic_";
        public const string RelicHuntId = "relic_hunt";

        public static QuestDefinition RelicHunt() => new QuestDefinition
        {
            Id = RelicHuntId,
            Title = "Encontrar 3 reliquias prehispánicas",
            Description = "Recorré el bosque con el detector y excavá las señales fuertes.",
            CounterLabel = "Reliquias",
            Target = 3,
            RewardMoney = 400,
            Matches = id => id != null && id.StartsWith(RelicPrefix, StringComparison.Ordinal),
        };

        public QuestDefinition Current { get; private set; }
        public int Progress { get; private set; }
        public bool IsCompleted { get; private set; }

        public event Action Changed;
        public event Action<QuestDefinition> Completed;

        public bool HasQuest => Current != null;

        public void Start(QuestDefinition def)
        {
            Current = def; Progress = 0; IsCompleted = false;
            Changed?.Invoke();
        }

        /// <summary>Informa un hallazgo. Devuelve true si hizo avanzar la mision.</summary>
        public bool ReportFind(string itemId)
        {
            if (Current == null || IsCompleted || Current.Matches == null || !Current.Matches(itemId)) return false;
            Progress = Math.Min(Current.Target, Progress + 1);
            if (Progress >= Current.Target) IsCompleted = true;
            Changed?.Invoke();
            if (IsCompleted) Completed?.Invoke(Current);
            return true;
        }

        /// <summary>Texto corto de progreso para el HUD, p. ej. "Reliquias: 1/3".</summary>
        public string ProgressText => Current == null ? "" : Current.CounterLabel + ": " + Progress + "/" + Current.Target;

        public string TitleText => Current == null ? "" : Current.Title;

        // Guardado: solo id + progreso (la definicion se reconstruye por id).
        public void Restore(string questId, int progress, bool completed)
        {
            if (questId == RelicHuntId) Current = RelicHunt();
            else { Current = null; Progress = 0; IsCompleted = false; Changed?.Invoke(); return; }
            Progress = Math.Max(0, Math.Min(Current.Target, progress));
            IsCompleted = completed || Progress >= Current.Target;
            Changed?.Invoke();
        }
    }
}
