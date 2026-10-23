using System;
using NUnit.Framework;

namespace CursedDeck.Core.Tests
{
    public sealed class RuleEngineTests
    {
        private static CardModel Card(int rank, Suit suit = Suit.Clubs) => new CardModel((Rank)rank, suit);
        private static BoardLayout Flat(int count)
        {
            var slots = new BoardSlot[count];
            for (int i = 0; i < count; i++) slots[i] = new BoardSlot(i, 0);
            return new BoardLayout(slots);
        }

        [Test]
        public void AllRankSuitPairsObeyAdjacencyIncludingWraparound()
        {
            for (int a = 1; a <= 13; a++)
                for (int b = 1; b <= 13; b++)
                    for (int sa = 0; sa < 4; sa++)
                        for (int sb = 0; sb < 4; sb++)
                        {
                            bool expected = b == a % 13 + 1 || a == b % 13 + 1;
                            Assert.That(RuleEngine.AreAdjacent(Card(a, (Suit)sa), Card(b, (Suit)sb)),
                                Is.EqualTo(expected), "ranks " + a + "," + b);
                        }
            Assert.That(RuleEngine.AreAdjacent(default, Card(1)), Is.False);
            Assert.That(RuleEngine.AreAdjacent(Card(1), default), Is.False);
        }

        [TestCase(13, 1)] [TestCase(1, 13)] [TestCase(6, 5)] [TestCase(5, 6)]
        public void SuccessfulPlayUpdatesWasteRemovalAndChain(int rank, int waste)
        {
            var game = new RuleEngine(Flat(1), new[] { Card(rank) }, Card(waste), new CardModel[0]);
            Assert.That(game.TryPlay(0), Is.True);
            Assert.That(game.Waste, Is.EqualTo(Card(rank)));
            Assert.That(game.Chain, Is.EqualTo(1));
            Assert.That(game.RemainingCards, Is.Zero);
            Assert.That(game.IsRemoved(0), Is.True);
            Assert.That(game.IsCleared, Is.True);
            Assert.That(game.IsExposed(0), Is.False);
            Assert.That(game.TryPlay(0), Is.False);
            Assert.That(game.Chain, Is.EqualTo(1));
        }

        [Test]
        public void BothCoveringCardsMustBeRemovedBeforeParentCanBePlayed()
        {
            var layout = new BoardLayout(new[] {new BoardSlot(0, 0, 1, 2), new BoardSlot(0, 1), new BoardSlot(1, 1)});
            var game = new RuleEngine(layout, new[] {Card(7), Card(6, Suit.Hearts), Card(8, Suit.Spades)},
                Card(7, Suit.Diamonds), new[] {Card(7, Suit.Spades)});
            Assert.That(game.IsExposed(0), Is.False);
            Assert.That(game.TryPlay(0), Is.False);
            Assert.That(game.TryPlay(1), Is.True);
            Assert.That(game.IsExposed(0), Is.False);
            Assert.That(game.HasNoMoves, Is.True);
            Assert.That(game.TryDraw(), Is.True);
            Assert.That(game.Chain, Is.Zero);
            Assert.That(game.TryPlay(2), Is.True);
            Assert.That(game.IsExposed(0), Is.True);
            Assert.That(game.TryPlay(0), Is.True);
            Assert.That(game.Chain, Is.EqualTo(2));
        }

        [Test]
        public void NoMoveDetectionIgnoresCoveredAndRemovedCards()
        {
            var layout = new BoardLayout(new[] {new BoardSlot(0, 0, 1), new BoardSlot(0, 1)});
            var game = new RuleEngine(layout, new[] {Card(5), Card(9)}, Card(4), new[] {Card(8)});
            Assert.That(game.HasNoMoves, Is.True);
            Assert.That(game.IsCleared, Is.False);
            Assert.That(game.TryDraw(), Is.True);
            Assert.That(game.HasNoMoves, Is.False);
            Assert.That(game.TryPlay(1), Is.True);
            Assert.That(game.HasNoMoves, Is.True);
            Assert.That(game.RemainingCards, Is.EqualTo(1));
        }

