using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CursedDeck.Core.Tests
{
    public sealed class CardAndDeckTests
    {
        [TestCase(Suit.Clubs, CardColor.Black)]
        [TestCase(Suit.Spades, CardColor.Black)]
        [TestCase(Suit.Diamonds, CardColor.Red)]
        [TestCase(Suit.Hearts, CardColor.Red)]
        public void ColorIsDerivedFromSuit(Suit suit, CardColor color)
        {
            for (int rank = 1; rank <= 13; rank++)
            {
                var card = new CardModel((Rank)rank, suit);
                Assert.That(card.IsValid, Is.True);
                Assert.That(card.Rank, Is.EqualTo((Rank)rank));
                Assert.That(card.Suit, Is.EqualTo(suit));
                Assert.That(card.Color, Is.EqualTo(color));
            }
        }

        [TestCase(0)] [TestCase(14)] [TestCase(-1)]
        public void RejectsInvalidRanks(int rank) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardModel((Rank)rank, Suit.Clubs));

        [TestCase(-1)] [TestCase(4)]
        public void RejectsInvalidSuits(int suit) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardModel(Rank.Ace, (Suit)suit));

        [Test]
        public void CardEqualityIncludesRankAndSuit()
        {
            var first = new CardModel(Rank.Ace, Suit.Clubs);
            var same = new CardModel(Rank.Ace, Suit.Clubs);
            Assert.That(first == same, Is.True);
            Assert.That(first.Equals((object)same), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(first != new CardModel(Rank.Ace, Suit.Hearts), Is.True);
            Assert.That(first != new CardModel(Rank.King, Suit.Clubs), Is.True);
            Assert.That(default(CardModel).IsValid, Is.False);
        }

        [Test]
        public void StandardDeckContainsEveryRankSuitPairExactlyOnce()
        {
            var deck = new DeckModel();
            var seen = new HashSet<CardModel>();
            Assert.That(deck.Remaining, Is.EqualTo(52));
            while (deck.TryDraw(out CardModel card)) Assert.That(seen.Add(card), Is.True);
            Assert.That(seen.Count, Is.EqualTo(52));
            for (int suit = 0; suit < 4; suit++)
                for (int rank = 1; rank <= 13; rank++)
                    Assert.That(seen.Contains(new CardModel((Rank)rank, (Suit)suit)), Is.True);
            Assert.That(deck.TryDraw(out _), Is.False);
            Assert.That(deck.Remaining, Is.Zero);
        }

        [Test]
        public void DeckCopiesInputAndDrawsIndexZeroFirst()
        {
            var original = new CardModel(Rank.Ace, Suit.Hearts);
            var source = new[] { original, new CardModel(Rank.Two, Suit.Clubs) };
            var deck = new DeckModel(source);
            source[0] = new CardModel(Rank.King, Suit.Clubs);
            Assert.That(deck.TryDraw(out CardModel card), Is.True);
            Assert.That(card, Is.EqualTo(original));
            Assert.That(deck.Remaining, Is.EqualTo(1));
        }

        [Test]
        public void DeckRejectsInvalidOrDuplicateCards()
        {
            var card = new CardModel(Rank.Ace, Suit.Clubs);
            Assert.Throws<ArgumentNullException>(() => new DeckModel(null));
            Assert.Throws<ArgumentException>(() => new DeckModel(new[] { card, card }));
            Assert.Throws<ArgumentException>(() => new DeckModel(new[] { default(CardModel) }));
        }

        [Test]
        public void ShufflePreservesUndrawnCardsAndNeverReturnsConsumedCard()
        {
            var deck = new DeckModel();
            deck.TryDraw(out CardModel consumed);
            deck.Shuffle(new SeededRNG(42));
            var seen = new HashSet<CardModel>();
            while (deck.TryDraw(out CardModel card))
            {
                Assert.That(card, Is.Not.EqualTo(consumed));
                Assert.That(seen.Add(card), Is.True);
            }
            Assert.That(seen.Count, Is.EqualTo(51));
            deck.Shuffle(new SeededRNG(42));
            Assert.Throws<ArgumentNullException>(() => deck.Shuffle(null));
        }

        [Test]
        public void SameSeedProducesSameDeckOrder()
        {
            var first = new DeckModel(); var second = new DeckModel();
            first.Shuffle(new SeededRNG(-12)); second.Shuffle(new SeededRNG(-12));
            while (first.TryDraw(out CardModel card))
            {
                Assert.That(second.TryDraw(out CardModel other), Is.True);
                Assert.That(card, Is.EqualTo(other));
            }
        }
    }
}
