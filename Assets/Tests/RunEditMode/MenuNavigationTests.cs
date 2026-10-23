using System.Linq;
using NUnit.Framework;
using CursedDeck.Core;

namespace CursedDeck.Run.Tests
{
    public sealed class MenuNavigationTests
    {
        private EventBus bus;private GameHost host;private RunController run;
        private static RelicSpec[] Catalog()=>Enumerable.Range(0,8).Select(i=>RelicTests.Spec((RelicKind)i)).ToArray();
        [SetUp]public void Setup(){bus=new EventBus();run=new RunController(42,Catalog());host=new GameHost(run,bus,null,new UiSettings());}
        [TearDown]public void Cleanup()=>host.Dispose();
        [Test]public void FreshTitleCannotContinueOrSaveAnUnstartedHand()
        {
            int saves=0;host.SaveRequested+=()=>saves++;host.ShowStartMenu(false);
            Assert.That(host.Snapshot().CanContinueRun,Is.False);bus.Send(new GameCommand(GameCommandKind.Resume));
            Assert.That(host.Menu,Is.EqualTo(MenuMode.Title));bus.Send(new GameCommand(GameCommandKind.ReturnToTitle));Assert.That(saves,Is.Zero);
        }
        [Test]public void PauseAndTitleBlockAllGameplayCommandsAndResumeRestoresExactState()
        {
            run.Execute(new RunAction(RunActionKind.Play,run.Deal.SolutionSlots[0]));string before=run.ToJson();
            foreach(var mode in new[]{GameCommandKind.Pause,GameCommandKind.ReturnToTitle})
            {
                bus.Send(new GameCommand(mode));
                foreach(var kind in new[]{GameCommandKind.Play,GameCommandKind.Draw,GameCommandKind.Undo,GameCommandKind.Peek,GameCommandKind.Shuffle,GameCommandKind.ChooseReward,GameCommandKind.Buy,GameCommandKind.Continue,GameCommandKind.NewRun,GameCommandKind.MetaDaily,GameCommandKind.TestAdDraw,GameCommandKind.TestAdRevive})bus.Send(new GameCommand(kind,run.Deal.SolutionSlots[1]));
                Assert.That(run.ToJson(),Is.EqualTo(before));bus.Send(new GameCommand(GameCommandKind.Resume));Assert.That(host.Menu,Is.EqualTo(MenuMode.Playing));Assert.That(run.ToJson(),Is.EqualTo(before));
            }
            bus.Send(new GameCommand(GameCommandKind.Draw));Assert.That(run.ToJson(),Is.Not.EqualTo(before));
        }
        [Test]public void ConfirmedMenuRunReplacesOnceAndSavesAfterEntering()
        {
            int starts=0,saves=0;host.ShowStartMenu(false);host.MenuNewRunRequested+=()=>{starts++;host.Replace(new RunController(7,Catalog()));};host.SaveRequested+=()=>{if(host.HasEnteredRun)saves++;};
            bus.Send(new GameCommand(GameCommandKind.MenuNewRun));bus.Send(new GameCommand(GameCommandKind.MenuNewRun));
            Assert.That(starts,Is.EqualTo(1));Assert.That(saves,Is.EqualTo(1));Assert.That(host.Controller.State.Seed,Is.EqualTo(7));Assert.That(host.Menu,Is.EqualTo(MenuMode.Playing));
        }
        [Test]public void SettingsAndStatsRemainAvailableWithoutUnpausing()
        {
            int toggles=0,meta=0;host.SettingsRequested+=k=>toggles++;host.MetaRequested+=c=>meta++;bus.Send(new GameCommand(GameCommandKind.Pause));
            bus.Send(new GameCommand(GameCommandKind.ToggleHaptics));bus.Send(new GameCommand(GameCommandKind.MetaRefresh));
            Assert.That(toggles,Is.EqualTo(1));Assert.That(meta,Is.EqualTo(1));Assert.That(host.Menu,Is.EqualTo(MenuMode.Paused));
        }
        [Test]public void CheckpointOnRewardScreenResumesWithIdenticalOffers()
        {
            foreach(int slot in run.Deal.SolutionSlots){if(run.State.Stage!=RunStage.Playing)break;run.Execute(new RunAction(RunActionKind.Play,slot));}
            Assert.That(run.State.Stage,Is.EqualTo(RunStage.Reward));string before=run.ToJson();var offers=host.Snapshot().Offers.Select(o=>o.Id).ToArray();
            host.ShowStartMenu(true);bus.Send(new GameCommand(GameCommandKind.Resume));
            Assert.That(run.ToJson(),Is.EqualTo(before));CollectionAssert.AreEqual(offers,host.Snapshot().Offers.Select(o=>o.Id));
        }
    }
}
