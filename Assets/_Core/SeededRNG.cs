using System;
using System.Collections.Generic;

namespace CursedDeck.Core
{
    /// <summary>Version 1: SplitMix64 with rejection-sampled bounded integers and Fisher-Yates.
    /// Explicit unsigned arithmetic makes seeds stable across Unity/.NET implementations.</summary>
    public sealed class SeededRNG
    {
        private ulong state;
        public SeededRNG(int seed) { state = unchecked((ulong)(long)seed); }

        public uint NextUInt()
        {
            unchecked
            {
                state += 0x9E3779B97F4A7C15UL;
                ulong value = state;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return (uint)((value ^ (value >> 31)) >> 32);
            }
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            uint bound = (uint)exclusiveMax;
            uint threshold = unchecked(0u - bound) % bound;
            uint value;
            do { value = NextUInt(); } while (value < threshold);
            return (int)(value % bound);
        }

        public void Shuffle<T>(IList<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = NextInt(i + 1);
                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }
    }
}
