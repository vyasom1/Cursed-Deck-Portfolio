using System;
using System.Collections.Generic;

namespace CursedDeck.Run
{
    public sealed class RunState
    {
        private readonly List<RelicSpec> relics = new List<RelicSpec>(15);
        internal RelicManager ActiveManager { get; set; }
        public int Floor { get; internal set; } = 1;
        public int Round { get; internal set; } = 1;
        public RunStage Stage { get; internal set; } = RunStage.Playing;
        public ConsumableInventory Consumables { get; } = new ConsumableInventory();
        internal void RestoreLastBreath(bool used) { LastBreathUsed = used; }
        public int Seed { get; }
        public int Gold { get; private set; }
        public bool LastBreathUsed { get; private set; }
        public IReadOnlyList<RelicSpec> Relics { get; }

        public RunState(int seed, int gold = 0)
        {
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            Seed = seed; Gold = gold; Relics = relics.AsReadOnly();
        }

        public bool Owns(string id)
        {
            for (int i = 0; i < relics.Count; i++) if (StringComparer.Ordinal.Equals(relics[i].Id, id)) return true;
            return false;
        }
        public bool Acquire(RelicSpec relic)
        {
            if (relic == null) throw new ArgumentNullException(nameof(relic));
            if (Owns(relic.Id)) return false;
            relics.Add(relic); return true;
        }
        public void AddGold(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            Gold = checked(Gold + amount);
        }
        public bool TrySpendGold(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (Gold < amount) return false;
            Gold -= amount; return true;
        }
        internal bool TryUseLastBreath()
        {
            if (LastBreathUsed) return false;
            LastBreathUsed = true; return true;
        }
        internal readonly struct Memento
        {
            internal readonly int Gold;
            internal readonly bool LastBreathUsed;
            internal readonly RelicManager ActiveManager;
            internal Memento(int gold, bool used, RelicManager active) { Gold = gold; LastBreathUsed = used; ActiveManager = active; }
        }
        internal Memento Capture() => new Memento(Gold, LastBreathUsed, ActiveManager);
        internal void Restore(Memento saved) { Gold = saved.Gold; LastBreathUsed = saved.LastBreathUsed; ActiveManager = saved.ActiveManager; }
    }
}
