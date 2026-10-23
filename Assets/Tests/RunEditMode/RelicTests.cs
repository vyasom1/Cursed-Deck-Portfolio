using System;
using System.Collections.Generic;
using NUnit.Framework;
using CursedDeck.Core;

namespace CursedDeck.Run.Tests
{
    public sealed class RelicTests
    {
        internal static CardModel C(int rank, Suit suit = Suit.Clubs) => new CardModel((Rank)rank, suit);
        internal static RelicSpec Spec(RelicKind kind) => new RelicSpec(kind.ToString(), kind, (int)kind * 10,
            kind == RelicKind.GamblersRing ? 10 : 1,
            kind == RelicKind.Metronome ? 5 : kind == RelicKind.ColorEcho ? 3 : 1,
            kind == RelicKind.RedVelvet || kind == RelicKind.CrackedMirror ? 2m : 1m);
        internal static RunState Run(params RelicKind[] kinds)
        {
            var run = new RunState(42, 30);
            foreach (var kind in kinds) run.Acquire(Spec(kind));
            return run;
        }
        internal static RuleEngine Rules(CardModel[] cards, CardModel waste, params CardModel[] stock)
        {
            var slots = new BoardSlot[cards.Length];
            for (int i = 0; i < slots.Length; i++) slots[i] = new BoardSlot(i, 0);
            return new RuleEngine(new BoardLayout(slots), cards, waste, stock);
        }
        internal static RoundSession Begin(RunState run, CardModel[] cards, CardModel waste, int draws = 0, params CardModel[] stock)
        {
            var round = new RoundSession(Rules(cards, waste, stock), 100000m, draws, effects: new RelicManager(run));
            round.Start(); return round;
        }

        [Test]
        public void LoadedDieMakesBoardAndWasteKingsWild()
        {
            var round = Begin(Run(RelicKind.LoadedDie), new[] {C(13), C(9, Suit.Hearts), C(2)}, C(5, Suit.Diamonds));
            Assert.That(round.CanPlay(0), Is.True);
            Assert.That(round.CanPlay(1), Is.False);
            round.TryPlay(0);
            Assert.That(round.CanPlay(1), Is.True);
            Assert.That(round.CanPlay(2), Is.True);
            Assert.That(round.TryPlay(1), Is.True);
        }

        [Test]
        public void WildCardsStillCannotBypassCoveringCards()
        {
            var layout = new BoardLayout(new[] {new BoardSlot(0, 0, 1), new BoardSlot(0, 1)});
            var round = new RoundSession(new RuleEngine(layout, new[] {C(13), C(9)}, C(3), new[] {C(8, Suit.Spades)}),
                100000m, 1, effects: new RelicManager(Run(RelicKind.LoadedDie)));
            round.Start();
            Assert.That(round.CanPlay(0), Is.False);
            Assert.That(round.TryPlay(0), Is.False);
        }

        [Test]
        public void MetronomeAddsOneDrawOnFifthActualChainStep()
        {
            var round = Begin(Run(RelicKind.Metronome), new[] {C(6),C(7),C(8),C(9),C(10),C(11)}, C(5, Suit.Diamonds), 0, C(2, Suit.Spades));
            for (int i = 0; i < 4; i++) round.TryPlay(i);
            Assert.That(round.State.DrawsRemaining, Is.Zero);
            Assert.That(round.TryPlay(4), Is.True);
            Assert.That(round.Chain, Is.EqualTo(5));
            Assert.That(round.State.DrawsRemaining, Is.EqualTo(1));
            round.TryDraw();
            Assert.That(round.Chain, Is.Zero);
        }

        [Test]
        public void RedVelvetOnlyDoublesRedCardScores()
        {
            var round = Begin(Run(RelicKind.RedVelvet), new[] {C(6, Suit.Hearts), C(7)}, C(5, Suit.Diamonds));
            round.TryPlay(0); Assert.That(round.State.Score, Is.EqualTo(200m));
            round.TryPlay(1); Assert.That(round.State.Score, Is.EqualTo(325m));
        }

