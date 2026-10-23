using System.Linq;
using CursedDeck.Core;
using NUnit.Framework;
namespace CursedDeck.Run.Tests
{
    public sealed class DealerIntegrationTests
    {
        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void BossCommandsReplayAndUndoExactly(int floor)
        {
            var run=new RunController(42,new RelicSpec[0]);run.DebugRestart(floor,3);
            Assert.That(run.CurrentRound.Dealer,Is.EqualTo((DealerKind)floor));
            run.State.Consumables.Add(ConsumableKind.Undo,1);
            // Start a fresh recorded round containing the consumable in its initial snapshot.
            run.DebugRestart(floor,3);
            foreach(int slot in run.Deal.SolutionSlots.Take(5))Assert.That(run.Execute(new RunAction(RunActionKind.Play,slot)));
            Assert.That(RunController.FromJson(run.ToJson()).ToJson(),Is.EqualTo(run.ToJson()));
            if(run.State.Stage==RunStage.Playing){Assert.That(run.Execute(new RunAction(RunActionKind.Undo)));Assert.That(RunController.FromJson(run.ToJson()).ToJson(),Is.EqualTo(run.ToJson()));}
        }
        [Test] public void LegacyRuleSetStillGeneratesOriginalBossDeal()
        {
            var r=new RunController(42,new RelicSpec[0],new RunConfig(dealerRulesVersion:0));r.DebugRestart(1,3);
            Assert.That(r.CurrentRound.Dealer,Is.EqualTo(DealerKind.None));
            CollectionAssert.AreEqual(r.Deal.Cards,WinnableBoardGenerator.Generate(r.Deal.Seed).Cards);
            Assert.That(RunController.FromJson(r.ToJson()).ToJson(),Is.EqualTo(r.ToJson()));
        }
        [Test] public void MirrorSnapshotDoesNotLeakHiddenRankSuitOrLegality()
        {
            var r=new RunController(42,new RelicSpec[0],new RunConfig(baseTarget:100000));r.DebugRestart(3,3);
            foreach(int slot in r.Deal.SolutionSlots.Take(3))r.Execute(new RunAction(RunActionKind.Play,slot));
            var bus=new EventBus();using(var host=new GameHost(r,bus,null,new UiSettings()))
            { var snap=host.Snapshot();var hidden=snap.Cards.Where(c=>r.CurrentRound.IsBlind(c.Slot)).ToArray();Assert.That(hidden.Length,Is.GreaterThan(0));foreach(var blind in hidden){Assert.That(blind.Rank,Is.Zero);Assert.That(blind.Suit,Is.Zero);Assert.That(blind.Visible,Is.False);Assert.That(blind.Playable);} }
        }
    }
}
