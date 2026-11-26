using System;
using System.Collections.Generic;

namespace HuesoOxido.Core
{
    public enum ClueKind { Inscription, ToolMarks, Patina, Material, Wear, Maker }
    public enum ClueTone { Consistent, Suspicious, Neutral }

    public sealed class Clue
    {
        public ClueKind Kind; public ClueTone Tone; public string Text;
    }

    public struct BrushResult
    {
        public float Removed;          // suciedad quitada en este tramo
        public bool Damaged;           // se paso de presion
        public Clue Revealed;          // pista que aparecio al limpiar el sector (o null)
    }

    public sealed class CleaningOutcome
    {
        public float CleanFraction, ConditionLost;
        public List<Clue> Clues = new List<Clue>();
    }

    /// <summary>
    /// Limpieza como actividad con decisiones: la pieza se divide en sectores cubiertos de tierra; el jugador elige donde cepillar y con que
    /// presion. Mas presion limpia mas rapido pero daña si supera el limite seguro (que depende de la herramienta y de lo fragil que sea la pieza).
    /// Cada sector puede esconder una PISTA sobre su autenticidad; limpiar mas sectores da mas informacion para decidir.
    /// Determinista (sin azar oculto): las pistas salen del uid de la pieza. Logica pura.
    /// </summary>
    public sealed class CleaningSession
    {
        public const int Sectors = 6;
        readonly ItemInstance _item;
        readonly float[] _soil = new float[Sectors];
        readonly Clue[] _hidden = new Clue[Sectors];
        readonly bool[] _revealed = new bool[Sectors];
        readonly float _toolQuality, _safeLimit;
        float _damage;

        public event Action<Clue> ClueRevealed;

        public CleaningSession(ItemInstance item, FindProfile profile, bool authentic, float toolQuality)
        {
            _item = item; _toolQuality = Math.Max(0f, Math.Min(1f, toolQuality));
            float fragility = 1f - Math.Max(0f, Math.Min(1f, item.Condition));
            _safeLimit = Math.Max(0.15f, 0.35f + 0.5f * _toolQuality - 0.35f * fragility);
            for (int i = 0; i < Sectors; i++) _soil[i] = Math.Min(1f, profile.InitialGrime + 0.1f * Hash01(item.Uid, i));
            BuildClues(authentic);
        }

        public float SafePressure => _safeLimit;
        public float Damage => _damage;
        public float SoilAt(int sector) => _soil[sector];
        public bool IsRevealed(int sector) => _revealed[sector];

        public float CleanFraction { get { float s = 0f; foreach (var v in _soil) s += v; return 1f - s / Sectors; } }

        /// <summary>Cepilla un sector con una presion (0..1) durante dt segundos.</summary>
        public BrushResult Brush(int sector, float pressure, float dt)
        {
            var r = new BrushResult();
            if (sector < 0 || sector >= Sectors || dt <= 0f || _soil[sector] <= 0f) return r;
            pressure = Math.Max(0f, Math.Min(1f, pressure));
            float removed = Math.Min(_soil[sector], pressure * (0.4f + _toolQuality) * dt * 0.8f);
            _soil[sector] -= removed; r.Removed = removed;
            if (pressure > _safeLimit) { _damage += (pressure - _safeLimit) * dt * 0.5f; r.Damaged = true; }
            if (!_revealed[sector] && _soil[sector] < 0.25f && _hidden[sector] != null)
            {
                _revealed[sector] = true; r.Revealed = _hidden[sector]; ClueRevealed?.Invoke(r.Revealed);
            }
            return r;
        }

        public List<Clue> RevealedClues()
        {
            var l = new List<Clue>();
            for (int i = 0; i < Sectors; i++) if (_revealed[i] && _hidden[i] != null) l.Add(_hidden[i]);
            return l;
        }

        /// <summary>Termina la sesion. El daño acumulado baja la condicion de la pieza (restaurable en el taller).</summary>
        public CleaningOutcome Finish()
        {
            float lost = Math.Min(_item.Condition, _damage * 0.3f);
            _item.Condition -= lost;
            return new CleaningOutcome { CleanFraction = CleanFraction, ConditionLost = lost, Clues = RevealedClues() };
        }