        [Test]
        public void CrackedMirrorOnlyRepeatsTheFirstCardScoreAndResetsNextRound()
        {
            var run = Run(RelicKind.CrackedMirror);
            var first = Begin(run, new[] {C(6),C(7)}, C(5, Suit.Diamonds));
            first.TryPlay(0); first.TryPlay(1);
            Assert.That(first.State.Score, Is.EqualTo(325m));
            Assert.That(first.State.CardsPlayed, Is.EqualTo(2));
            Assert.That(first.Chain, Is.EqualTo(2));
            var second = Begin(run, new[] {C(6)}, C(5, Suit.Diamonds));
            second.TryPlay(0);
            Assert.That(second.State.Score, Is.EqualTo(200m));
        }

        [Test]
        public void RedVelvetAndCrackedMirrorStackWithoutDoublePlayingTheCard()
        {
            var round = Begin(Run(RelicKind.RedVelvet, RelicKind.CrackedMirror), new[] {C(6, Suit.Hearts),C(7, Suit.Hearts)}, C(5, Suit.Diamonds));
            round.TryPlay(0);
            Assert.That(round.State.Score, Is.EqualTo(400m));
            Assert.That(round.State.CardsPlayed, Is.EqualTo(1));
            Assert.That(round.Chain, Is.EqualTo(1));
            round.TryPlay(1);
            Assert.That(round.State.Score, Is.EqualTo(650m));
        }

        [Test]
        public void MirrorDoesNotDoubleMetronomeResourceTriggersAndUndoRestoresBoth()
        {
            var run = Run(RelicKind.CrackedMirror);
            run.Acquire(new RelicSpec("fast-metronome", RelicKind.Metronome, 20, interval: 1));
            var round = Begin(run, new[] {C(6),C(7)}, C(5, Suit.Diamonds), 0, C(2, Suit.Spades));
            round.TryPlay(0);
            Assert.That(round.State.Score, Is.EqualTo(200m));
            Assert.That(round.State.DrawsRemaining, Is.EqualTo(1));
            round.TryUndo();
            Assert.That(round.State.Score, Is.Zero);
            Assert.That(round.State.DrawsRemaining, Is.Zero);
            round.TryPlay(0);
            Assert.That(round.State.Score, Is.EqualTo(200m));
            Assert.That(round.State.DrawsRemaining, Is.EqualTo(1));
        }

        [Test]
        public void SuitBridgeOnlyAppliesToFirstPlayAfterDrawAndUndoRestoresEligibility()
        {
            var round = Begin(Run(RelicKind.SuitBridge), new[] {C(9, Suit.Hearts),C(6, Suit.Hearts)}, C(5, Suit.Hearts), 1, C(3, Suit.Hearts));
            Assert.That(round.CanPlay(0), Is.False);
            round.TryDraw();
            Assert.That(round.CanPlay(0), Is.True);
            round.TryPlay(0);
            Assert.That(round.CanPlay(1), Is.False);
            round.TryUndo();
            Assert.That(round.CanPlay(0), Is.True);
            Assert.That(round.CanPlay(1), Is.True);
            round.TryUndo();
            Assert.That(round.CanPlay(0), Is.False);
            Assert.That(round.State.DrawsRemaining, Is.EqualTo(1));
        }

        [Test]
        public void ColorEchoOnlyRelaxesEveryThirdPlayAndRequiresMatchingColor()
        {
            var round = Begin(Run(RelicKind.ColorEcho), new[] {C(6, Suit.Hearts),C(7, Suit.Hearts),C(10, Suit.Diamonds),C(10, Suit.Spades)}, C(5, Suit.Diamonds));
            Assert.That(round.CanPlay(2), Is.False);
            round.TryPlay(0); round.TryPlay(1);
            Assert.That(round.CanPlay(2), Is.True);
            Assert.That(round.CanPlay(3), Is.False);
            round.TryPlay(2);
            Assert.That(round.State.CardsPlayed, Is.EqualTo(3));
            Assert.That(round.CanPlay(3), Is.False);
        }

