using System;
using System.Collections.Generic;

namespace CursedDeck.Core
{
    /// <summary>Owns a single board's mutable state. Queries and actions allocate no collections.</summary>
    public sealed class RuleEngine
    {
        private readonly CardModel[] cards;
        private readonly bool[] removed;
        private readonly DeckModel stock;
        internal IMovePolicy MovePolicy { get; set; }
        public BoardLayout Layout { get; }
        public IDealerStrategy Dealer { get; }
        public int NumberedPlayed { get; private set; }
        public int CardsPlayed { get; private set; }
        public bool IsBlind(int slot)=>Dealer.HidesNext(CardsPlayed) && IsExposed(slot);
        public CardModel Waste { get; private set; }
        public int Chain { get; private set; }
        public int RemainingCards { get; private set; }
        public int StockRemaining => stock.Remaining;
        public bool IsCleared => RemainingCards == 0;
        // A cleared board also has no move; callers distinguish completion with IsCleared.
        public bool HasNoMoves
        {
            get
            {
                for (int i = 0; i < cards.Length; i++) if (CanPlay(i)) return false;
                return true;
            }
        }

        public RuleEngine(BoardLayout layout, IReadOnlyList<CardModel> boardCards,
            CardModel waste, IReadOnlyList<CardModel> stockCards, DealerKind dealer=DealerKind.None)
        {
            Dealer=DealerStrategies.For(dealer);
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            if (boardCards == null) throw new ArgumentNullException(nameof(boardCards));
            if (stockCards == null) throw new ArgumentNullException(nameof(stockCards));
            if (boardCards.Count != layout.Count) throw new ArgumentException("Layout/card count mismatch.");
            var seen = new HashSet<CardModel>();
            if (!waste.IsValid) throw new ArgumentException("Invalid waste.", nameof(waste));
            seen.Add(waste);
            cards = new CardModel[boardCards.Count];
            for (int i = 0; i < cards.Length; i++)
            {
                if (!boardCards[i].IsValid || !seen.Add(boardCards[i])) throw new ArgumentException("Invalid or duplicate board card.");
                cards[i] = boardCards[i];
            }
            for (int i = 0; i < stockCards.Count; i++)
                if (!stockCards[i].IsValid || !seen.Add(stockCards[i])) throw new ArgumentException("Invalid or duplicate stock card.");
            stock = new DeckModel(stockCards);
            removed = new bool[cards.Length];
            Waste = waste;
            RemainingCards = cards.Length;
        }

        internal sealed class Memento
        {
            internal bool[] Removed;
            internal DeckModel.Memento Stock;
            internal CardModel Waste;
            internal int Chain, Remaining, Numbered, Plays;
        }
        internal Memento Capture() => new Memento { Removed = (bool[])removed.Clone(), Stock = stock.Capture(), Waste = Waste, Chain = Chain, Remaining = RemainingCards, Numbered=NumberedPlayed, Plays=CardsPlayed };
        internal void Restore(Memento saved)
        {
            Array.Copy(saved.Removed, removed, removed.Length); stock.Restore(saved.Stock);
            Waste = saved.Waste; Chain = saved.Chain; RemainingCards = saved.Remaining; NumberedPlayed=saved.Numbered; CardsPlayed=saved.Plays;
        }
        private RuleEngine(RuleEngine source)
        {
            Dealer=source.Dealer; NumberedPlayed=source.NumberedPlayed; CardsPlayed=source.CardsPlayed;
            Layout = source.Layout; cards = (CardModel[])source.cards.Clone();
            removed = (bool[])source.removed.Clone(); stock = source.stock.Fork();
            Waste = source.Waste; Chain = source.Chain; RemainingCards = source.RemainingCards;
        }
        internal RuleEngine Fork() => new RuleEngine(this);

        public static bool AreAdjacent(CardModel card, CardModel waste)
        {
            if (!card.IsValid || !waste.IsValid) return false;
            int difference = Math.Abs((int)card.Rank - (int)waste.Rank);
            return difference == 1 || difference == 12;
        }

        public CardModel GetCard(int slot) => cards[slot];
        public bool IsRemoved(int slot) => removed[slot];

        public bool IsExposed(int slot)
        {
            if (slot < 0 || slot >= cards.Length || removed[slot]) return false;
            var covering = Layout[slot].CoveringSlots;
            for (int i = 0; i < covering.Count; i++) if (!removed[covering[i]]) return false;
            return true;
        }

        public bool CanPlay(int slot) => IsExposed(slot) && Dealer.Allows(cards[slot],NumberedPlayed) && (AreAdjacent(cards[slot], Waste) || (MovePolicy != null && MovePolicy.AllowsMove(cards[slot], Waste)));

        public bool TryPlay(int slot)
        {
            if (!CanPlay(slot)) return false;
            Waste = cards[slot];
            removed[slot] = true;
            RemainingCards--;
            Chain++; CardsPlayed++; if((int)Waste.Rank>=2 && (int)Waste.Rank<=10)NumberedPlayed++;
            return true;
        }

        // Voluntary draws are allowed, as in standard TriPeaks. No redeal.
        internal void ShuffleStock(SeededRNG rng) => stock.Shuffle(rng);

        public bool TryDraw()
        {
            if (IsCleared || !stock.TryDraw(out CardModel card)) return false;
            Waste = card;
            Chain = 0;
            return true;
        }
    }
}
