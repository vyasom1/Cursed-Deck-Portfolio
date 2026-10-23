using System;
using System.Linq;
using CursedDeck.Core;
using NUnit.Framework;
namespace CursedDeck.Core.Tests
{
    public sealed class BossRuleTests
    {
        [TestCase(DealerKind.Apprentice)] [TestCase(DealerKind.Clockmaker)] [TestCase(DealerKind.Mirror)]
        public void ThousandSeededBossBoardsHaveLegalClearingWitness(DealerKind dealer)
        {
            foreach(int seed in Enumerable.Range(-500,1000).Concat(new[]{int.MinValue,int.MaxValue}))
            {
                var deal=WinnableBoardGenerator.Generate(seed,BoardLayout.TriPeaks,dealer);
                var repeat=WinnableBoardGenerator.Generate(seed,BoardLayout.TriPeaks,dealer);
                CollectionAssert.AreEqual(deal.Cards,repeat.Cards);
                var game=deal.CreateGame();
                foreach(int slot in deal.SolutionSlots)Assert.That(game.TryPlay(slot),Is.True,$"dealer={dealer} seed={seed} slot={slot}");
                Assert.That(game.IsCleared);Assert.That(game.StockRemaining,Is.EqualTo(23));
            }
        }
        private static RuleEngine Line(DealerKind dealer)
        {
            var slots=new BoardSlot[8];for(int i=0;i<8;i++)slots[i]=new BoardSlot(i,0,Array.Empty<int>());
            return new RuleEngine(new BoardLayout(slots),new[]{new CardModel(Rank.Two,Suit.Clubs),new CardModel(Rank.Three,Suit.Clubs),new CardModel(Rank.Four,Suit.Clubs),new CardModel(Rank.Five,Suit.Clubs),new CardModel(Rank.Six,Suit.Clubs),new CardModel(Rank.Seven,Suit.Clubs),new CardModel(Rank.Jack,Suit.Clubs),new CardModel(Rank.Ace,Suit.Hearts)},new CardModel(Rank.Ace,Suit.Clubs),new[]{new CardModel(Rank.Ten,Suit.Diamonds)},dealer);
        }
        [Test] public void ApprenticeLocksFacesUntilThreeNumberedCardsAndUndoRelocks()
        {
            var g=Line(DealerKind.Apprentice);Assert.That(g.TryDraw());Assert.That(g.CanPlay(6),Is.False);
            Assert.That(DealerStrategies.For(DealerKind.Apprentice).Allows(new CardModel(Rank.King,Suit.Spades),2),Is.False);
            Assert.That(DealerStrategies.For(DealerKind.Apprentice).Allows(new CardModel(Rank.King,Suit.Spades),3));
            var r=new RoundSession(Line(DealerKind.Apprentice),100000,23);r.Start();for(int i=0;i<3;i++)Assert.That(r.TryPlay(i));
            Assert.That(r.TryDraw());Assert.That(r.CanPlay(6));Assert.That(r.TryUndo());Assert.That(r.TryUndo());Assert.That(r.TryDraw());Assert.That(r.CanPlay(6),Is.False);
        }
        [Test] public void ClockmakerReducesAllowanceRefundsFiveAndSixAndUndoRestores()
        {
            var r=new RoundSession(Line(DealerKind.Clockmaker),100000,23);r.Start();Assert.That(r.State.DrawsRemaining,Is.EqualTo(20));
            for(int i=0;i<4;i++)Assert.That(r.TryPlay(i));Assert.That(r.State.DrawsRemaining,Is.EqualTo(20));
            r.TryPlay(4);Assert.That(r.State.DrawsRemaining,Is.EqualTo(21));r.TryPlay(5);Assert.That(r.State.DrawsRemaining,Is.EqualTo(22));
            r.TryUndo();Assert.That(r.State.DrawsRemaining,Is.EqualTo(21));r.TryUndo();Assert.That(r.State.DrawsRemaining,Is.EqualTo(20));
            Assert.That(DealerStrategies.For(DealerKind.Clockmaker).StartingDraws(2),Is.Zero);
        }
        [Test] public void MirrorHidesBeforeFourthValidatesBlindAndUndoRestores()
        {
            var r=new RoundSession(Line(DealerKind.Mirror),100000,23);r.Start();for(int i=0;i<3;i++)r.TryPlay(i);
            Assert.That(r.IsBlind(3));Assert.That(r.TryPlay(6),Is.False);Assert.That(r.IsBlind(3));
            Assert.That(r.TryPlay(3));Assert.That(r.IsBlind(4),Is.False);r.TryUndo();Assert.That(r.IsBlind(3));
            r.TryDraw();Assert.That(r.IsBlind(3));
        }
    }
}
