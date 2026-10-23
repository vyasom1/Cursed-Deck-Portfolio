using System.Linq;
using CursedDeck.Core;
using CursedDeck.Data;
using NUnit.Framework;
using UnityEngine;
namespace CursedDeck.Run.Tests
{
    public sealed class PortfolioRelicTests
    {
        private RelicSpec Find(RelicKind kind)=>Resources.Load<GameContent>("CursedDeck/Content").CreateCatalog().Single(r=>r.Kind==kind);
        private RunState Run(params RelicKind[] kinds){var run=new RunState(1,30);foreach(var kind in kinds)run.Acquire(Find(kind));return run;}
        [Test] public void GraveCompassIsGatedByChainAndUndoRestoresEligibility()
        {
            var r=RelicTests.Begin(Run(RelicKind.GraveCompass),new[]{RelicTests.C(2),RelicTests.C(3),RelicTests.C(4),RelicTests.C(1,Suit.Spades)},RelicTests.C(1));
            Assert.That(r.CanPlay(3),Is.False);r.TryPlay(0);r.TryPlay(1);Assert.That(r.CanPlay(3),Is.False);r.TryPlay(2);Assert.That(r.CanPlay(3));r.TryPlay(3);r.TryUndo();r.TryUndo();Assert.That(r.CanPlay(3),Is.False);
        }
        [TestCase(false)] [TestCase(true)] public void VoidStepAllowsOnlyEveryFourthTwoRankJumpIncludingWrap(bool wrap)
        {
            var cards=wrap?new[]{RelicTests.C(10),RelicTests.C(11),RelicTests.C(12),RelicTests.C(1),RelicTests.C(3)}:new[]{RelicTests.C(2),RelicTests.C(3),RelicTests.C(4),RelicTests.C(6),RelicTests.C(8)};
            var r=RelicTests.Begin(Run(RelicKind.VoidStep),cards,RelicTests.C(wrap?9:1,Suit.Hearts),1,RelicTests.C(7,Suit.Spades));for(int i=0;i<3;i++)Assert.That(r.TryPlay(i));Assert.That(r.CanPlay(3));Assert.That(r.TryPlay(3));Assert.That(r.CanPlay(4),Is.False);
        }
        [Test] public void BoneAbacusAndBoneCrownComposeFlatThenChainThenAceMultiplier()
        {
            var manager=new RelicManager(Run(RelicKind.BoneAbacus,RelicKind.BoneCrown));var round=new RoundSession(RelicTests.Rules(new[]{RelicTests.C(1),RelicTests.C(2)},RelicTests.C(13,Suit.Hearts)),100000,0,effects:manager);round.Start();
            Assert.That(new ScoringPipeline().Evaluate(new ScoreContext(RelicTests.C(1),3),manager),Is.EqualTo(345));Assert.That(new ScoringPipeline().Evaluate(new ScoreContext(RelicTests.C(1,Suit.Hearts),3),manager),Is.EqualTo(300));
        }
        [Test] public void SoulLedgerGoldIsTransactionalAndUndoRestoresIt()
        {
            var run=Run(RelicKind.SoulLedger);var r=RelicTests.Begin(run,new[]{RelicTests.C(11),RelicTests.C(3,Suit.Spades)},RelicTests.C(10,Suit.Hearts),1,RelicTests.C(12,Suit.Diamonds));Assert.That(r.TryPlay(0));Assert.That(run.Gold,Is.EqualTo(32));Assert.That(r.TryUndo());Assert.That(run.Gold,Is.EqualTo(30));
        }
        [Test] public void StillHourComposesWithClockmakerWithoutCreatingPhysicalCards()
        {
            var deal=WinnableBoardGenerator.Generate(42,BoardLayout.TriPeaks,DealerKind.Clockmaker);var r=new RoundSession(deal.CreateGame(),100000,23,effects:new RelicManager(Run(RelicKind.StillHour)));r.Start();Assert.That(r.State.DrawsRemaining,Is.EqualTo(22));Assert.That(r.StockRemaining,Is.EqualTo(23));Assert.That(r.Start(),Is.False);Assert.That(r.State.DrawsRemaining,Is.EqualTo(22));
        }
        [Test] public void ClockmakerAndMetronomeRefundBothHooksAndUndoBoth()
        {
            var deal=WinnableBoardGenerator.Generate(42,BoardLayout.TriPeaks,DealerKind.Clockmaker);var r=new RoundSession(deal.CreateGame(),100000,23,effects:new RelicManager(Run(RelicKind.Metronome)));r.Start();foreach(int slot in deal.SolutionSlots.Take(5))Assert.That(r.TryPlay(slot));Assert.That(r.State.DrawsRemaining,Is.EqualTo(22));r.TryUndo();Assert.That(r.State.DrawsRemaining,Is.EqualTo(20));
        }
        [Test] public void FrostPocketRefundIsOncePerRoundAndItsFlagUndoesExactly()
        {
            var r=RelicTests.Begin(Run(RelicKind.FrostPocket),new[]{RelicTests.C(3)},RelicTests.C(1,Suit.Hearts),2,RelicTests.C(8,Suit.Diamonds),RelicTests.C(7,Suit.Spades),RelicTests.C(6,Suit.Hearts));r.TryDraw();Assert.That(r.State.DrawsRemaining,Is.EqualTo(2));r.TryUndo();r.TryDraw();Assert.That(r.State.DrawsRemaining,Is.EqualTo(2));r.TryDraw();Assert.That(r.State.DrawsRemaining,Is.EqualTo(1));r.TryUndo();r.TryDraw();Assert.That(r.State.DrawsRemaining,Is.EqualTo(1));
        }
        [Test] public void ApprenticeVetoCannotBeBypassedByLoadedDie()
        {
            var board=new[]{RelicTests.C(13),RelicTests.C(2),RelicTests.C(3),RelicTests.C(4)};var layout=new BoardLayout(Enumerable.Range(0,4).Select(i=>new BoardSlot(i,0)).ToArray());var r=new RoundSession(new RuleEngine(layout,board,RelicTests.C(1,Suit.Hearts),new CardModel[0],DealerKind.Apprentice),100000,0,effects:new RelicManager(Run(RelicKind.LoadedDie)));r.Start();Assert.That(r.CanPlay(0),Is.False);for(int i=1;i<4;i++)Assert.That(r.TryPlay(i));Assert.That(r.CanPlay(0));
        }
        [Test] public void ExhaustedPoolPaysGoldExactlyOnceAndReplays()
        {
            var catalog=Resources.Load<GameContent>("CursedDeck/Content").CreateCatalog();var r=new RunController(42,catalog,startingRelics:catalog);foreach(int slot in r.Deal.SolutionSlots){if(r.State.Stage!=RunStage.Playing)break;r.Execute(new RunAction(RunActionKind.Play,slot));}
            Assert.That(r.Reward.Options.Count,Is.Zero);Assert.That(r.Reward.FallbackGold,Is.EqualTo(25));int gold=r.State.Gold;Assert.That(r.ChooseReward(null));Assert.That(r.State.Gold,Is.EqualTo(gold+25));Assert.That(r.ChooseReward(null),Is.False);Assert.That(RunController.FromJson(r.ToJson()).ToJson(),Is.EqualTo(r.ToJson()));
        }
        [Test] public void FullRunSimulatorIsDeterministicAndUsesOrganicAcquisition()
        {
            var catalog=Resources.Load<GameContent>("CursedDeck/Content").CreateCatalog();var a=BalanceSimulator.RunFull(42,catalog);var b=BalanceSimulator.RunFull(42,catalog);Assert.That(a.Won,Is.EqualTo(b.Won));Assert.That(a.Score,Is.EqualTo(b.Score));Assert.That(a.Steps,Is.EqualTo(b.Steps));Assert.That(a.BlindMisses,Is.EqualTo(b.BlindMisses));CollectionAssert.AreEqual(a.Owned,b.Owned);CollectionAssert.AreEqual(a.RoundScores,b.RoundScores);Assert.That(a.Reached[0]);Assert.That(a.Steps,Is.InRange(1,4096));
        }
    }
}
