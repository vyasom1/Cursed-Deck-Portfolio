#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CursedDeck.Bootstrap;
using CursedDeck.Run;
namespace CursedDeck.PlayTests
{
    public sealed class PlayableTouchTests
    {
        private string directory;
        private InputSettings originalSettings;
        private Touchscreen touch;
        private GameBootstrap bootstrap;
        private UnityEditor.EditorWindow gameView;
        private int oldSizeIndex;
        private void Portrait()
        {
            var asm=typeof(UnityEditor.Editor).Assembly;var sizesType=asm.GetType("UnityEditor.GameViewSizes");var single=typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=single.GetProperty("instance").GetValue(null);var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{UnityEditor.EditorUserBuildSettings.activeBuildTarget==UnityEditor.BuildTarget.Android?UnityEditor.GameViewSizeGroupType.Android:UnityEditor.GameViewSizeGroupType.Standalone});var sizeType=asm.GetType("UnityEditor.GameViewSize");var kind=asm.GetType("UnityEditor.GameViewSizeType");var size=System.Activator.CreateInstance(sizeType,new object[]{System.Enum.ToObject(kind,1),1080,1920,"Cursed Deck Test Portrait"});int index=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);group.GetType().GetMethod("AddCustomSize").Invoke(group,new object[]{size});gameView=UnityEditor.EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));var property=gameView.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);oldSizeIndex=(int)property.GetValue(gameView);property.SetValue(gameView,index);gameView.Show();gameView.Focus();gameView.Repaint();
        }
        private void Loaded(Scene scene,LoadSceneMode mode){if(scene.name!="CursedDeck")return;bootstrap=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();bootstrap.StorageDirectory=directory;}
        private IEnumerator Tap(Button button)
        {
            while(bootstrap!=null&&bootstrap.View.Board.IsDealing)yield return null;
            Assert.That(button.interactable);var rect=(RectTransform)button.transform;var position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            var pointer=new PointerEventData(EventSystem.current){position=position};var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.That(hits.Count,Is.GreaterThan(0));Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button),"Top hit "+hits[0].gameObject.name+" at "+position+" screen "+Screen.width+"x"+Screen.height);
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=position});yield return null;yield return null;
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=position});yield return null;yield return null;
        }
        private IEnumerator CaptureTutorial(string step)
        {
            string folder=Environment.GetEnvironmentVariable("CURSED_DECK_FTUE_CAPTURE_DIR");
            if(!string.IsNullOrEmpty(folder)&&NUnit.Framework.TestContext.CurrentContext.Test.Name=="PortraitTouchDrivesTutorialFullRunDailyShareAndResume")
            {Directory.CreateDirectory(folder);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(folder,step+".png"));yield return null;yield return null;}
        }
        private IEnumerator CompleteFirstMatchGuide()
        {
            var panel=bootstrap.View.SafeRoot.Find("Tutorial");Assert.That(panel.gameObject.activeSelf);Assert.That(bootstrap.View.TutorialStep,Is.Zero);
            Assert.That(bootstrap.Host.IsTutorial);Assert.That(File.Exists(bootstrap.SavePath),Is.False,"Practice must not create a regular checkpoint before entering standard play.");
            yield return CaptureTutorial("01-welcome");
            yield return Tap(bootstrap.View.Tutorial.NextButton);Assert.That(bootstrap.View.TutorialStep,Is.EqualTo(1));
            var pointerStart=bootstrap.View.Tutorial.Hand.anchoredPosition;for(int frame=0;frame<5;frame++)yield return null;Assert.That(bootstrap.View.Tutorial.Hand.anchoredPosition,Is.Not.EqualTo(pointerStart));
            int wrong=bootstrap.View.LastSnapshot.TutorialSlot==18?17:18;var wrongCard=bootstrap.View.Board.CardAt(wrong);var wrongPoint=RectTransformUtility.WorldToScreenPoint(null,wrongCard.Rect.TransformPoint(wrongCard.Rect.rect.center));
            string unchanged=bootstrap.Host.Controller.ToJson();InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=wrongPoint});yield return null;yield return null;InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=wrongPoint});yield return null;yield return null;
            Assert.That(bootstrap.Host.Controller.ToJson(),Is.EqualTo(unchanged),"A real touch on another card must be blocked.");yield return CaptureTutorial("02-match");
            yield return Tap(bootstrap.View.Board.CardAt(bootstrap.View.LastSnapshot.TutorialSlot).Button);Assert.That(bootstrap.View.TutorialStep,Is.EqualTo(2));Assert.That(bootstrap.View.LastSnapshot.HasNoMoves);yield return CaptureTutorial("03-chain");
            yield return Tap(bootstrap.View.Tutorial.NextButton);Assert.That(bootstrap.View.TutorialStep,Is.EqualTo(3));yield return CaptureTutorial("04-draw");
            yield return Tap(bootstrap.View.DrawButton);Assert.That(bootstrap.View.TutorialStep,Is.EqualTo(4));yield return CaptureTutorial("05-complete");
            yield return Tap(bootstrap.View.Tutorial.NextButton);yield return new WaitForSecondsRealtime(.4f);Assert.That(bootstrap.Profile.TutorialDone);Assert.That(CursedDeck.Meta.GameSettings.Instance.hasCompletedTutorial);
        }
        [UnityTest] public IEnumerator PortraitTouchDrivesTutorialFullRunDailyShareAndResume()
        {
            Portrait();yield return null;yield return null;
            directory=Path.Combine(Path.GetTempPath(),"CursedDeck-Touch-"+Guid.NewGuid().ToString("N"));originalSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(originalSettings);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
            touch=InputSystem.AddDevice<Touchscreen>();SceneManager.sceneLoaded+=Loaded;yield return SceneManager.LoadSceneAsync("CursedDeck");yield return null;yield return null;
            Assert.That(Screen.width,Is.EqualTo(1080));Assert.That(Screen.height,Is.EqualTo(1920));
            Assert.That(bootstrap.View,Is.Not.Null);Assert.That(bootstrap.View.Board.AllocatedCardCount,Is.EqualTo(28));
            var tutorial=bootstrap.View.transform.Find("SafeArea/Tutorial");yield return CompleteFirstMatchGuide();
            Assert.That(tutorial.gameObject.activeSelf,Is.False);Assert.That(bootstrap.Profile.TutorialDone);
            Assert.That(bootstrap.View.LastSnapshot.Cards.Count,Is.EqualTo(19));yield return new WaitForSecondsRealtime(.5f);yield return CaptureTutorial("06-two-peaks-board");
            int inspectionSlot=-1;foreach(var c in bootstrap.View.LastSnapshot.Cards)if(c.Visible){inspectionSlot=c.Slot;break;}
            string preInspect=bootstrap.Host.Controller.ToJson();var inspected=bootstrap.View.Board.CardAt(inspectionSlot);
            var inspectPoint=RectTransformUtility.WorldToScreenPoint(null,inspected.Rect.TransformPoint(inspected.Rect.rect.center));
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=inspectPoint});yield return new WaitForSecondsRealtime(.65f);
            Assert.That(bootstrap.View.CardInspectionVisible);InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=inspectPoint});yield return null;yield return null;
            Assert.That(bootstrap.Host.Controller.ToJson(),Is.EqualTo(preInspect),"Inspecting must never play a card.");yield return CaptureTutorial("07-full-art-inspection");
            yield return Tap(bootstrap.View.SafeRoot.Find("CardInspection/Close").GetComponent<Button>());Assert.That(bootstrap.View.CardInspectionVisible,Is.False);
            var current=(RectTransform)bootstrap.View.SafeRoot.Find("BottomControlPanel/CurrentCardSlot");var currentPoint=RectTransformUtility.WorldToScreenPoint(null,current.TransformPoint(current.rect.center));
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=currentPoint});yield return new WaitForSecondsRealtime(.65f);
            Assert.That(bootstrap.View.CardInspectionVisible,"Current-card inspection must be enabled again after title/practice transitions.");
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=currentPoint});yield return null;yield return null;
            Assert.That(bootstrap.Host.Controller.ToJson(),Is.EqualTo(preInspect));yield return Tap(bootstrap.View.SafeRoot.Find("CardInspection/Close").GetComponent<Button>());
            int slot=-1;foreach(var c in bootstrap.View.LastSnapshot.Cards)if(c.Playable){slot=c.Slot;break;}Assert.That(slot,Is.GreaterThanOrEqualTo(0));yield return Tap(bootstrap.View.Board.CardAt(slot).Button);
            Assert.That(bootstrap.View.LastSnapshot.Score,Is.GreaterThan(0));Assert.That(bootstrap.View.LastSnapshot.Chain,Is.EqualTo(1));yield return Tap(bootstrap.View.DrawButton);Assert.That(bootstrap.View.LastSnapshot.Draws,Is.EqualTo(22));Assert.That(bootstrap.View.LastSnapshot.Chain,Is.EqualTo(0));
            string saved=File.ReadAllText(bootstrap.SavePath);Assert.That(RunController.FromJson(saved).ToJson(),Is.EqualTo(bootstrap.Host.Controller.ToJson()));Assert.That(bootstrap.View.Board.AllocatedCardCount,Is.EqualTo(28));
            // Finish a deterministic witness via ordinary UI pointer events, then begin the real daily menu action.
            bootstrap.Host.Controller.DebugRestart(1,1);bootstrap.Host.Publish(GameFeedback.Deal);
            foreach(int solution in bootstrap.Host.Controller.Deal.SolutionSlots){if(bootstrap.Host.Controller.State.Stage!=RunStage.Playing)break;yield return Tap(bootstrap.View.Board.CardAt(solution).Button);}
            Assert.That(bootstrap.View.ResultTitle,Is.EqualTo("ROUND WON"));Assert.That(bootstrap.Host.Controller.State.Stage,Is.EqualTo(RunStage.Reward));yield return null;
            while(bootstrap.Host.Controller.State.Stage!=RunStage.Won)
            {
                if(bootstrap.Host.Controller.State.Stage==RunStage.Reward)yield return Tap(bootstrap.Host.Controller.Reward.Options.Count>0?bootstrap.View.OfferButton(0):bootstrap.View.ContinueButton);
                else if(bootstrap.Host.Controller.State.Stage==RunStage.Shop)yield return Tap(bootstrap.View.ContinueButton);
                else foreach(int solution in bootstrap.Host.Controller.Deal.SolutionSlots){if(bootstrap.Host.Controller.State.Stage!=RunStage.Playing)break;yield return Tap(bootstrap.View.Board.CardAt(solution).Button);}
            }
            Assert.That(bootstrap.Profile.Wins,Is.EqualTo(1));Assert.That(bootstrap.Profile.SoulShards,Is.EqualTo(67));yield return null;
            bootstrap.View.Events.Send(new GameCommand(GameCommandKind.MetaDaily));yield return null;
            Assert.That(bootstrap.Host.Controller.State.Seed,Is.EqualTo(CursedDeck.Meta.DailyRun.Seed(DateTime.UtcNow)));
            bootstrap.View.Events.Send(new GameCommand(GameCommandKind.MetaShare));yield return null;
            var menu=bootstrap.View.StatsButton;yield return Tap(menu);
            Assert.That(GUIUtility.systemCopyBuffer,Does.StartWith("Cursed Deck Daily "+CursedDeck.Meta.DailyRun.DateKey(DateTime.UtcNow)));
            Assert.That(bootstrap.View.Board.AllocatedCardCount,Is.EqualTo(28));
            Assert.That(RunController.FromJson(File.ReadAllText(bootstrap.SavePath)).ToJson(),Is.EqualTo(bootstrap.Host.Controller.ToJson()));
        }

        [UnityTest] public IEnumerator NativeTitleDailyStartsDirectlyWithoutClassicSubmenu()
        {
            Portrait();yield return null;yield return null;directory=Path.Combine(Path.GetTempPath(),"CursedDeck-DailyTitle-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);new CursedDeck.Meta.MetaStore(Path.Combine(directory,"meta-v1.json")).Save(new CursedDeck.Meta.MetaProfile{TutorialDone=true});originalSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(originalSettings);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
            touch=InputSystem.AddDevice<Touchscreen>();SceneManager.sceneLoaded+=Loaded;yield return SceneManager.LoadSceneAsync("CursedDeck");yield return null;yield return null;
            Assert.That(bootstrap.View.StartMenuVisible);yield return Tap(bootstrap.View.StartDailyChallengeButton);Assert.That(bootstrap.View.StartMenuVisible,Is.False);Assert.That(bootstrap.Profile.Active.DailyDate,Is.EqualTo(CursedDeck.Meta.DailyRun.DateKey(DateTime.UtcNow)));Assert.That(bootstrap.Host.Controller.State.Seed,Is.EqualTo(CursedDeck.Meta.DailyRun.Seed(DateTime.UtcNow)));Assert.That(bootstrap.View.SafeRoot.Find("Tutorial").gameObject.activeSelf,Is.False);
        }
        [UnityTest] public IEnumerator NativeOwnedRelicBadgeShowsIconBuffAndReturnsToUnchangedHand()
        {
            Portrait();yield return null;yield return null;directory=Path.Combine(Path.GetTempPath(),"CursedDeck-OwnedTouch-"+Guid.NewGuid().ToString("N"));originalSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(originalSettings);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
            touch=InputSystem.AddDevice<Touchscreen>();SceneManager.sceneLoaded+=Loaded;yield return SceneManager.LoadSceneAsync("CursedDeck");yield return null;yield return null;yield return CompleteFirstMatchGuide();
            bootstrap.Host.Controller.DebugRestart(1,1);bootstrap.Host.Publish(GameFeedback.Deal);foreach(int slot in bootstrap.Host.Controller.Deal.SolutionSlots){if(bootstrap.Host.Controller.State.Stage!=RunStage.Playing)break;yield return Tap(bootstrap.View.Board.CardAt(slot).Button);}
            yield return Tap(bootstrap.View.OfferButton(0));string before=bootstrap.Host.Controller.ToJson();yield return Tap(bootstrap.View.OwnedRelicsButton);Assert.That(bootstrap.View.OwnedRelicsVisible);yield return Tap(bootstrap.View.OwnedRelicButton(0));Assert.That(bootstrap.View.RelicDetailVisible);Assert.That(bootstrap.View.SafeRoot.Find("RelicDetail/Icon").GetComponent<Image>().sprite,Is.Not.Null);yield return Tap(bootstrap.View.SafeRoot.Find("RelicDetail/Back").GetComponent<Button>());yield return Tap(bootstrap.View.SafeRoot.Find("OwnedRelics/Close").GetComponent<Button>());Assert.That(bootstrap.View.OwnedRelicsVisible,Is.False);Assert.That(bootstrap.Host.Controller.ToJson(),Is.EqualTo(before));
        }
        [UnityTest] public IEnumerator NativeTouchTiltsWithoutPlayingOnDragAndExposedCardsFlip()
        {
            Portrait();yield return null;yield return null;
            directory=Path.Combine(Path.GetTempPath(),"CursedDeck-TCG-Touch-"+Guid.NewGuid().ToString("N"));originalSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(originalSettings);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
            touch=InputSystem.AddDevice<Touchscreen>();SceneManager.sceneLoaded+=Loaded;yield return SceneManager.LoadSceneAsync("CursedDeck");yield return null;yield return null;
            var tutorial=bootstrap.View.transform.Find("SafeArea/Tutorial");yield return CompleteFirstMatchGuide();
            bootstrap.Host.Controller.DebugRestart(1,1);bootstrap.Host.Publish(GameFeedback.Deal);yield return null;int slot=-1;foreach(var c in bootstrap.View.LastSnapshot.Cards)if(c.Playable){slot=c.Slot;break;}Assert.That(slot,Is.GreaterThanOrEqualTo(0));var card=bootstrap.View.Board.CardAt(slot);var rect=card.Rect;var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector2(rect.rect.width*.25f,rect.rect.height*.25f)));
            int history=bootstrap.Host.Controller.CurrentRound.History.Count;
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});for(int i=0;i<8;i++)yield return null;
            Assert.That(Quaternion.Angle(card.FX.CurrentTilt,Quaternion.identity),Is.GreaterThan(1));Assert.That(card.FX.GlowOpacity,Is.GreaterThan(0));
            point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(new Vector2(-rect.rect.width*.25f,-rect.rect.height*.25f)));InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Moved,position=point});yield return null;yield return null;
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});yield return new WaitForSecondsRealtime(.6f);yield return null;
            Assert.That(bootstrap.Host.Controller.CurrentRound.History.Count,Is.EqualTo(history),"Dragging tilts; it must not also play a card.");Assert.That(card.FX.GlowOpacity,Is.Zero);
            CursedDeck.View.CardView revealed=null;
            foreach(int nextSlot in bootstrap.Host.Controller.Deal.SolutionSlots)
            {
                var before=new bool[28];foreach(var c in bootstrap.View.LastSnapshot.Cards)before[c.Slot]=c.Visible;
                yield return Tap(bootstrap.View.Board.CardAt(nextSlot).Button);
                foreach(var c in bootstrap.View.LastSnapshot.Cards)if(c.Visible&&!c.Removed&&!before[c.Slot]){revealed=bootstrap.View.Board.CardAt(c.Slot);break;}
                if(revealed!=null)break;
            }
            Assert.That(revealed,Is.Not.Null);Assert.That(revealed.FX.TargetFront);yield return new WaitForSecondsRealtime(.5f);Assert.That(revealed.FX.FaceFront);Assert.That(revealed.FX.IsFlipping,Is.False);Assert.That(revealed.RenderedPortrait,Is.Not.Null);Assert.That(revealed.DisplayedName,Is.Not.Empty);Assert.That(bootstrap.View.Board.AllocatedCardCount,Is.EqualTo(28));
        }

        [UnityTest]public IEnumerator NativeDrawSpriteStatesAndHeaderMenusRemainInteractive()
        {
            Portrait();yield return null;yield return null;
            directory=Path.Combine(Path.GetTempPath(),"CursedDeck-UI-Touch-"+Guid.NewGuid().ToString("N"));originalSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(originalSettings);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
            touch=InputSystem.AddDevice<Touchscreen>();SceneManager.sceneLoaded+=Loaded;yield return SceneManager.LoadSceneAsync("CursedDeck");yield return null;yield return null;
            var tutorial=bootstrap.View.transform.Find("SafeArea/Tutorial");yield return CompleteFirstMatchGuide();
            var button=bootstrap.View.DrawButton;var image=(Image)button.targetGraphic;var skin=CursedDeck.View.UIPackSkin.Load();var size=((RectTransform)button.transform).rect.size;
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerEnterHandler);yield return null;Assert.That(image.overrideSprite,Is.SameAs(skin.DrawHover));
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);yield return null;Assert.That(image.overrideSprite,Is.SameAs(skin.DrawHover));
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerExitHandler);EventSystem.current.SetSelectedGameObject(null);yield return null;Assert.That(image.overrideSprite,Is.SameAs(skin.DrawDefault));
            button.interactable=false;yield return null;Assert.That(image.overrideSprite,Is.SameAs(skin.DrawDisabled));Assert.That(((RectTransform)button.transform).rect.size,Is.EqualTo(size));button.interactable=true;yield return null;
            yield return Tap(bootstrap.View.StatsButton);var meta=bootstrap.View.transform.Find("SafeArea/MetaPanel");Assert.That(meta.gameObject.activeSelf);yield return Tap(meta.Find("Close").GetComponent<Button>());
            var gear=bootstrap.View.transform.Find("SafeArea/HeaderBar/SettingsButton").GetComponent<Button>();yield return Tap(gear);Assert.That(bootstrap.View.PauseMenuVisible);yield return Tap(bootstrap.View.SafeRoot.Find("PauseMenu/Frame/Content/Settings").GetComponent<Button>());var panel=bootstrap.View.transform.Find("SafeArea/SettingsPanel");Assert.That(panel.gameObject.activeSelf);yield return Tap(panel.Find("Close").GetComponent<Button>());Assert.That(panel.gameObject.activeSelf,Is.False);yield return Tap(bootstrap.View.SafeRoot.Find("PauseMenu/Frame/Content/Resume").GetComponent<Button>());
            bootstrap.Host.Controller.DebugRestart(1,1);bootstrap.Host.Publish(GameFeedback.Deal);
            foreach(int slot in bootstrap.Host.Controller.Deal.SolutionSlots){if(bootstrap.Host.Controller.State.Stage!=RunStage.Playing)break;yield return Tap(bootstrap.View.Board.CardAt(slot).Button);}
            Assert.That(bootstrap.Host.Controller.State.Stage,Is.EqualTo(RunStage.Reward));Assert.That(bootstrap.Host.Controller.Reward.Options.Count,Is.GreaterThanOrEqualTo(3));string chosen=bootstrap.Host.Controller.Reward.Options[2].Id;yield return Tap(bootstrap.View.OfferButton(2));bool acquired=false;foreach(var relic in bootstrap.Host.Controller.State.Relics)if(relic.Id==chosen)acquired=true;Assert.That(acquired,"Third reward option must remain unobstructed and choose its own relic.");
        }

        [UnityTest]public IEnumerator NativeStartPauseSettingsTitleAndReloadContinuePreserveTheCheckpoint()
        {
            Portrait();yield return null;yield return null;
            directory=Path.Combine(Path.GetTempPath(),"CursedDeck-Menus-"+Guid.NewGuid().ToString("N"));originalSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(originalSettings);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
            touch=InputSystem.AddDevice<Touchscreen>();SceneManager.sceneLoaded+=Loaded;yield return SceneManager.LoadSceneAsync("CursedDeck");yield return null;yield return null;
            Assert.That(bootstrap.Host.IsTutorial);Assert.That(bootstrap.View.StartMenuVisible,Is.False);Assert.That(File.Exists(bootstrap.SavePath),Is.False);
            var tutorial=bootstrap.View.SafeRoot.Find("Tutorial");yield return CompleteFirstMatchGuide();
            yield return Tap(bootstrap.View.DrawButton);string before=bootstrap.Host.Controller.ToJson();
            yield return Tap(bootstrap.View.SafeRoot.Find("HeaderBar/SettingsButton").GetComponent<Button>());yield return Tap(bootstrap.View.SafeRoot.Find("PauseMenu/Frame/Content/Settings").GetComponent<Button>());
            yield return Tap(bootstrap.View.SettingsReplayTutorialButton);Assert.That(bootstrap.Host.IsTutorial);Assert.That(CursedDeck.Meta.GameSettings.Instance.hasCompletedTutorial,Is.False);
            string savedPractice=File.ReadAllText(bootstrap.SavePath);Assert.That(RunController.FromJson(savedPractice).ToJson(),Is.EqualTo(before));
            yield return Tap(bootstrap.View.Tutorial.NextButton);yield return Tap(bootstrap.View.Board.CardAt(bootstrap.View.LastSnapshot.TutorialSlot).Button);yield return Tap(bootstrap.View.Tutorial.NextButton);yield return Tap(bootstrap.View.DrawButton);yield return Tap(bootstrap.View.Tutorial.NextButton);yield return new WaitForSecondsRealtime(.4f);
            Assert.That(bootstrap.Host.Controller.ToJson(),Is.EqualTo(before));Assert.That(CursedDeck.Meta.MetaStore.FromJson(File.ReadAllText(Path.Combine(directory,"meta-v1.json"))).Settings.hasCompletedTutorial);

            yield return Tap(bootstrap.View.SafeRoot.Find("HeaderBar/SettingsButton").GetComponent<Button>());Assert.That(bootstrap.View.PauseMenuVisible);
            yield return Tap(bootstrap.View.SafeRoot.Find("PauseMenu/Frame/Content/Settings").GetComponent<Button>());yield return Tap(bootstrap.View.SafeRoot.Find("SettingsPanel/Close").GetComponent<Button>());Assert.That(bootstrap.View.PauseMenuVisible);
            yield return Tap(bootstrap.View.SafeRoot.Find("PauseMenu/Frame/Content/Resume").GetComponent<Button>());Assert.That(bootstrap.View.PauseMenuVisible,Is.False);Assert.That(bootstrap.Host.Controller.ToJson(),Is.EqualTo(before));
            yield return Tap(bootstrap.View.SafeRoot.Find("HeaderBar/SettingsButton").GetComponent<Button>());yield return Tap(bootstrap.View.SafeRoot.Find("PauseMenu/Frame/Content/MainMenu").GetComponent<Button>());Assert.That(bootstrap.View.StartMenuVisible);
            yield return Tap(bootstrap.View.StartNewRunButton);yield return Tap(bootstrap.View.SafeRoot.Find("NewRunConfirmation/Frame/Cancel").GetComponent<Button>());Assert.That(bootstrap.Host.Controller.ToJson(),Is.EqualTo(before));
            Assert.That(RunController.FromJson(File.ReadAllText(bootstrap.SavePath)).ToJson(),Is.EqualTo(before));
            yield return SceneManager.LoadSceneAsync("CursedDeck");yield return null;yield return null;Assert.That(bootstrap.View.StartMenuVisible);Assert.That(bootstrap.View.ResumeRunButton.interactable);
            yield return Tap(bootstrap.View.ResumeRunButton);Assert.That(bootstrap.View.StartMenuVisible,Is.False);Assert.That(bootstrap.Host.Controller.ToJson(),Is.EqualTo(before));Assert.That(bootstrap.View.SafeRoot.Find("Tutorial").gameObject.activeSelf,Is.False);
            yield return Tap(bootstrap.View.SafeRoot.Find("HeaderBar/SettingsButton").GetComponent<Button>());yield return Tap(bootstrap.View.SafeRoot.Find("PauseMenu/Frame/Content/MainMenu").GetComponent<Button>());yield return Tap(bootstrap.View.StartNewRunButton);yield return Tap(bootstrap.View.SafeRoot.Find("NewRunConfirmation/Frame/Confirm").GetComponent<Button>());
            Assert.That(bootstrap.View.StartMenuVisible,Is.False);Assert.That(bootstrap.Host.Controller.CurrentRound.History.Count,Is.Zero);Assert.That(bootstrap.View.LastSnapshot.Draws,Is.EqualTo(23));
        }

        private sealed class TouchHaptics:CursedDeck.Services.IHapticOutput
        {public int LightCount,HeavyCount;public void Light(){LightCount++;}public void Heavy(){HeavyCount++;}public void Dispose(){}}
        [UnityTest] public IEnumerator NativeAcceptedPlaysDriveBoundedParticlesAndRespectHapticSetting()
        {
            Portrait();yield return null;yield return null;
            directory=Path.Combine(Path.GetTempPath(),"CursedDeck-FullCards-"+Guid.NewGuid().ToString("N"));originalSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(originalSettings);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
            touch=InputSystem.AddDevice<Touchscreen>();SceneManager.sceneLoaded+=Loaded;yield return SceneManager.LoadSceneAsync("CursedDeck");yield return null;yield return null;
            var tutorial=bootstrap.View.transform.Find("SafeArea/Tutorial");yield return CompleteFirstMatchGuide();
            bootstrap.Host.Controller.DebugRestart(1,1);bootstrap.Host.Publish(GameFeedback.Deal);yield return null;var haptics=new TouchHaptics();CursedDeck.Services.HapticService.SetOutputForTests(haptics);bootstrap.Host.UpdateSettings(new UiSettings(true,true));
            var fx=bootstrap.View.Particles;int played=0;
            foreach(int slot in bootstrap.Host.Controller.Deal.SolutionSlots)
            {
                var card=bootstrap.View.Board.CardAt(slot);int rank=(int)bootstrap.Host.Controller.CurrentRound.GetCard(slot).Rank;int bursts=fx.TotalBurstsPlayed,heavy=haptics.HeavyCount,light=haptics.LightCount;
                yield return Tap(card.Button);played++;bool expected=CursedDeck.View.CardParticleFX.ShouldBurst(rank,bootstrap.View.LastSnapshot.Chain);Assert.That(fx.TotalBurstsPlayed-bursts,Is.EqualTo(expected?1:0));Assert.That(haptics.HeavyCount-heavy,Is.Zero);Assert.That(haptics.LightCount-light,Is.EqualTo(1));Assert.That(card.FX.TargetFront);if(played>=5)break;
            }
            Assert.That(haptics.LightCount,Is.GreaterThan(0));Assert.That(fx.TotalBurstsPlayed,Is.GreaterThan(0));Assert.That(fx.LiveParticleCount,Is.GreaterThan(0));Assert.That(fx.AllocatedBurstCount,Is.EqualTo(12));Assert.That(fx.LiveParticleCount,Is.LessThanOrEqualTo(336));
            bootstrap.Host.UpdateSettings(new UiSettings(true,false));int count=haptics.LightCount+haptics.HeavyCount;foreach(int slot in bootstrap.Host.Controller.Deal.SolutionSlots)if(!bootstrap.Host.Controller.CurrentRound.IsRemoved(slot)){yield return Tap(bootstrap.View.Board.CardAt(slot).Button);break;}
            yield return new WaitForSecondsRealtime(1);Assert.That(haptics.LightCount+haptics.HeavyCount,Is.EqualTo(count));Assert.That(fx.ActiveBurstCount,Is.Zero);Assert.That(bootstrap.View.Board.AllocatedCardCount,Is.EqualTo(28));
        }


        private IEnumerator CapturePhase2(string name)
        {
            string folder=Environment.GetEnvironmentVariable("CURSED_DECK_PHASE2_CAPTURE_DIR");if(string.IsNullOrEmpty(folder))yield break;
            Directory.CreateDirectory(folder);Canvas.ForceUpdateCanvases();ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return null;yield return null;
        }
        [UnityTest] public IEnumerator NativeReduceMotionPersistsAcrossReloadAndAcceptedFeedbackRemainsPlayable()
        {
            Portrait();yield return null;yield return null;
            directory=Path.Combine(Path.GetTempPath(),"CursedDeck-Motion-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            new CursedDeck.Meta.MetaStore(Path.Combine(directory,"meta-v1.json")).Save(new CursedDeck.Meta.MetaProfile{TutorialDone=true,Settings=new CursedDeck.Meta.GameSettings{hasCompletedTutorial=true,Sound=false,Haptics=false}});
            originalSettings=InputSystem.settings;var settings=UnityEngine.Object.Instantiate(originalSettings);settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;InputSystem.settings=settings;
            touch=InputSystem.AddDevice<Touchscreen>();SceneManager.sceneLoaded+=Loaded;yield return SceneManager.LoadSceneAsync("CursedDeck");yield return null;yield return null;
            yield return Tap(bootstrap.View.StartDailyChallengeButton);while(bootstrap.View.Board.IsDealing)yield return null;yield return new WaitForSecondsRealtime(.5f);
            Assert.That(bootstrap.View.AmbientMist.gameObject.activeSelf);yield return CapturePhase2("01-ambient-board-editor");
            int slot=bootstrap.Host.Controller.Deal.SolutionSlots[0];yield return Tap(bootstrap.View.Board.CardAt(slot).Button);Assert.That(bootstrap.View.ScorePopups.ActiveCount,Is.GreaterThan(0));Assert.That(bootstrap.View.Particles.TotalBurstsPlayed,Is.EqualTo(1));yield return CapturePhase2("02-card-flight-editor");
            yield return new WaitForSecondsRealtime(.7f);Assert.That(bootstrap.View.DisplayedScore,Is.EqualTo((float)bootstrap.View.LastSnapshot.Score));
            yield return Tap(bootstrap.View.SafeRoot.Find("HeaderBar/SettingsButton").GetComponent<Button>());yield return Tap(bootstrap.View.SafeRoot.Find("PauseMenu/Frame/Content/Settings").GetComponent<Button>());
            string checkpoint=bootstrap.Host.Controller.ToJson();yield return Tap(bootstrap.View.SafeRoot.Find("SettingsPanel/Setting-4").GetComponent<Button>());
            Assert.That(bootstrap.View.LastSnapshot.Settings.ReduceMotion);Assert.That(bootstrap.View.AmbientMist.gameObject.activeSelf,Is.False);Assert.That(bootstrap.Host.Controller.ToJson(),Is.EqualTo(checkpoint));
            Assert.That(CursedDeck.Meta.MetaStore.FromJson(File.ReadAllText(Path.Combine(directory,"meta-v1.json"))).Settings.reduceMotion);yield return CapturePhase2("03-reduce-motion-settings-editor");
            yield return Tap(bootstrap.View.SafeRoot.Find("SettingsPanel/Close").GetComponent<Button>());yield return Tap(bootstrap.View.SafeRoot.Find("PauseMenu/Frame/Content/Resume").GetComponent<Button>());
            var card=bootstrap.View.Board.CardAt(bootstrap.Host.Controller.Deal.SolutionSlots[1]);var point=RectTransformUtility.WorldToScreenPoint(null,card.Rect.TransformPoint(new Vector2(card.Rect.rect.width*.25f,card.Rect.rect.height*.25f)));
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Began,position=point});yield return null;yield return null;Assert.That(card.FX.CurrentTilt,Is.EqualTo(Quaternion.identity));
            InputSystem.QueueStateEvent(touch,new TouchState{touchId=1,phase=UnityEngine.InputSystem.TouchPhase.Ended,position=point});yield return null;yield return null;
            Assert.That(bootstrap.View.GetComponent<CursedDeck.View.TweenRunner>().ActiveFlightCount,Is.Zero);Assert.That(bootstrap.View.DisplayedScore,Is.EqualTo((float)bootstrap.View.LastSnapshot.Score));Assert.That(bootstrap.View.DrawButton.transform.localScale,Is.EqualTo(Vector3.one));
            yield return SceneManager.LoadSceneAsync("CursedDeck");yield return null;yield return null;Assert.That(bootstrap.View.LastSnapshot.Settings.ReduceMotion);Assert.That(bootstrap.View.AmbientMist.gameObject.activeSelf,Is.False);yield return Tap(bootstrap.View.ResumeRunButton);
            Assert.That(bootstrap.View.Board.IsDealing,Is.False);yield return Tap(bootstrap.View.DrawButton);Assert.That(bootstrap.View.LastSnapshot.Chain,Is.Zero);
            Debug.Log("CURSED_DECK_PHASE2_NATIVE_OK Touch=accepted Motion=disabled Persistence=reload Draw=accepted");
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            CursedDeck.Services.HapticService.Shutdown();CursedDeck.Services.HapticService.Enabled=true;SceneManager.sceneLoaded-=Loaded;if(bootstrap!=null)UnityEngine.Object.Destroy(bootstrap.gameObject);yield return null;
            var events=UnityEngine.Object.FindFirstObjectByType<EventSystem>();if(events!=null)UnityEngine.Object.Destroy(events.gameObject);yield return null;
            if(touch!=null&&touch.added)InputSystem.RemoveDevice(touch);if(originalSettings!=null)InputSystem.settings=originalSettings;
            if(directory!=null&&Directory.Exists(directory))Directory.Delete(directory,true);
            if(gameView!=null)gameView.GetType().GetProperty("selectedSizeIndex",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(gameView,oldSizeIndex);
        }
    }
}
#endif