        [Test]
        public void GamblersRingRerollsStuckDrawAtGoldCostAndUndoIsExact()
        {
            var run = new RunState(42, 10); run.Acquire(Spec(RelicKind.GamblersRing));
            var round = Begin(run, new[] {C(9)}, C(3, Suit.Diamonds), 1, C(5, Suit.Spades), C(8, Suit.Spades));
            Assert.That(round.TryReroll(), Is.False);
            round.TryDraw();
            Assert.That(round.State.DrawsRemaining, Is.Zero);
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Pending));
            Assert.That(round.CanReroll, Is.True);
            Assert.That(round.TryReroll(), Is.True);
            Assert.That(run.Gold, Is.Zero);
            Assert.That(round.Waste, Is.EqualTo(C(8, Suit.Spades)));
            Assert.That(round.State.DrawsRemaining, Is.Zero);
            Assert.That(round.History[1].Kind, Is.EqualTo(CommandKind.Reroll));
            round.TryUndo();
            Assert.That(run.Gold, Is.EqualTo(10));
            Assert.That(round.StockRemaining, Is.EqualTo(1));
            Assert.That(round.Waste, Is.EqualTo(C(5, Suit.Spades)));
            Assert.That(round.CanReroll, Is.True);
            round.TryReroll(); round.TryPlay(0);
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Won));
            Assert.That(run.Gold, Is.Zero);
        }

        [Test]
        public void RingCannotSpendWithoutEnoughGoldOrWithoutAStuckDraw()
        {
            var run = new RunState(1, 9); run.Acquire(Spec(RelicKind.GamblersRing));
            var round = Begin(run, new[] {C(9)}, C(3, Suit.Diamonds), 1, C(5, Suit.Spades),C(8, Suit.Spades));
            round.TryDraw();
            Assert.That(round.TryReroll(), Is.False);
            Assert.That(run.Gold, Is.EqualTo(9));
            Assert.That(round.History.Count, Is.EqualTo(1));
        }

        [Test]
        public void LastBreathReleasesReservedCardOnceAcrossAllRoundsInRun()
        {
            var run = Run(RelicKind.LastBreath);
            var first = Begin(run, new[] {C(9)}, C(3, Suit.Diamonds), 1, C(5, Suit.Spades));
            Assert.That(run.LastBreathUsed, Is.True);
            Assert.That(first.State.DrawsRemaining, Is.EqualTo(1));
            Assert.That(first.CanDraw, Is.True);
            first.TryDraw();
            Assert.That(first.State.Outcome, Is.EqualTo(RoundOutcome.Lost));
            var second = Begin(run, new[] {C(9)}, C(3, Suit.Diamonds), 0, C(8, Suit.Spades));
            Assert.That(second.State.Outcome, Is.EqualTo(RoundOutcome.Lost));
            Assert.That(second.State.DrawsRemaining, Is.Zero);
            Assert.That(run.LastBreathUsed, Is.True);
            Assert.That(first.TryUndo(), Is.False, "Old-round undo must not rewind a later run state.");
        }

        [Test]
        public void UndoRestoresLastBreathConsumptionAndCanReplayTheRescueExactly()
        {
            var run = Run(RelicKind.LastBreath);
            var round = Begin(run, new[] {C(9)}, C(3, Suit.Diamonds), 1, C(5, Suit.Spades), C(8, Suit.Spades));
            Assert.That(run.LastBreathUsed, Is.False);
            round.TryDraw();
            Assert.That(run.LastBreathUsed, Is.True);
            Assert.That(round.State.DrawsRemaining, Is.EqualTo(1));
            round.TryUndo();
            Assert.That(run.LastBreathUsed, Is.False);
            Assert.That(round.StockRemaining, Is.EqualTo(2));
            Assert.That(round.State.DrawsRemaining, Is.EqualTo(1));
            round.TryDraw(); round.TryDraw(); round.TryPlay(0);
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Won));
            Assert.That(run.LastBreathUsed, Is.True);
        }

        [Test]
        public void LastBreathNeverInventsACardWhenCustomRoundHasNoStock()
        {
            var run = Run(RelicKind.LastBreath);
            var round = Begin(run, new[] {C(9)}, C(3, Suit.Diamonds));
            Assert.That(round.State.Outcome, Is.EqualTo(RoundOutcome.Lost));
            Assert.That(run.LastBreathUsed, Is.False);
        }

        [Test]
        public void RulesWithoutRelicsRemainUnchangedAndRunHasNoUnityReference()
        {
            var round = Begin(Run(), new[] {C(13)}, C(5, Suit.Diamonds));
            Assert.That(round.CanPlay(0), Is.False);
            foreach (var dependency in typeof(RunState).Assembly.GetReferencedAssemblies())
                Assert.That(dependency.Name.StartsWith("Unity"), Is.False, dependency.Name);
        }
    }
}
