using System;

namespace HuesoOxido.Economy
{
    public sealed class Wallet
    {
        int _money;
        public event Action<int> Changed;

        public Wallet(int startingMoney) { _money = Math.Max(0, startingMoney); }

        public int Money => _money;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            _money += amount;
            Changed?.Invoke(_money);
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > _money) return false;
            _money -= amount;
            Changed?.Invoke(_money);
            return true;
        }

        public void Set(int amount) { _money = Math.Max(0, amount); Changed?.Invoke(_money); }
    }
}
