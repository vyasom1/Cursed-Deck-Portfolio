using NUnit.Framework;
using CursedDeck.Run;
using CursedDeck.Data;
using UnityEngine;
namespace CursedDeck.Run.Tests
{
    public sealed class GuidedTutorialTests
    {
        [Test] public void PracticeLocksCommandsAndRestoresExactRegularRunAfterCompletion()
        {
            var data=Resources.Load<GameContent>("CursedDeck/Content");var run=new RunController(77,data.CreateCatalog());var bus=new EventBus();
            using(var host=new GameHost(run,bus,data.CreatePresentation(),new UiSettings()))
            {
                string before=run.ToJson();int completed=0;host.ServiceRequested+=c=>{if(c.Kind==GameCommandKind.TutorialComplete)completed++;};
                var practice=new RunController(FirstMatchGuide.Seed,data.CreateCatalog());host.BeginTutorial(practice);string initial=practice.ToJson();
                foreach(var kind in new[]{GameCommandKind.Play,GameCommandKind.Draw,GameCommandKind.Undo,GameCommandKind.Peek,GameCommandKind.Shuffle,GameCommandKind.Continue,GameCommandKind.NewRun,GameCommandKind.MenuDaily,GameCommandKind.TutorialComplete})bus.Send(new GameCommand(kind,FirstMatchGuide.Slot));
                Assert.That(practice.ToJson(),Is.EqualTo(initial));Assert.That(completed,Is.Zero);
                bus.Send(new GameCommand(GameCommandKind.TutorialNext));Assert.That(host.Snapshot().TutorialStage,Is.EqualTo(TutorialStage.Match));
                bus.Send(new GameCommand(GameCommandKind.Play,(FirstMatchGuide.Slot+1)%28));bus.Send(new GameCommand(GameCommandKind.Draw));Assert.That(practice.ToJson(),Is.EqualTo(initial));
                bus.Send(new GameCommand(GameCommandKind.Play,FirstMatchGuide.Slot));Assert.That(host.Snapshot().Chain,Is.EqualTo(1));Assert.That(host.Snapshot().TutorialStage,Is.EqualTo(TutorialStage.Chain));Assert.That(practice.CurrentRound.HasNoMoves);
                bus.Send(new GameCommand(GameCommandKind.Draw));Assert.That(practice.CurrentRound.History.Count,Is.EqualTo(1));
                bus.Send(new GameCommand(GameCommandKind.Pause));bus.Send(new GameCommand(GameCommandKind.TutorialNext));Assert.That(host.Snapshot().TutorialStage,Is.EqualTo(TutorialStage.Chain));bus.Send(new GameCommand(GameCommandKind.Resume));
                bus.Send(new GameCommand(GameCommandKind.TutorialNext));Assert.That(host.Snapshot().TutorialStage,Is.EqualTo(TutorialStage.Draw));
                bus.Send(new GameCommand(GameCommandKind.Draw));Assert.That(host.Snapshot().TutorialStage,Is.EqualTo(TutorialStage.Complete));
                bus.Send(new GameCommand(GameCommandKind.TutorialComplete));bus.Send(new GameCommand(GameCommandKind.TutorialComplete));
                Assert.That(completed,Is.EqualTo(1));Assert.That(host.IsTutorial,Is.False);Assert.That(host.Controller,Is.SameAs(run));Assert.That(run.ToJson(),Is.EqualTo(before));
            }
        }
        [Test] public void ReplayDuringPracticeRestartsWelcomeAndPreservesTheOriginalSuspendedRun()
        {
            var data=Resources.Load<GameContent>("CursedDeck/Content");var run=new RunController(77,data.CreateCatalog());var bus=new EventBus();
            using(var host=new GameHost(run,bus,data.CreatePresentation(),new UiSettings()))
            {
                string before=run.ToJson();host.TutorialRequested+=()=>host.BeginTutorial(new RunController(FirstMatchGuide.Seed,data.CreateCatalog()));
                bus.Send(new GameCommand(GameCommandKind.StartTutorial));bus.Send(new GameCommand(GameCommandKind.TutorialNext));bus.Send(new GameCommand(GameCommandKind.Play,FirstMatchGuide.Slot));bus.Send(new GameCommand(GameCommandKind.Pause));
                bus.Send(new GameCommand(GameCommandKind.StartTutorial));Assert.That(host.Snapshot().TutorialStage,Is.EqualTo(TutorialStage.Welcome));Assert.That(host.Controller.CurrentRound.History.Count,Is.Zero);Assert.That(host.Menu,Is.EqualTo(MenuMode.Playing));
                bus.Send(new GameCommand(GameCommandKind.ReturnToTitle));Assert.That(host.Controller,Is.SameAs(run));Assert.That(run.ToJson(),Is.EqualTo(before));
            }
        }
        [Test] public void LeavingIncompletePracticeRestoresCheckpointAndNeverSignalsCompletion()
        {
            var data=Resources.Load<GameContent>("CursedDeck/Content");var run=new RunController(42,data.CreateCatalog());var bus=new EventBus();
            using(var host=new GameHost(run,bus,data.CreatePresentation(),new UiSettings()))
            {
                string before=run.ToJson();int signals=0;host.ServiceRequested+=c=>signals++;host.BeginTutorial(new RunController(FirstMatchGuide.Seed,data.CreateCatalog()));
                bus.Send(new GameCommand(GameCommandKind.ReturnToTitle));Assert.That(host.Controller,Is.SameAs(run));Assert.That(run.ToJson(),Is.EqualTo(before));Assert.That(signals,Is.Zero);Assert.That(host.Menu,Is.EqualTo(MenuMode.Title));
            }
        }
    }
}
