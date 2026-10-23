using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using CursedDeck.Data;
using CursedDeck.Run;
using CursedDeck.View;
namespace CursedDeck.ViewTests
{
    public sealed class PolishTests
    {
        [Test] public void OriginalAudioImportsAndPitchRisesWithChain(){var data=Resources.Load<AudioDefinition>("CursedDeck/Audio");Assert.That(data,Is.Not.Null);Assert.That(data.Ambient.length,Is.GreaterThanOrEqualTo(19));Assert.That(data.Card,Is.Not.Null);Assert.That(data.Combo,Is.Not.Null);Assert.That(AudioFeedback.ChainPitch(5),Is.GreaterThan(AudioFeedback.ChainPitch(4)));Assert.That(AudioFeedback.ChainPitch(99),Is.EqualTo(2).Within(.001));}
        [Test] public void TutorialRequiresAcceptedMatchAndDrawThenEmitsCompletionExactlyOnce()
        {
            var data=Resources.Load<GameContent>("CursedDeck/Content");var bus=new EventBus();var run=new RunController(FirstMatchGuide.Seed,data.CreateCatalog());var root=new GameObject("Polish",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(GameView));
            try
            {
                var view=root.GetComponent<GameView>();view.Initialize(bus,data.Font,data.CardFront,data.CardBack,data.SuitIcons);int complete=0;bus.CommandRequested+=c=>{if(c.Kind==GameCommandKind.TutorialComplete)complete++;};
                using(var host=new GameHost(run,bus,data.CreatePresentation(),new UiSettings()))
                {
                    host.TutorialRequested+=()=>host.BeginTutorial(run);host.Publish(GameFeedback.Deal);view.ShowTutorial();var panel=root.transform.Find("SafeArea/Tutorial");var next=panel.Find("Next").GetComponent<Button>();
                    Assert.That(view.TutorialStep,Is.Zero);Assert.That(complete,Is.Zero);next.onClick.Invoke();Assert.That(view.TutorialStep,Is.EqualTo(1));
                    bus.Send(new GameCommand(GameCommandKind.Play,FirstMatchGuide.Slot));Assert.That(view.TutorialStep,Is.EqualTo(2));Assert.That(run.CurrentRound.HasNoMoves);Assert.That(panel.gameObject.activeSelf);
                    next.onClick.Invoke();Assert.That(view.TutorialStep,Is.EqualTo(3));bus.Send(new GameCommand(GameCommandKind.Draw));Assert.That(view.TutorialStep,Is.EqualTo(4));
                    next.onClick.Invoke();next.onClick.Invoke();view.Tutorial.StepVisuals(.4f);Assert.That(panel.gameObject.activeSelf,Is.False);Assert.That(complete,Is.EqualTo(1));
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void WarmTweenLoopAllocatesZeroManagedBytes(){var go=new GameObject("TweenProbe",typeof(RectTransform),typeof(TweenRunner));try{var runner=go.GetComponent<TweenRunner>();var rect=(RectTransform)go.transform;for(int i=0;i<30;i++){runner.Shake(rect);runner.Step(.01f);}long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)runner.Step(.001f);long bytes=System.GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(bytes,Is.EqualTo(0),"Only the project's fixed-slot tween loop is measured; this does not certify the whole engine or Android frame rate.");}finally{Object.DestroyImmediate(go);}}
    }
}
