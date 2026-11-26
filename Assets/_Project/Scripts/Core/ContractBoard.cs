using System;
using System.Collections.Generic;

namespace HuesoOxido.Core
{
    /// <summary>
    /// Tipos de objetivo: Recover (piezas de un tipo), Collection (un conjunto de piezas distintas), Document (fotografiar un sitio: exige camara),
    /// Survey (llegar a un lugar sin perderse: exige brujula o GPS). Cada tipo empuja a usar equipo y habitos distintos.
    /// </summary>
    public enum ContractKind { Recover, Collection, Document, Survey }

    /// <summary>Encargo de un cliente: recuperar piezas concretas en buen estado, identificadas y autenticas, o documentar/reconocer un lugar.</summary>
    public sealed class ContractDefinition
    {
        public ContractKind Kind = ContractKind.Recover;
        public string[] ItemIds;                 // Collection: piezas distintas que se piden
        public string SiteId;                    // Document / Survey: lugar objetivo
        public string PrerequisiteId;            // cadena: se ofrece solo despues de completar este contrato
        public string Id, Title, Client, Story, RegionId;
        public string ItemId;                    // null = cualquier pieza que cumpla MinRarity
        public Rarity MinRarity = Rarity.Common;
        public int Count = 1;
        public float MinCondition = 0.4f;
        public int RewardMoney, ReputationReward, MinReputation;
        public int DeadlineDays;                 // 0 = sin plazo
    }

    public enum DeliveryResult { Accepted, NoMatchingContract, NotIdentified, Counterfeit, ConditionTooLow }

    public sealed class ActiveContract
    {
        public ContractDefinition Def;
        public int Delivered;
        public int AcceptedDay;
        public readonly List<string> DeliveredIds = new List<string>();
        public bool Done => Delivered >= Def.Count;
        public int DueDay => Def.DeadlineDays > 0 ? AcceptedDay + Def.DeadlineDays : int.MaxValue;
    }

    public static class DefaultContracts
    {
        public static IReadOnlyList<ContractDefinition> All { get; } = new[]
        {
            new ContractDefinition { Id = "c_arrowhead", Title = "Una punta para el museo", Client = "Museo del valle", RegionId = Biomes.ForestId,
                Story = "El museo quiere una punta de flecha de sílex bien conservada para su vitrina.", ItemId = "flint_arrowhead", Count = 1, MinCondition = 0.5f, RewardMoney = 90, ReputationReward = 10 },
            new ContractDefinition { Id = "c_pottery", Title = "Cerámica de la aldea", Client = "Dra. Ibarra, arqueóloga", RegionId = Biomes.ForestId,
                Story = "Necesita fragmentos para reconstruir una vasija de la aldea junto al río.", ItemId = "pottery_shard", Count = 3, MinCondition = 0.3f, RewardMoney = 150, ReputationReward = 15 },
            new ContractDefinition { Id = "c_denarii", Title = "Monedas del camino", Client = "Coleccionista Barrios", RegionId = Biomes.ForestId,
                Story = "Busca denarios de la antigua ruta comercial. Solo auténticos.", ItemId = "roman_denarius", Count = 2, MinCondition = 0.5f, RewardMoney = 320, ReputationReward = 25 },
            new ContractDefinition { Id = "c_axe", Title = "El hacha de bronce", Client = "Fundación Hallstatt", RegionId = Biomes.ForestId,
                Story = "Una pieza de fundición antigua para un estudio de metalurgia.", ItemId = "bronze_axe", Count = 1, MinCondition = 0.6f, RewardMoney = 480, ReputationReward = 40, MinReputation = 100 },
            new ContractDefinition { Id = "c_survey_ruins", Title = "Reconocer las ruinas", Client = "Familia Valdés", RegionId = Biomes.ForestId, Kind = ContractKind.Survey, SiteId = "ruins",
                Story = "Hace décadas una expedición desapareció cerca de unas ruinas. La familia quiere saber si todavía se puede llegar. Te hará falta orientarte.", Count = 1, RewardMoney = 70, ReputationReward = 15 },
            new ContractDefinition { Id = "c_doc_arch", Title = "Documentar el arco", Client = "Dra. Ibarra, arqueóloga", RegionId = Biomes.ForestId, Kind = ContractKind.Document, SiteId = "ruins_arch",
                Story = "Necesita fotografías del arco de piedra antes de que el bosque lo cubra. Hay que llevar una cámara.", Count = 1, RewardMoney = 220, ReputationReward = 20 },
            new ContractDefinition { Id = "c_village", Title = "Un día en la aldea", Client = "Museo del valle", RegionId = Biomes.ForestId, Kind = ContractKind.Collection,
                ItemIds = new[] { "pottery_shard", "iron_nail", "flint_arrowhead" }, Count = 3, MinCondition = 0.4f,
                Story = "Para la sala nueva: una pieza de cada oficio de la aldea, limpias y comprobadas.", RewardMoney = 300, ReputationReward = 30, MinReputation = 100 },
            new ContractDefinition { Id = "c_journal", Title = "El diario de Valdés", Client = "Familia Valdés", RegionId = Biomes.ForestId, PrerequisiteId = "c_survey_ruins",
                Story = "Llevan décadas buscando las últimas páginas de la expedición. Devolverlo cierra una historia.", ItemId = "expedition_journal", Count = 1, MinCondition = 0.4f, RewardMoney = 600, ReputationReward = 60, MinReputation = 100 },
            new ContractDefinition { Id = "c_relic", Title = "Una reliquia para el coleccionista", Client = "Casa Montenegro", RegionId = Biomes.ForestId,
                Story = "Pagará bien por cualquier pieza de rareza épica o superior, con certificado de autenticidad.", ItemId = null, MinRarity = Rarity.Epic, Count = 1, MinCondition = 0.6f, RewardMoney = 900, ReputationReward = 80, MinReputation = 300 },
        };
    }

