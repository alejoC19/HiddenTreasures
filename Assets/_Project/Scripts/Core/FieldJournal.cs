using System;
using System.Collections.Generic;

namespace HuesoOxido.Core
{
    /// <summary>Un sitio con tesoro tal como lo ve la libreta: que hay (id del objeto) y si ya se desenterro.</summary>
    public struct JournalSite
    {
        public string SiteId;
        public string ItemId;
        public bool Excavated;
    }

    /// <summary>Linea de la libreta lista para mostrar. Si no esta descubierta, nombre/rareza/descripcion quedan ocultos ("???").</summary>
    public sealed class JournalEntry
    {
        public int Number;
        public bool IsRelic;
        public bool Discovered;
        public string Title;          // "Sitio 07" / "Reliquia 2"
        public string Name;           // "???" o el nombre real
        public Rarity Rarity;
        public string Description;
    }

    public sealed class JournalSummary
    {
        public List<JournalEntry> Relics = new List<JournalEntry>();
        public List<JournalEntry> Finds = new List<JournalEntry>();
        public int RelicsFound, FindsFound;
        public int TotalFound => RelicsFound + FindsFound;
        public int Total => Relics.Count + Finds.Count;
    }

    /// <summary>Arma la libreta de campo a partir de los sitios del mundo y el catalogo. Logica pura.</summary>
    public static class FieldJournal
    {
        public const string Hidden = "???";

        public static JournalSummary Build(IEnumerable<JournalSite> sites, IItemLookup lookup)
        {
            var sum = new JournalSummary();
            int relicNo = 0, findNo = 0;
            foreach (var s in sites)
            {
                bool relic = s.ItemId != null && s.ItemId.StartsWith(QuestManager.RelicPrefix, StringComparison.Ordinal);
                var e = new JournalEntry { IsRelic = relic, Discovered = s.Excavated, Name = Hidden, Description = "" };
                if (relic) { e.Number = ++relicNo; e.Title = "Reliquia " + e.Number; }
                else { e.Number = ++findNo; e.Title = "Sitio " + e.Number.ToString("00"); }

                if (s.Excavated && lookup != null && lookup.TryGet(s.ItemId, out var info))
                {
                    e.Name = info.DisplayName; e.Rarity = info.Rarity; e.Description = info.Description;
                }
                if (relic) { sum.Relics.Add(e); if (e.Discovered) sum.RelicsFound++; }
                else { sum.Finds.Add(e); if (e.Discovered) sum.FindsFound++; }
            }
            return sum;
        }
    }
}
