using System.Collections.Generic;
using System.Linq;
using HuesoOxido.Core;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public sealed class PlayerStateAndJournalTests
    {
        [Test]
        public void Idle_To_Crouching_And_Back()
        {
            var m = new PlayerStateMachine();
            m.Tick(0.1f, false, false);
            Assert.AreEqual(PlayerAction.Idle, m.State);
            m.Tick(0.1f, true, false);
            Assert.AreEqual(PlayerAction.Crouching, m.State);
            m.Tick(0.1f, false, false);
            Assert.AreEqual(PlayerAction.Idle, m.State);
        }

        [Test]
        public void Dig_Find_PickUp_Clean_Idle_Sequence()
        {
            var m = new PlayerStateMachine();
            var log = new List<PlayerAction>();
            m.Changed += (_, n) => log.Add(n);

            m.Tick(0.1f, false, true);
            Assert.AreEqual(PlayerAction.Digging, m.State);
            Assert.IsFalse(m.LocksMovement, "mientras cava todavia puede cancelar");

            m.NotifyFind();
            Assert.AreEqual(PlayerAction.PickingUp, m.State);
            Assert.IsTrue(m.LocksMovement);

            m.Tick(PlayerStateMachine.PickUpSeconds + 0.01f, false, false);
            Assert.AreEqual(PlayerAction.Cleaning, m.State);
            m.Tick(PlayerStateMachine.CleanSeconds + 0.01f, false, false);
            Assert.AreEqual(PlayerAction.Idle, m.State);
            CollectionAssert.AreEqual(new[] { PlayerAction.Digging, PlayerAction.PickingUp, PlayerAction.Cleaning, PlayerAction.Idle }, log);
        }

        [Test]
        public void BusyStates_IgnoreInput_AndReturnToCrouchIfCrouched()
        {
            var m = new PlayerStateMachine();
            m.NotifyFind();
            m.Tick(0.2f, true, true);                      // intentar cavar de nuevo no interrumpe
            Assert.AreEqual(PlayerAction.PickingUp, m.State);
            m.NotifyFind();                                 // otra pieza durante la secuencia: se ignora
            Assert.AreEqual(PlayerAction.PickingUp, m.State);
            m.Tick(PlayerStateMachine.PickUpSeconds, true, false);
            m.Tick(PlayerStateMachine.CleanSeconds + 0.01f, true, false);
            Assert.AreEqual(PlayerAction.Crouching, m.State);
        }

        [Test]
        public void Progress_GrowsWithinTimedStates()
        {
            var m = new PlayerStateMachine();
            m.NotifyFind();
            m.Tick(PlayerStateMachine.PickUpSeconds * 0.5f, false, false);
            Assert.AreEqual(0.5f, m.Progress01, 0.01f);
            Assert.AreEqual("PickUp", PlayerAnimatorParams.TriggerFor(PlayerAction.PickingUp));
            Assert.AreEqual("Clean", PlayerAnimatorParams.TriggerFor(PlayerAction.Cleaning));
        }

        [Test]
        public void DiggingCancelled_ReturnsToRest()
        {
            var m = new PlayerStateMachine();
            m.Tick(0.1f, false, true);
            m.Tick(0.1f, false, false);
            Assert.AreEqual(PlayerAction.Idle, m.State);
        }

        sealed class Lookup : IItemLookup
        {
            readonly Dictionary<string, IItemInfo> _d;
            public Lookup() { _d = DefaultItems.Specs().ToDictionary(s => s.Id, s => (IItemInfo)s); }
            public bool TryGet(string id, out IItemInfo info) => _d.TryGetValue(id, out info);
        }

        [Test]
        public void Journal_HidesUndiscovered_AndRevealsExcavated()
        {
            var sites = new List<JournalSite>
            {
                new JournalSite { SiteId = "a", ItemId = "flint_arrowhead", Excavated = true },
                new JournalSite { SiteId = "b", ItemId = "iron_nail", Excavated = false },
                new JournalSite { SiteId = "r0", ItemId = "relic_gold_tunjo", Excavated = false },
                new JournalSite { SiteId = "r1", ItemId = "relic_jade_pendant", Excavated = true },
            };
            var j = FieldJournal.Build(sites, new Lookup());
            Assert.AreEqual(4, j.Total);
            Assert.AreEqual(2, j.TotalFound);
            Assert.AreEqual(1, j.RelicsFound);
            Assert.AreEqual("Punta de flecha de sílex", j.Finds[0].Name);
            Assert.AreEqual(FieldJournal.Hidden, j.Finds[1].Name);
            Assert.AreEqual("", j.Finds[1].Description);
            Assert.AreEqual(FieldJournal.Hidden, j.Relics[0].Name);
            Assert.AreEqual("Colgante de jade tallado", j.Relics[1].Name);
            Assert.AreEqual("Reliquia 2", j.Relics[1].Title);
            Assert.AreEqual("Sitio 02", j.Finds[1].Title);
            Assert.AreEqual(Rarity.Epic, j.Relics[1].Rarity);
        }
    }
}
