using NUnit.Framework;
using CursedDeck.Core;
namespace CursedDeck.Run.Tests
{
    public sealed class TwoPeaksIntegrationTests
    {
        [TestCase(false)][TestCase(true)]public void LegacyAndTwoPeakCheckpointReplayPreservesExactState(bool twoPeaks)
        {
            var run=new RunController(42,new RelicSpec[0],layout:twoPeaks?BoardLayout.TwoPeaks:BoardLayout.TriPeaks);
            Assert.That(run.Execute(new RunAction(RunActionKind.Play,run.Deal.SolutionSlots[0])));Assert.That(run.Execute(new RunAction(RunActionKind.Draw)));string saved=run.ToJson();var loaded=RunController.FromJson(saved);
            Assert.That(loaded.ToJson(),Is.EqualTo(saved));Assert.That(loaded.Deal.Cards.Count,Is.EqualTo(twoPeaks?19:28));
            var bus=new EventBus();using(var host=new GameHost(loaded,bus,null,new UiSettings()))Assert.That(host.Snapshot().Cards.Count,Is.EqualTo(loaded.Deal.Layout.Count));
        }
        [Test]public void TwoPeakPracticeStillTeachesMatchBlockAndUsefulDraw()
        {
            var opening=FirstMatchGuide.For(BoardLayout.TwoPeaks);var run=new RunController(opening.Seed,new RelicSpec[0],layout:BoardLayout.TwoPeaks);
            Assert.That(run.Execute(new RunAction(RunActionKind.Play,opening.Slot)));Assert.That(run.CurrentRound.HasNoMoves);Assert.That(run.Execute(new RunAction(RunActionKind.Draw)));Assert.That(run.CurrentRound.HasNoMoves,Is.False);
        }
    }
}
