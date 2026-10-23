using System;
using System.Collections.Generic;

namespace CursedDeck.Core
{
    /// <summary>Immutable logical geometry. X is measured in half-card widths; Y is a row.
    /// Covering slots must all be removed before this slot is exposed.</summary>
    public sealed class BoardSlot
    {
        private readonly int[] covering;
        public int X { get; }
        public int Y { get; }
        public IReadOnlyList<int> CoveringSlots { get; }

        public BoardSlot(int x, int y, params int[] coveringSlots)
        {
            if (coveringSlots == null) throw new ArgumentNullException(nameof(coveringSlots));
            X = x; Y = y;
            covering = (int[])coveringSlots.Clone();
            CoveringSlots = Array.AsReadOnly(covering);
        }
    }

    public sealed class BoardLayout
    {
        private readonly BoardSlot[] slots;
        public int Count => slots.Length;
        public BoardSlot this[int index] => slots[index];
        public static BoardLayout TriPeaks { get; } = CreateTriPeaks();
        public static BoardLayout TwoPeaks { get; } = CreateTwoPeaks();

        public BoardLayout(IReadOnlyList<BoardSlot> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Count == 0 || source.Count > 51)
                throw new ArgumentException("A layout needs 1..51 slots and leaves a starting waste card.", nameof(source));
            slots = new BoardSlot[source.Count];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = source[i] ?? throw new ArgumentException("Null slot.", nameof(source));
            for (int i = 0; i < slots.Length; i++)
            {
                var seen = new HashSet<int>();
                foreach (int covering in slots[i].CoveringSlots)
                    if (covering < 0 || covering >= Count || covering == i || !seen.Add(covering))
                        throw new ArgumentException("Invalid covering slot.", nameof(source));
            }
            var visited = new byte[Count];
            for (int i = 0; i < Count; i++) Visit(i, visited);
        }

        private void Visit(int index, byte[] visited)
        {
            if (visited[index] == 1) throw new ArgumentException("Layout contains a covering cycle.");
            if (visited[index] == 2) return;
            visited[index] = 1;
            foreach (int covering in slots[index].CoveringSlots) Visit(covering, visited);
            visited[index] = 2;
        }

        private static BoardLayout CreateTwoPeaks()
        {
            var result=new BoardSlot[19];
            for(int peak=0;peak<2;peak++)
            {
                result[peak]=new BoardSlot(2+6*peak,0,2+2*peak,3+2*peak);
                for(int j=0;j<2;j++) result[2+2*peak+j]=new BoardSlot(1+6*peak+2*j,1,6+3*peak+j,7+3*peak+j);
            }
            for(int i=0;i<6;i++)result[6+i]=new BoardSlot(2*i,2,12+i,13+i);
            for(int i=0;i<7;i++)result[12+i]=new BoardSlot(-1+2*i,3);
            return new BoardLayout(result);
        }

        private static BoardLayout CreateTriPeaks()
        {
            var result = new BoardSlot[28];
            for (int peak = 0; peak < 3; peak++)
            {
                result[peak] = new BoardSlot(2 + 6 * peak, 0, 3 + 2 * peak, 4 + 2 * peak);
                for (int j = 0; j < 2; j++)
                    result[3 + 2 * peak + j] = new BoardSlot(1 + 6 * peak + 2 * j, 1,
                        9 + 3 * peak + j, 10 + 3 * peak + j);
            }
            for (int i = 0; i < 9; i++) result[9 + i] = new BoardSlot(2 * i, 2, 18 + i, 19 + i);
            for (int i = 0; i < 10; i++) result[18 + i] = new BoardSlot(-1 + 2 * i, 3);
            return new BoardLayout(result);
        }
    }
}