        // ---- pistas ----
        static readonly string[][] Consistent =
        {
            new[] { "Inscripción coherente con la época." }, new[] { "Marcas de herramienta antiguas, irregulares." }, new[] { "Pátina profunda y despareja, como corresponde." },
            new[] { "El material coincide con el de otras piezas del lugar." }, new[] { "Desgaste de uso real en los bordes." }, new[] { "Estilo del taller que se documenta en la zona." },
        };
        static readonly string[][] Suspicious =
        {
            new[] { "Letras demasiado nítidas para su edad." }, new[] { "Marcas de herramienta moderna, paralelas y limpias." }, new[] { "Pátina uniforme, como pintada." },
            new[] { "El material no aparece en la región." }, new[] { "Bordes sin desgaste, casi sin uso." }, new[] { "Estilo mezclado de épocas distintas." },
        };
        static readonly string[][] Neutral =
        {
            new[] { "Hay una inscripción, pero está demasiado gastada para leerla." }, new[] { "Marcas de herramienta que no dicen mucho." }, new[] { "Pátina difícil de interpretar." },
            new[] { "Material común, no decide nada." }, new[] { "Desgaste normal." }, new[] { "Sin rasgos de taller reconocibles." },
        };

        void BuildClues(bool authentic)
        {
            // 4 de los 6 sectores esconden una pista; cual y de que clase sale del uid.
            var order = new List<int> { 0, 1, 2, 3, 4, 5 };
            for (int i = order.Count - 1; i > 0; i--) { int j = (int)(Hash01(_item.Uid, 100 + i) * (i + 1)); int t = order[i]; order[i] = order[j]; order[j] = t; }
            int suspicious = authentic ? (Hash01(_item.Uid, 200) < 0.15f ? 1 : 0)            // falsa alarma ocasional
                                       : (Hash01(_item.Uid, 201) < 0.25f ? 1 : 2);           // las falsificaciones suelen delatarse con 2 pistas
            for (int k = 0; k < 4; k++)
            {
                int s = order[k];
                ClueTone tone = k < suspicious ? ClueTone.Suspicious : (Hash01(_item.Uid, 300 + k) < (authentic ? 0.8f : 0.5f) ? ClueTone.Consistent : ClueTone.Neutral);
                var table = tone == ClueTone.Suspicious ? Suspicious : (tone == ClueTone.Consistent ? Consistent : Neutral);
                int kind = (int)(Hash01(_item.Uid, 400 + k) * 6f) % 6;
                _hidden[s] = new Clue { Kind = (ClueKind)kind, Tone = tone, Text = table[kind][0] };
            }
        }

        static float Hash01(string uid, int salt)
        {
            unchecked
            {
                int h = salt * 73856093;
                foreach (char c in uid ?? "") h = h * 31 + c;
                h ^= h >> 15; h *= 0x2c1b3c6d; h ^= h >> 12; h *= 0x297a2d39; h ^= h >> 15;
                return ((uint)h % 100000u) / 100000f;
            }
        }
    }

    /// <summary>Opinion del explorador a partir de las pistas reunidas: no es certeza; la certeza llega con un laboratorio o una referencia.</summary>
    public struct Appraisal
    {
        public bool BelievesAuthentic;
        public float Confidence;      // 0..0,95

        /// <param name="referenceBonus">0..0,3: ya tener una pieza igual en la coleccion o un documento de referencia suma confianza.</param>
        public static Appraisal From(IReadOnlyList<Clue> clues, float referenceBonus = 0f)
        {
            float score = 0f;
            foreach (var c in clues) score += c.Tone == ClueTone.Consistent ? 1f : (c.Tone == ClueTone.Suspicious ? -1.5f : 0f);
            float conf = Math.Max(0f, Math.Min(0.95f, 0.2f + 0.14f * clues.Count + Math.Max(0f, Math.Min(0.3f, referenceBonus))));
            return new Appraisal { BelievesAuthentic = score >= 0f, Confidence = conf };
        }
    }
}