        [Test]
        public void ChainCountsConsecutivePlaysAndVoluntaryDrawResetsIt()
        {
            var game = new RuleEngine(Flat(3), new[] {Card(6), Card(7), Card(9)}, Card(5), new[] {Card(8)});
            Assert.That(game.TryPlay(0), Is.True);
            Assert.That(game.TryPlay(1), Is.True);
            Assert.That(game.Chain, Is.EqualTo(2));
            Assert.That(game.HasNoMoves, Is.True);
            Assert.That(game.TryDraw(), Is.True);
            Assert.That(game.Waste, Is.EqualTo(Card(8)));
            Assert.That(game.Chain, Is.Zero);
            Assert.That(game.TryPlay(2), Is.True);
            Assert.That(game.Chain, Is.EqualTo(1));

            var voluntary = new RuleEngine(Flat(1), new[] {Card(6)}, Card(5), new[] {Card(7)});
            Assert.That(voluntary.HasNoMoves, Is.False);
            Assert.That(voluntary.TryDraw(), Is.True);
            Assert.That(voluntary.StockRemaining, Is.Zero);
        }

        [Test]
        public void FailedActionsNeverChangeStateOrResetChain()
        {
            var game = new RuleEngine(Flat(2), new[] {Card(6), Card(9)}, Card(5), new CardModel[0]);
            game.TryPlay(0);
            Assert.That(game.TryPlay(-1), Is.False);
            Assert.That(game.TryPlay(2), Is.False);
            Assert.That(game.TryPlay(0), Is.False);
            Assert.That(game.TryPlay(1), Is.False);
            Assert.That(game.TryDraw(), Is.False);
            Assert.That(game.Waste, Is.EqualTo(Card(6)));
            Assert.That(game.Chain, Is.EqualTo(1));
            Assert.That(game.RemainingCards, Is.EqualTo(1));
            Assert.That(game.StockRemaining, Is.Zero);
        }

        [Test]
        public void CannotDrawAfterBoardIsCleared()
        {
            var game = new RuleEngine(Flat(1), new[] {Card(6)}, Card(5), new[] {Card(7)});
            game.TryPlay(0);
            Assert.That(game.HasNoMoves, Is.True);
            Assert.That(game.TryDraw(), Is.False);
            Assert.That(game.StockRemaining, Is.EqualTo(1));
            Assert.That(game.Chain, Is.EqualTo(1));
        }

        [Test]
        public void InputCollectionsAreCopied()
        {
            var cards = new[] {Card(6)}; var stock = new[] {Card(7)};
            var game = new RuleEngine(Flat(1), cards, Card(5), stock);
            cards[0] = Card(9); stock[0] = Card(10);
            Assert.That(game.GetCard(0), Is.EqualTo(Card(6)));
            game.TryDraw();
            Assert.That(game.Waste, Is.EqualTo(Card(7)));
        }

        [Test]
        public void RejectsMalformedStateAndDuplicatesAcrossAllZones()
        {
            var layout = Flat(1); var empty = new CardModel[0];
            Assert.Throws<ArgumentNullException>(() => new RuleEngine(null, new[] {Card(6)}, Card(5), empty));
            Assert.Throws<ArgumentNullException>(() => new RuleEngine(layout, null, Card(5), empty));
            Assert.Throws<ArgumentNullException>(() => new RuleEngine(layout, new[] {Card(6)}, Card(5), null));
            Assert.Throws<ArgumentException>(() => new RuleEngine(layout, empty, Card(5), empty));
            Assert.Throws<ArgumentException>(() => new RuleEngine(layout, new[] {Card(5)}, Card(5), empty));
            Assert.Throws<ArgumentException>(() => new RuleEngine(layout, new[] {Card(6)}, Card(5), new[] {Card(6)}));
            Assert.Throws<ArgumentException>(() => new RuleEngine(layout, new[] {Card(6)}, Card(5), new[] {Card(5)}));
            Assert.Throws<ArgumentException>(() => new RuleEngine(Flat(2), new[] {Card(6), Card(6)}, Card(5), empty));
            Assert.Throws<ArgumentException>(() => new RuleEngine(layout, new[] {default(CardModel)}, Card(5), empty));
            Assert.Throws<ArgumentException>(() => new RuleEngine(layout, new[] {Card(6)}, default, empty));
            Assert.Throws<ArgumentException>(() => new RuleEngine(layout, new[] {Card(6)}, Card(5), new[] {default(CardModel)}));
        }
    }
}
