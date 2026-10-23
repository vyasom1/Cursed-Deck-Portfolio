using System;
using System.Collections.Generic;
using NUnit.Framework;
using CursedDeck.Core;

namespace CursedDeck.Run.Tests
{
    public sealed class HookAndApplicationTests
    {
        private sealed class Spy : RelicBase
        {
            private readonly List<string> log;
            public int Calls { get; private set; }
            public bool ThrowOnScore;
            public Spy(string id, int order, List<string> log) : base(new RelicSpec(id, RelicKind.LoadedDie, order)) { this.log = log; }
            private void Record(string hook) { log.Add(hook + ":" + Definition.Id); Calls++; }
            public override void OnRoundStart(RelicContext c) => Record("start");
            public override void OnDraw(RelicContext c, bool reroll) => Record("draw");
            public override void OnCardPlayed(RelicContext c, CardModel card, CardModel waste)
            {
                Record("play");
                if (ThrowOnScore) c.Run.TrySpendGold(10);
            }
            public override decimal OnScore(RelicContext c, ScoringStage stage, ScoreContext score, decimal value)
            {
                Record(stage.ToString());
                if (ThrowOnScore) throw new InvalidOperationException("Deliberate test hook failure");
                return value;
            }
            public override void OnRoundEnd(RelicContext c, RoundOutcome outcome) => Record("end");
            public override object CaptureState() => Calls;
            public override void RestoreState(object state) { Calls = (int)state; }
        }

        [Test]
        public void HookOrderIsStableByPriorityThenOrdinalIdAcrossEveryStage()
        {
            var log = new List<string>(); var run = new RunState(1);
            var manager = new RelicManager(run, new IRelic[] {new Spy("beta",20,log), new Spy("zeta",10,log),new Spy("alpha",10,log)});
            var round = new RoundSession(RelicTests.Rules(new[] {RelicTests.C(7)}, RelicTests.C(3, Suit.Diamonds), RelicTests.C(6, Suit.Spades)),
                1000m, 1, effects: manager);
            round.Start(); round.TryDraw(); round.TryPlay(0);
            var expected = new List<string>();
            foreach (string hook in new[] {"start","draw","play","BasePoints","FlatBonuses","ChainMultiplier","RelicMultipliers","end"})
                foreach (string id in new[] {"alpha","zeta","beta"}) expected.Add(hook + ":" + id);
            CollectionAssert.AreEqual(expected, log);
            Assert.That(round.Start(), Is.False);
            Assert.That(round.TryPlay(0), Is.False);
            CollectionAssert.AreEqual(expected, log);
        }

        [Test]
        public void UndoRestoresCustomRelicStateAndReplayDoesNotDuplicateIt()
        {
            var spy = new Spy("spy",0,new List<string>()); var run = new RunState(1);
            var round = new RoundSession(RelicTests.Rules(new[] {RelicTests.C(6)},RelicTests.C(5)), 1000m,0,
                effects: new RelicManager(run,new[] {spy}));
            round.Start(); Assert.That(spy.Calls, Is.EqualTo(1));
            round.TryPlay(0); Assert.That(spy.Calls, Is.EqualTo(7));
            round.TryUndo(); Assert.That(spy.Calls, Is.EqualTo(1));
            round.TryPlay(0); Assert.That(spy.Calls, Is.EqualTo(7));
        }

        [Test]
        public void ThrowingRelicRollsBackGoldCountersBoardAndHistory()
        {
            var spy = new Spy("spy",0,new List<string>()) {ThrowOnScore = true}; var run = new RunState(1,30);
            var round = new RoundSession(RelicTests.Rules(new[] {RelicTests.C(6)},RelicTests.C(5)), 1000m,0,
                effects: new RelicManager(run,new[] {spy}));
            round.Start();
            Assert.Throws<InvalidOperationException>(() => round.TryPlay(0));
            Assert.That(run.Gold, Is.EqualTo(30));
            Assert.That(spy.Calls, Is.EqualTo(1));
            Assert.That(round.RemainingCards, Is.EqualTo(1));
            Assert.That(round.IsRemoved(0), Is.False);
            Assert.That(round.State.Score, Is.Zero);
            Assert.That(round.History.Count, Is.Zero);
            Assert.That(round.State.Phase, Is.EqualTo(RoundPhase.Play));
        }

