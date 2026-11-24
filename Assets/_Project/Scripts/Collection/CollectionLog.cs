using System;
using System.Collections.Generic;

namespace HuesoOxido.Collection
{
    public readonly struct FindResult
    {
        public readonly bool IsNewDiscovery;
        public readonly int TimesFound;
        public FindResult(bool isNew, int times) { IsNewDiscovery = isNew; TimesFound = times; }
        public bool IsDuplicate => !IsNewDiscovery;
    }

    [Serializable]
    public sealed class CollectionEntry
    {
        public string Id;
        public int TimesFound;
        public bool Displayed;
    }

    /// <summary>Catalogo de piezas descubiertas, duplicados y piezas exhibidas en el museo.</summary>
    public sealed class CollectionLog
    {
        readonly Dictionary<string, CollectionEntry> _entries = new Dictionary<string, CollectionEntry>();
        public event Action Changed;

        public int DiscoveredCount => _entries.Count;
        public int DisplayedCount
        {
            get { int n = 0; foreach (var e in _entries.Values) if (e.Displayed) n++; return n; }
        }
        public IEnumerable<CollectionEntry> Entries => _entries.Values;

        public FindResult RegisterFind(string id)
        {
            bool isNew = !_entries.TryGetValue(id, out var e);
            if (isNew) { e = new CollectionEntry { Id = id }; _entries[id] = e; }
            e.TimesFound++;
            Changed?.Invoke();
            return new FindResult(isNew, e.TimesFound);
        }

        public int TimesFound(string id) => _entries.TryGetValue(id, out var e) ? e.TimesFound : 0;
        public bool IsDisplayed(string id) => _entries.TryGetValue(id, out var e) && e.Displayed;

        /// <summary>Marca la pieza como exhibida. Solo se exhibe una por tipo (las demas son duplicados).</summary>
        public bool TryDisplay(string id)
        {
            if (!_entries.TryGetValue(id, out var e) || e.Displayed) return false;
            e.Displayed = true;
            Changed?.Invoke();
            return true;
        }

        public void Restore(IEnumerable<CollectionEntry> entries)
        {
            _entries.Clear();
            if (entries != null) foreach (var e in entries) _entries[e.Id] = e;
            Changed?.Invoke();
        }
    }
}
