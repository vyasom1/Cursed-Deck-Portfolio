using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace CursedDeck.Core.Tests
{
    public sealed class RoundFlowTests
    {
        private static CardModel C(int rank, Suit suit = Suit.Clubs) => new CardModel((Rank)rank, suit);
        private static RuleEngine Flat(int[] ranks, int waste, params int[] stock)
        {
            var slots = new BoardSlot[ranks.Length]; var cards = new CardModel[ranks.Length];
            for (int i = 0; i < ranks.Length; i++) { slots[i] = new BoardSlot(i, 0); cards[i] = C(ranks[i]); }
            var pile = new CardModel[stock.Length];
            for (int i = 0; i < stock.Length; i++) pile[i] = C(stock[i], Suit.Spades);
            return new RuleEngine(new BoardLayout(slots), cards, C(waste, Suit.Diamonds), pile);
        }
        private static RoundSession Round(int[] board, int waste, decimal target = 10000m, int draws = 0, params int[] stock)
        {
            var round = new RoundSession(Flat(board, waste, stock), target, draws);
            round.Start(); return round;
        }
        private static string Fingerprint(RoundSession round)
        {
            var text = new StringBuilder();
            text.Append(round.Waste).Append('|').Append(round.Chain).Append('|').Append(round.StockRemaining)
                .Append('|').Append(round.RemainingCards).Append('|').Append(round.State.Score).Append('|')
                .Append(round.State.DrawsRemaining).Append('|').Append(round.State.CardsPlayed).Append('|')
                .Append(round.State.Phase).Append('|').Append(round.State.Outcome);
            for (int i = 0; i < round.Layout.Count; i++) text.Append('|').Append(round.GetCard(i))
                .Append(':').Append(round.IsRemoved(i)).Append(':').Append(round.IsExposed(i));
            return text.ToString();
        }

        [Test]
        public void StartIsExplicitAndOnlyRunsOnce()
        {
            var round = new RoundSession(Flat(new[] {6,7}, 5), 1000m, 0);
            Assert.That(round.State.Phase, Is.EqualTo(RoundPhase.Start));
            Assert.That(round.TryPlay(0), Is.False);
            Assert.That(round.TryDraw(), Is.False);
            Assert.That(round.Start(), Is.True);
            Assert.That(round.State.Phase, Is.EqualTo(RoundPhase.Play));
            Assert.That(round.Start(), Is.False);
        }

        [TestCase(100)] [TestCase(99)]
        public void ExactTargetOrOvershootWinsWithBoardUncleared(int target)
        {
            var round = Round(new[] {6,7}, 5, target);
            round.TryPlay(0);
            Assert.That(round.State.Score, Is.EqualTo(100m));
            Assert.That(round.RemainingCards, Is.EqualTo(1));
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Won));
            Assert.That(round.State.Phase, Is.EqualTo(RoundPhase.End));
            Assert.That(round.TryPlay(1), Is.False);
        }

        [Test]
        public void ClearingBoardAutoWinsBelowTargetAndWinsBeforeStuckCheck()
        {
            var round = Round(new[] {6}, 5);
            round.TryPlay(0);
            Assert.That(round.State.Score, Is.LessThan(round.State.TargetScore));
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Won));
            Assert.That(round.HasNoMoves, Is.True);
        }

        [Test]
        public void FinalAllowedDrawCanEnableAnEntireWinningChain()
        {
            var round = Round(new[] {7,8}, 3, 10000m, 1, 6);
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Pending));
            Assert.That(round.TryDraw(), Is.True);
            Assert.That(round.State.DrawsRemaining, Is.Zero);
            Assert.That(round.State.Score, Is.Zero);
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Pending));
            Assert.That(round.TryPlay(0), Is.True);
            Assert.That(round.TryPlay(1), Is.True);
            Assert.That(round.State.Score, Is.EqualTo(225m));
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Won));
        }

        [Test]
        public void UselessFinalDrawLosesAndUndoRestoresPendingState()
        {
            var round = Round(new[] {9}, 3, 10000m, 1, 5);
            string before = Fingerprint(round);
            round.TryDraw();
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Lost));
            Assert.That(round.TryDraw(), Is.False);
            Assert.That(round.TryUndo(), Is.True);
            Assert.That(Fingerprint(round), Is.EqualTo(before));
            Assert.That(round.TryDraw(), Is.True);
            Assert.That(round.Waste, Is.EqualTo(C(5, Suit.Spades)));
        }

        [Test]
        public void StockExhaustionCanLoseEvenWithDrawBudgetRemaining()
        {
            var round = Round(new[] {9}, 3, 10000m, 5, 5);
            round.TryDraw();
            Assert.That(round.State.DrawsRemaining, Is.EqualTo(4));
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Lost));
        }

        [Test]
        public void ZeroDrawBudgetIsLegalAndOnlyLosesIfNoMoveExists()
        {
            var stuck = Round(new[] {9}, 3, 10000m, 0, 8);
            Assert.That(stuck.State.Outcome, Is.EqualTo(RoundOutcome.Lost));
            var playable = Round(new[] {6}, 5);
            Assert.That(playable.State.Outcome, Is.EqualTo(RoundOutcome.Pending));
            Assert.That(playable.TryPlay(0), Is.True);
            Assert.That(playable.State.Outcome, Is.EqualTo(RoundOutcome.Won));
        }

        [Test]
        public void UndoRestoresMixedCommandsAndEveryFutureStockCardExactly()
        {
            var round = Round(new[] {6,7,8}, 5, 10000m, 2, 4, 3);
            string start = Fingerprint(round);
            round.TryPlay(0); string afterPlay = Fingerprint(round);
            round.TryDraw(); string afterFirstDraw = Fingerprint(round);
            round.TryDraw();
            Assert.That(round.History.Count, Is.EqualTo(3));
            Assert.That(round.History[0].Kind, Is.EqualTo(CommandKind.Play));
            Assert.That(round.History[0].Slot, Is.EqualTo(0));
            Assert.That(round.History[1].Kind, Is.EqualTo(CommandKind.Draw));
            Assert.That(round.TryUndo(), Is.True);
            Assert.That(Fingerprint(round), Is.EqualTo(afterFirstDraw));
            Assert.That(round.TryDraw(), Is.True);
            Assert.That(round.Waste, Is.EqualTo(C(3, Suit.Spades)));
            round.TryUndo(); round.TryUndo();
            Assert.That(Fingerprint(round), Is.EqualTo(afterPlay));
            round.TryUndo();
            Assert.That(Fingerprint(round), Is.EqualTo(start));
            Assert.That(round.History.Count, Is.Zero);
            Assert.That(round.TryUndo(), Is.False);
        }

        [Test]
        public void UndoReCoversParentAndCanUndoATerminalWin()
        {
            var layout = new BoardLayout(new[] {new BoardSlot(0, 0, 1), new BoardSlot(0, 1)});
            var round = new RoundSession(new RuleEngine(layout, new[] {C(7), C(6)}, C(5), new CardModel[0]), 10000m, 0);
            round.Start(); string start = Fingerprint(round);
            round.TryPlay(1); round.TryPlay(0);
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Won));
            round.TryUndo();
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Pending));
            round.TryUndo();
            Assert.That(Fingerprint(round), Is.EqualTo(start));
            Assert.That(round.IsExposed(0), Is.False);
        }

        [Test]
        public void RejectedCommandsAreNotRecordedAndDoNotMutateAnything()
        {
            var round = Round(new[] {6,9}, 5);
            string before = Fingerprint(round);
            Assert.That(round.TryPlay(-1), Is.False);
            Assert.That(round.TryPlay(1), Is.False);
            Assert.That(round.TryDraw(), Is.False);
            Assert.That(round.History.Count, Is.Zero);
            Assert.That(Fingerprint(round), Is.EqualTo(before));
        }

        [Test]
        public void PhaseTransitionsReflectPlayDrawScoreAndEnd()
        {
            var round = new RoundSession(Flat(new[] {7}, 3, 6), 1000m, 1);
            var phases = new List<RoundPhase>(); round.PhaseChanged += phases.Add;
            round.Start(); round.TryDraw(); round.TryPlay(0);
            CollectionAssert.AreEqual(new[] {RoundPhase.Play, RoundPhase.Draw, RoundPhase.Score,
                RoundPhase.Play, RoundPhase.Score, RoundPhase.End}, phases);
        }

        private sealed class ThrowingHook : IScoreHook
        {
            public decimal Apply(ScoringStage stage, ScoreContext context, decimal value) => throw new InvalidOperationException("Test failure");
        }
        [Test]
        public void ThrowingHookRollsBackTheWholeCommand()
        {
            var round = new RoundSession(Flat(new[] {6,7}, 5), 1000m, 0, scoreHook: new ThrowingHook());
            round.Start(); string before = Fingerprint(round);
            Assert.Throws<InvalidOperationException>(() => round.TryPlay(0));
            Assert.That(Fingerprint(round), Is.EqualTo(before));
            Assert.That(round.History.Count, Is.Zero);
        }

        [Test]
        public void SessionOwnsAForkOfTheSourceRules()
        {
            var rules = Flat(new[] {6,7}, 5);
            var round = new RoundSession(rules, 1000m, 0); round.Start();
            rules.TryPlay(0);
            Assert.That(round.IsRemoved(0), Is.False);
            Assert.That(round.Waste, Is.EqualTo(C(5, Suit.Diamonds)));
        }

        [Test]
        public void RejectsInvalidRoundConfiguration()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoundState(0m, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoundState(1m, -1));
            Assert.Throws<ArgumentNullException>(() => new RoundSession(null, 100m, 1));
        }
    }
}
