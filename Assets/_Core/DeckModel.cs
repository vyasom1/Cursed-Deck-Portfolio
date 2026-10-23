using System;
using System.Collections.Generic;

namespace CursedDeck.Core
{
    /// <summary>Index zero is drawn first. Owns a defensive copy; exhausted draws do not mutate.</summary>
    public sealed class DeckModel
    {
        private readonly CardModel[] cards;
        private int next;
        public int Remaining => cards.Length - next;

        public DeckModel() : this(CreateStandardCards()) { }

        public DeckModel(IReadOnlyList<CardModel> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            cards = new CardModel[source.Count];
            var seen = new HashSet<CardModel>();
            for (int i = 0; i < cards.Length; i++)
            {
                if (!source[i].IsValid || !seen.Add(source[i]))
                    throw new ArgumentException("Cards must be valid and unique.", nameof(source));
                cards[i] = source[i];
            }
        }

        public static CardModel[] CreateStandardCards()
        {
            var result = new CardModel[52];
            int index = 0;
            for (int suit = 0; suit < 4; suit++)
                for (int rank = 1; rank <= 13; rank++)
                    result[index++] = new CardModel((Rank)rank, (Suit)suit);
            return result;
        }

        public void Shuffle(SeededRNG rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            // Shuffle only the undrawn suffix, preserving consumed cards.
            for (int i = cards.Length - 1; i > next; i--)
            {
                int j = next + rng.NextInt(i - next + 1);
                CardModel temp = cards[i]; cards[i] = cards[j]; cards[j] = temp;
            }
        }

        internal sealed class Memento
        {
            internal CardModel[] Cards;
            internal int Next;
        }
        internal Memento Capture() => new Memento { Cards = (CardModel[])cards.Clone(), Next = next };
        internal void Restore(Memento saved) { Array.Copy(saved.Cards, cards, cards.Length); next = saved.Next; }
        internal DeckModel Fork() { var copy = new DeckModel(cards); copy.next = next; return copy; }

        public bool TryDraw(out CardModel card)
        {
            if (Remaining == 0) { card = default; return false; }
            card = cards[next++];
            return true;
        }
    }
}