        [Test]
        public void StateNotificationsCannotReenterOrUndoTheExecutingCommand()
        {
            var round = new RoundSession(RelicTests.Rules(new[] {RelicTests.C(6),RelicTests.C(7)},RelicTests.C(5)),1000m,0);
            round.Start();
            round.PhaseChanged += phase => {
                Assert.That(round.TryPlay(1), Is.False);
                Assert.That(round.TryUndo(), Is.False);
                Assert.That(round.TryDraw(), Is.False);
                Assert.That(round.Seal(), Is.False);
            };
            Assert.That(round.TryPlay(0), Is.True);
            Assert.That(round.State.CardsPlayed, Is.EqualTo(1));
            Assert.That(round.History.Count, Is.EqualTo(1));
        }

        [Test]
        public void ApplicationPublishesCommittedStateOnlyForSuccessfulCommands()
        {
            var run = new RunState(1); var bus = new EventBus();
            var round = new RoundSession(RelicTests.Rules(new[] {RelicTests.C(6),RelicTests.C(7)},RelicTests.C(5)),1000m,0);
            var app = new RoundApplication(round,run,bus); var states = new List<RoundViewState>();
            var history = new List<int>();
            bus.RoundChanged += state => { states.Add(state); history.Add(round.History.Count); };
            app.Start(); app.Play(1); app.Play(0); app.Undo();
            Assert.That(states.Count, Is.EqualTo(3));
            Assert.That(states[1].Score, Is.EqualTo(100m));
            Assert.That(states[2].Score, Is.Zero);
            CollectionAssert.AreEqual(new[] {0,1,0}, history);
        }

        [Test]
        public void RunRejectsConcurrentRoundsAndOldRoundUndoAfterAdvancing()
        {
            var run = new RunState(1);
            var first = RelicTests.Begin(run,new[] {RelicTests.C(6)},RelicTests.C(5));
            var second = new RoundSession(RelicTests.Rules(new[] {RelicTests.C(6)},RelicTests.C(5)),1000m,0,effects:new RelicManager(run));
            Assert.Throws<InvalidOperationException>(() => second.Start());
            Assert.That(first.CanPlay(0), Is.True);
            first.TryPlay(0);
            Assert.That(second.Start(), Is.True);
            Assert.That(first.TryUndo(), Is.False);
        }

        [Test]
        public void InvalidAndDuplicateRelicsAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new RelicSpec("",RelicKind.LoadedDie,0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RelicSpec("a",(RelicKind)99,0));
            Assert.Throws<ArgumentException>(() => new RelicSpec("a",RelicKind.Metronome,0,interval:0));
            Assert.Throws<ArgumentException>(() => new RelicSpec("a",RelicKind.RedVelvet,0,multiplier:0));
            Assert.Throws<ArgumentException>(() => new RelicSpec("a",RelicKind.GamblersRing,0,amount:0));
            var spy = new Spy("duplicate",0,new List<string>());
            Assert.Throws<ArgumentException>(() => new RelicManager(new RunState(1),new[] {spy,spy}));
            Assert.Throws<ArgumentException>(() => new RelicManager(new RunState(1),new IRelic[] {null}));
            Assert.Throws<ArgumentNullException>(() => new RelicManager(null));
        }

        [Test]
        public void GoldAndOwnershipValidateBoundaries()
        {
            var run = new RunState(1,10); var relic = RelicTests.Spec(RelicKind.LoadedDie);
            Assert.That(run.Acquire(relic), Is.True); Assert.That(run.Acquire(relic), Is.False);
            Assert.That(run.TrySpendGold(11), Is.False); Assert.That(run.Gold, Is.EqualTo(10));
            Assert.That(run.TrySpendGold(10), Is.True); Assert.That(run.Gold, Is.Zero);
            run.AddGold(5); Assert.That(run.Gold, Is.EqualTo(5));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.TrySpendGold(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.AddGold(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunState(1,-1));
        }
    }
}