    /// <summary>
    /// Tablon de contratos y reputacion. Logica pura. El jugador acepta hasta 3 encargos; entrega piezas ya limpias e
    /// identificadas (por eso este sistema depende de FindWorkbook); si vence un plazo pierde un poco de reputacion, nunca dinero.
    /// </summary>
    public sealed class ContractBoard
    {
        public const int MaxActive = 3;
        public static readonly int[] LevelThresholds = { 0, 100, 300, 700 };
        public static readonly string[] LevelNames = { "Aprendiz", "Explorador", "Veterano", "Leyenda" };

        readonly List<ContractDefinition> _catalog;
        readonly List<ActiveContract> _active = new List<ActiveContract>();
        readonly HashSet<string> _completed = new HashSet<string>();

        public event Action Changed;
        public event Action<ContractDefinition> Completed;
        public int Reputation { get; private set; }

        public ContractBoard(IEnumerable<ContractDefinition> catalog) { _catalog = new List<ContractDefinition>(catalog); }

        public IReadOnlyList<ActiveContract> Active => _active;
        public bool IsCompleted(string id) => _completed.Contains(id);
        public bool IsActive(string id) => _active.Exists(a => a.Def.Id == id);

        public int Level { get { int l = 0; for (int i = 0; i < LevelThresholds.Length; i++) if (Reputation >= LevelThresholds[i]) l = i; return l; } }
        public string LevelName => LevelNames[Level];

        /// <summary>Contratos que el jugador puede aceptar ahora (region, reputacion suficiente, no hechos ni en curso).</summary>
        public List<ContractDefinition> Offers(string regionId)
        {
            var list = new List<ContractDefinition>();
            foreach (var d in _catalog)
                if ((d.RegionId == null || d.RegionId == regionId) && Reputation >= d.MinReputation && !_completed.Contains(d.Id) && !IsActive(d.Id)
                    && (d.PrerequisiteId == null || _completed.Contains(d.PrerequisiteId))) list.Add(d);
            return list;
        }

        public bool Accept(string id, int currentDay)
        {
            if (_active.Count >= MaxActive || IsActive(id) || _completed.Contains(id)) return false;
            var d = _catalog.Find(x => x.Id == id);
            if (d == null || Reputation < d.MinReputation || (d.PrerequisiteId != null && !_completed.Contains(d.PrerequisiteId))) return false;
            _active.Add(new ActiveContract { Def = d, AcceptedDay = currentDay });
            Changed?.Invoke();
            return true;
        }

