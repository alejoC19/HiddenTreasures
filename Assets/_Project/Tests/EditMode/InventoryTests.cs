using System;
using HuesoOxido.Core;
using HuesoOxido.Inventory;
using NUnit.Framework;

namespace HuesoOxido.Tests
{
    public class InventoryTests
    {
        static TestLookup Lookup() => new TestLookup().Add("a", 2f, 10).Add("b", 3f, 20).Add("heavy", 9f, 100);

        [Test]
        public void Add_RespectsWeightLimit()
        {
            var inv = new InventoryModel("mochila", 5f, Lookup());
            Assert.IsTrue(inv.TryAdd(new ItemInstance("1", "a", 1f)));
            Assert.IsTrue(inv.TryAdd(new ItemInstance("2", "b", 1f)));
            Assert.AreEqual(5f, inv.TotalWeight, 1e-4f);
            Assert.IsFalse(inv.TryAdd(new ItemInstance("3", "a", 1f)), "sin peso libre no entra");
            Assert.AreEqual(1f, inv.LoadFraction, 1e-4f);
        }

        [Test]
        public void Add_RejectsDuplicateUid()
        {
            var inv = new InventoryModel("m", 50f, Lookup());
            Assert.IsTrue(inv.TryAdd(new ItemInstance("1", "a", 1f)));
            Assert.IsFalse(inv.TryAdd(new ItemInstance("1", "a", 1f)));
        }

        [Test]
        public void Remove_FreesWeightAndFiresChanged()
        {
            var inv = new InventoryModel("m", 10f, Lookup());
            inv.TryAdd(new ItemInstance("1", "b", 1f));
            int changes = 0;
            inv.Changed += () => changes++;
            Assert.IsTrue(inv.Remove("1", out var it));
            Assert.AreEqual("b", it.DefinitionId);
            Assert.AreEqual(0f, inv.TotalWeight);
            Assert.AreEqual(1, changes);
            Assert.IsFalse(inv.Remove("1", out _));
        }

        [Test]
        public void TransferAll_MovesOnlyWhatFits()
        {
            var backpack = new InventoryModel("mochila", 20f, Lookup());
            var trunk = new InventoryModel("baul", 10f, Lookup());
            backpack.TryAdd(new ItemInstance("1", "heavy", 1f));
            backpack.TryAdd(new ItemInstance("2", "a", 1f));
            backpack.TryAdd(new ItemInstance("3", "b", 1f));
            int moved = backpack.TransferAllTo(trunk);
            Assert.AreEqual(1, moved, "heavy(9) entra; 'a'(2) y 'b'(3) superarian los 10 kg del baul");
            Assert.AreEqual(2, backpack.Items.Count);
            Assert.LessOrEqual(trunk.TotalWeight, 10f + 1e-4f);
            Assert.AreEqual(backpack.Items.Count + trunk.Items.Count, 3);
        }

        [Test]
        public void SetCapacity_LowerThanCurrent_BlocksFurtherAdds()
        {
            var inv = new InventoryModel("m", 10f, Lookup());
            inv.TryAdd(new ItemInstance("1", "heavy", 1f));
            inv.SetCapacity(4f);
            Assert.IsFalse(inv.TryAdd(new ItemInstance("2", "a", 1f)));
            Assert.AreEqual(1, inv.Items.Count);
        }

        [Test]
        public void RemoveRandomFraction_RemovesExpectedCount()
        {
            var inv = new InventoryModel("m", 100f, Lookup());
            for (int i = 0; i < 10; i++) inv.TryAdd(new ItemInstance("u" + i, "a", 1f));
            var removed = inv.RemoveRandomFraction(0.4f, new Random(7));
            Assert.AreEqual(4, removed.Count);
            Assert.AreEqual(6, inv.Items.Count);
        }
    }
}
