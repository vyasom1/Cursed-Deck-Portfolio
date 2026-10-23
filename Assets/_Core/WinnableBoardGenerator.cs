using System;
using System.Collections.Generic;

namespace CursedDeck.Core
{
    public sealed class GeneratedBoard
    {
        public const int GeneratorVersion = 1;
        public int Seed { get; }
        public DealerKind Dealer { get; }
        public BoardLayout Layout { get; }
        public IReadOnlyList<CardModel> Cards { get; }
        public CardModel InitialWaste { get; }
        public IReadOnlyList<CardModel> Stock { get; }
        /// <summary>Diagnostic witness; keep out of player-facing state.</summary>
        public IReadOnlyList<int> SolutionSlots { get; }

        internal GeneratedBoard(int seed, BoardLayout layout, CardModel[] cards, CardModel waste,
            CardModel[] stock, int[] solution, DealerKind dealer=DealerKind.None)
        {
            Dealer=dealer; Seed = seed; Layout = layout; InitialWaste = waste;
            Cards = Array.AsReadOnly((CardModel[])cards.Clone());
            Stock = Array.AsReadOnly((CardModel[])stock.Clone());
            SolutionSlots = Array.AsReadOnly((int[])solution.Clone());
        }

        public RuleEngine CreateGame() => new RuleEngine(Layout, Cards, InitialWaste, Stock, Dealer);
    }

    public static class WinnableBoardGenerator
    {
        public static GeneratedBoard Generate(int seed) => Generate(seed, BoardLayout.TriPeaks);

        public static GeneratedBoard Generate(int seed, BoardLayout layout) => Generate(seed,layout,DealerKind.None);

        public static GeneratedBoard Generate(int seed, BoardLayout layout, DealerKind dealer)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            int boardCount=layout.Count;
            if(boardCount!=19&&boardCount!=28)throw new ArgumentException("Supported tableau sizes are 19 (two peaks) and 28 (legacy).",nameof(layout));
            DealerStrategies.For(dealer);
            var rng = new SeededRNG(seed);
            var restored = new bool[layout.Count];
            var reverseOrder = new int[layout.Count];
            var candidates = new int[layout.Count];

            // Start with the solved (empty) board. Restore a slot only after all cards
            // it covers are restored, so reversing this order is a legal uncovering path.
            for (int step = 0; step < layout.Count; step++)
            {
                int count = 0;
                for (int slot = 0; slot < layout.Count; slot++)
                    if (!restored[slot] && CanRestore(layout, restored, slot)) candidates[count++] = slot;
                int chosen = candidates[rng.NextInt(count)];
                reverseOrder[step] = chosen;
                restored[chosen] = true;
            }

            // Build the waste history backwards too. Each rank has four physical cards.
            // Search is bounded; the fallback walks the 13-rank cycle (29 cards use <=3/rank).
            var ranks = new int[boardCount+1];
            var used = new int[14];
            ranks[0] = 1 + rng.NextInt(13);
            used[ranks[0]] = 1;
            int budget = 4096;
            if (!BuildRanks(ranks, used, 1, rng, ref budget, dealer==DealerKind.Apprentice))
            {
                int direction = rng.NextInt(2) == 0 ? -1 : 1;
                if(dealer==DealerKind.Apprentice){ranks[0]=Neighbor(6,-((boardCount-1)%13));direction=1;}
                for (int i = 1; i < ranks.Length; i++) ranks[i] = Neighbor(ranks[i - 1], direction);
            }

            var suitOrder = new int[13][];
            var rankUses = new int[14];
            for (int rank = 0; rank < 13; rank++)
            {
                suitOrder[rank] = new[] { 0, 1, 2, 3 };
                rng.Shuffle(suitOrder[rank]);
            }
            var board = new CardModel[boardCount];
            var selected = new HashSet<CardModel>();
            for (int i = 0; i < reverseOrder.Length; i++)
            {
                int rank = ranks[i];
                var card = new CardModel((Rank)rank, (Suit)suitOrder[rank - 1][rankUses[rank]++]);
                board[reverseOrder[i]] = card;
                selected.Add(card);
            }
            int wasteRank = ranks[boardCount];
            var waste = new CardModel((Rank)wasteRank, (Suit)suitOrder[wasteRank - 1][rankUses[wasteRank]++]);
            selected.Add(waste);
            var stock = new List<CardModel>(51-boardCount);
            foreach (CardModel card in DeckModel.CreateStandardCards()) if (!selected.Contains(card)) stock.Add(card);
            rng.Shuffle(stock);
            var solution = new int[boardCount];
            for (int i = 0; i < solution.Length; i++) solution[i] = reverseOrder[boardCount-1-i];
            return new GeneratedBoard(seed, layout, board, waste, stock.ToArray(), solution, dealer);
        }

        private static bool CanRestore(BoardLayout layout, bool[] restored, int slot)
        {
            for (int parent = 0; parent < layout.Count; parent++)
            {
                var covering = layout[parent].CoveringSlots;
                for (int i = 0; i < covering.Count; i++)
                    if (covering[i] == slot && !restored[parent]) return false;
            }
            return true;
        }

        private static int Neighbor(int rank, int direction) => (rank - 1 + direction + 13) % 13 + 1;

        private static bool BuildRanks(int[] ranks, int[] used, int index, SeededRNG rng, ref int budget, bool apprentice=false)
        {
            if (index == ranks.Length) return true;
            if (--budget < 0) return false;
            int firstDirection = rng.NextInt(2) == 0 ? -1 : 1;
            for (int choice = 0; choice < 2; choice++)
            {
                int rank = Neighbor(ranks[index - 1], choice == 0 ? firstDirection : -firstDirection);
                if (used[rank] == 4 || (apprentice && index>=Math.Max(0,ranks.Length-4) && index<=ranks.Length-2 && (rank<2 || rank>10))) continue;
                ranks[index] = rank;
                used[rank]++;
                if (BuildRanks(ranks, used, index + 1, rng, ref budget, apprentice)) return true;
                used[rank]--;
            }
            return false;
        }
    }
}
