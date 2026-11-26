using System;
using System.Collections.Generic;

namespace HuesoOxido.Core
{
    /// <summary>Encargo de un cliente: recuperar piezas concretas en buen estado, identificadas y autenticas.</summary>
    public sealed class ContractDefinition
    {
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
            new ContractDefinition { Id = "c_journal", Title = "El diario de Valdés", Client = "Familia Valdés", RegionId = Biomes.ForestId,
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
                if ((d.RegionId == null || d.RegionId == regionId) && Reputation >= d.MinReputation && !_completed.Contains(d.Id) && !IsActive(d.Id)) list.Add(d);
            return list;
        }

        public bool Accept(string id, int currentDay)
        {
            if (_active.Count >= MaxActive || IsActive(id) || _completed.Contains(id)) return false;
            var d = _catalog.Find(x => x.Id == id);
            if (d == null || Reputation < d.MinReputation) return false;
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
                if (a.Done) continue;
                bool typeOk = a.Def.ItemId != null ? a.Def.ItemId == item.DefinitionId : (info != null && info.Rarity >= a.Def.MinRarity);
                if (!typeOk) continue;
                sawType = true;
                if (!state.Identified) return DeliveryResult.NotIdentified;
                if (!state.Authentic) return DeliveryResult.Counterfeit;
                if (item.Condition + 1e-4f < a.Def.MinCondition) { match = match ?? null; continue; }
                match = a; break;
            }
            if (match == null) return sawType ? DeliveryResult.ConditionTooLow : DeliveryResult.NoMatchingContract;
            match.Delivered++;
            if (match.Done)
            {
                _active.Remove(match); _completed.Add(match.Def.Id);
                Reputation += match.Def.ReputationReward;
                Completed?.Invoke(match.Def);
            }
            Changed?.Invoke();
            return DeliveryResult.Accepted;
        }

        /// <summary>Vence los contratos con plazo cumplido: -10 de reputacion cada uno (minimo 0), sin penalidad economica.</summary>
        public int ExpireOverdue(int currentDay)
        {
            int n = _active.RemoveAll(a => currentDay > a.DueDay);
            if (n > 0) { Reputation = Math.Max(0, Reputation - 10 * n); Changed?.Invoke(); }
            return n;
        }

        public (int reputation, List<string> completed, List<(string id, int delivered, int day)> active) Snapshot()
        {
            var act = new List<(string, int, int)>();
            foreach (var a in _active) act.Add((a.Def.Id, a.Delivered, a.AcceptedDay));
            return (Reputation, new List<string>(_completed), act);
        }

        public void Restore(int reputation, IEnumerable<string> completed, IEnumerable<(string id, int delivered, int day)> active)
        {
            Reputation = Math.Max(0, reputation);
            _completed.Clear(); _active.Clear();
            if (completed != null) foreach (var c in completed) _completed.Add(c);
            if (active != null)
                foreach (var a in active) { var d = _catalog.Find(x => x.Id == a.id); if (d != null) _active.Add(new ActiveContract { Def = d, Delivered = a.delivered, AcceptedDay = a.day }); }
            Changed?.Invoke();
        }
    }
}
