using System;

namespace CursedDeck.Run
{
    public sealed class ConsumableInventory
    {
        private readonly int[] counts=new int[3];
        private static int Index(ConsumableKind kind)
        {
            int index=(int)kind;
            if(index<0 || index>2) throw new ArgumentOutOfRangeException(nameof(kind));
            return index;
        }
        public int this[ConsumableKind kind] => counts[Index(kind)];
        public bool CanAdd(ConsumableKind kind) => counts[Index(kind)]<int.MaxValue;
        public void Add(ConsumableKind kind,int quantity=1)
        {
            if(quantity<1) throw new ArgumentOutOfRangeException(nameof(quantity));
            int index=Index(kind); counts[index]=checked(counts[index]+quantity);
        }
        internal bool TryConsume(ConsumableKind kind)
        {
            int index=Index(kind); if(counts[index]==0) return false;
            counts[index]--; return true;
        }
    }
}
