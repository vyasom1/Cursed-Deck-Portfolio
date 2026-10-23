using NUnit.Framework;
using CursedDeck.Meta;
using CursedDeck.Run;
using CursedDeck.Data;
using CursedDeck.View;
using UnityEngine;
namespace CursedDeck.ViewTests
{
    public sealed class GuidedTutorialViewTests
    {
        [Test] public void CompletionSettingsRoundTripInProfileAndLegacyFlagRemainsReadable()
        {
            var profile=new MetaProfile {Settings=new GameSettings{hasCompletedTutorial=true,Sound=false},TutorialDone=true};
            var loaded=MetaStore.FromJson(MetaStore.ToJson(profile));Assert.That(loaded.Settings.hasCompletedTutorial);Assert.That(loaded.Settings.Sound,Is.False);
            var legacy=MetaStore.FromJson(MetaStore.ToJson(new MetaProfile{TutorialDone=true}));Assert.That(legacy.TutorialDone);Assert.That(legacy.Settings,Is.Null);
        }
        [Test] public void AcceptedGuidedMatchFliesTowardWasteThenReturnsToPool()
        {
            var data=Resources.Load<GameContent>("CursedDeck/Content");var bus=new EventBus();var root=Object.Instantiate(Resources.Load<GameView>("UI/BoardView")).gameObject;
            try
            {
                var view=root.GetComponent<GameView>();view.Initialize(bus,data.Font,data.CardFront,data.CardBack,data.SuitIcons);
                using(var host=new GameHost(new RunController(42,data.CreateCatalog()),bus,data.CreatePresentation(),new UiSettings()))
                {
                    host.BeginTutorial(new RunController(FirstMatchGuide.Seed,data.CreateCatalog()));bus.Send(new GameCommand(GameCommandKind.TutorialNext));
                    var card=view.Board.CardAt(FirstMatchGuide.Slot);var initial=card.Rect.anchoredPosition;bus.Send(new GameCommand(GameCommandKind.Play,FirstMatchGuide.Slot));
                    var tween=root.GetComponent<TweenRunner>();tween.Step(.10f);Assert.That(card.Rect.anchoredPosition,Is.Not.EqualTo(initial));Assert.That(card.InPool,Is.False);
                    tween.Step(.10f);Assert.That(card.InPool);Assert.That(view.Board.AllocatedCardCount,Is.EqualTo(28));
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
        [TestCase(1920)][TestCase(2340)][TestCase(2400)] public void OverlayPrefabAndReplayMenusFitAndPointerMovesWithoutAllocating(int height)
        {
            var data=Resources.Load<GameContent>("CursedDeck/Content");var bus=new EventBus();var root=Object.Instantiate(Resources.Load<GameView>("UI/BoardView")).gameObject;
            try
            {
                var view=root.GetComponent<GameView>();view.Initialize(bus,data.Font,data.CardFront,data.CardBack,data.SuitIcons);root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)root.transform).sizeDelta=new Vector2(1080,height);
                using(var host=new GameHost(new RunController(42,data.CreateCatalog()),bus,data.CreatePresentation(),new UiSettings()))
                {
                    host.TutorialRequested+=()=>host.BeginTutorial(new RunController(FirstMatchGuide.Seed,data.CreateCatalog()));host.ShowStartMenu(false);view.ReplayTutorialButton.onClick.Invoke();Canvas.ForceUpdateCanvases();
                    Assert.That(view.Tutorial.gameObject.activeSelf);Assert.That(view.Tutorial.Hand.gameObject.activeSelf,Is.False);
                    Assert.That(view.SafeRoot.Find("Tutorial/Dim-0").GetComponent<UnityEngine.UI.Image>().color.a,Is.EqualTo(.8f));
                    view.Tutorial.NextButton.onClick.Invoke();Canvas.ForceUpdateCanvases();Assert.That(view.Tutorial.Hand.gameObject.activeSelf);Assert.That(view.Tutorial.Hand.GetComponent<CanvasRenderer>(),Is.Not.Null,"The hand must actually render, not only animate a Transform.");var start=view.Tutorial.Hand.anchoredPosition;
                    view.Tutorial.StepVisuals(.6f);Assert.That(view.Tutorial.Hand.anchoredPosition,Is.Not.EqualTo(start));
                    long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<300;i++)view.Tutorial.StepVisuals(.01f);Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero,"Only cached project-side hand/pulse loop measured.");
                    foreach(var label in view.Tutorial.GetComponentsInChildren<TMPro.TextMeshProUGUI>()){label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.text);}
                    view.OpenPause();view.OpenSettings();Assert.That(view.SettingsReplayTutorialButton.interactable);
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