        public bool Abandon(string id)
        {
            int n = _active.RemoveAll(a => a.Def.Id == id);
            if (n > 0) Changed?.Invoke();
            return n > 0;
        }

        /// <summary>Entrega una pieza. No consume la pieza: el llamador la quita del inventario si el resultado es Accepted.</summary>
        public DeliveryResult Deliver(ItemInstance item, IItemInfo info, FindWorkbook workbook)
        {
            var state = workbook.State(item);
            ActiveContract match = null; bool sawType = false;
            foreach (var a in _active)
            {
                if (a.Done || a.Def.Kind == ContractKind.Document || a.Def.Kind == ContractKind.Survey) continue;
                bool typeOk = a.Def.Kind == ContractKind.Collection
                    ? (Array.IndexOf(a.Def.ItemIds ?? new string[0], item.DefinitionId) >= 0 && !a.DeliveredIds.Contains(item.DefinitionId))
                    : (a.Def.ItemId != null ? a.Def.ItemId == item.DefinitionId : (info != null && info.Rarity >= a.Def.MinRarity));
                if (!typeOk) continue;
                sawType = true;
                if (!state.Identified) return DeliveryResult.NotIdentified;
                if (!state.Authentic) return DeliveryResult.Counterfeit;
                if (item.Condition + 1e-4f < a.Def.MinCondition) { match = match ?? null; continue; }
                match = a; break;
            }
            if (match == null) return sawType ? DeliveryResult.ConditionTooLow : DeliveryResult.NoMatchingContract;
            match.Delivered++;
            match.DeliveredIds.Add(item.DefinitionId);
            Finish(match);
            Changed?.Invoke();
            return DeliveryResult.Accepted;
        }

        void Finish(ActiveContract a)
        {
            if (!a.Done) return;
            _active.Remove(a); _completed.Add(a.Def.Id);
            Reputation += a.Def.ReputationReward;
            Completed?.Invoke(a.Def);
        }

        /// <summary>
        /// El jugador estuvo en un lugar. Document exige haber fotografiado (camara); Survey exige haber llegado orientandose (brujula/GPS).
        /// Devuelve true si avanzo algun contrato.
        /// </summary>
        public bool ReportSite(string siteId, bool documented, bool navigating)
        {
            bool any = false;
            foreach (var a in new List<ActiveContract>(_active))
            {
                if (a.Done || a.Def.SiteId != siteId) continue;
                bool ok = (a.Def.Kind == ContractKind.Document && documented) || (a.Def.Kind == ContractKind.Survey && navigating);
                if (!ok) continue;
                a.Delivered++; any = true; Finish(a);
            }
            if (any) Changed?.Invoke();
            return any;
        }

        /// <summary>Vence los contratos con plazo cumplido: -10 de reputacion cada uno (minimo 0), sin penalidad economica.</summary>
        public int ExpireOverdue(int currentDay)
        {
            int n = _active.RemoveAll(a => currentDay > a.DueDay);
            if (n > 0) { Reputation = Math.Max(0, Reputation - 10 * n); Changed?.Invoke(); }
            return n;
        }

        public (int reputation, List<string> completed, List<(string id, int delivered, int day, string deliveredIds)> active) Snapshot()
        {
            var act = new List<(string, int, int, string)>();
            foreach (var a in _active) act.Add((a.Def.Id, a.Delivered, a.AcceptedDay, string.Join(";", a.DeliveredIds)));
            return (Reputation, new List<string>(_completed), act);
        }

        public void Restore(int reputation, IEnumerable<string> completed, IEnumerable<(string id, int delivered, int day, string deliveredIds)> active)
        {
            Reputation = Math.Max(0, reputation);
            _completed.Clear(); _active.Clear();
            if (completed != null) foreach (var c in completed) _completed.Add(c);
            if (active != null)
                foreach (var a in active)
                {
                    var d = _catalog.Find(x => x.Id == a.id); if (d == null) continue;
                    var ac = new ActiveContract { Def = d, Delivered = a.delivered, AcceptedDay = a.day };
                    if (!string.IsNullOrEmpty(a.deliveredIds)) foreach (var id in a.deliveredIds.Split(';')) if (id.Length > 0) ac.DeliveredIds.Add(id);
                    _active.Add(ac);
                }
            Changed?.Invoke();
        }
    }
}
