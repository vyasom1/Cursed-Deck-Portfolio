using System;

namespace CursedDeck.Core
{
    public enum Rank { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }
    public enum Suit { Clubs, Diamonds, Hearts, Spades }
    public enum CardColor { Black, Red }

    public readonly struct CardModel : IEquatable<CardModel>
    {
        public Rank Rank { get; }
        public Suit Suit { get; }
        public CardColor Color => Suit == Suit.Diamonds || Suit == Suit.Hearts ? CardColor.Red : CardColor.Black;
        // default(CardModel) is deliberately invalid; boundaries reject it.
        public bool IsValid => Rank >= Rank.Ace && Rank <= Rank.King && Suit >= Suit.Clubs && Suit <= Suit.Spades;

        public CardModel(Rank rank, Suit suit)
        {
            if (rank < Rank.Ace || rank > Rank.King) throw new ArgumentOutOfRangeException(nameof(rank));
            if (suit < Suit.Clubs || suit > Suit.Spades) throw new ArgumentOutOfRangeException(nameof(suit));
            Rank = rank;
            Suit = suit;
        }

        public bool Equals(CardModel other) => Rank == other.Rank && Suit == other.Suit;
        public override bool Equals(object obj) => obj is CardModel other && Equals(other);
        public override int GetHashCode() => ((int)Rank - 1) * 4 + (int)Suit;
        public static bool operator ==(CardModel left, CardModel right) => left.Equals(right);
        public static bool operator !=(CardModel left, CardModel right) => !left.Equals(right);
        public override string ToString() => Rank + " of " + Suit;
    }
}
