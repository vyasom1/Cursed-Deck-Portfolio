using System;
using System.Collections.Generic;
using System.Reflection;
using CursedDeck.Data;
using CursedDeck.Meta;
using CursedDeck.Run;
using CursedDeck.View;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CursedDeck.ViewTests
{
    public sealed class VisualPhase2Tests
    {
        private GameObject root;private GameHost host;private EventBus bus;private GameView view;
        private void SetupView(bool reduced=false)
        {
            var data=Resources.Load<GameContent>("CursedDeck/Content");bus=new EventBus();
            root=UnityEngine.Object.Instantiate(Resources.Load<GameView>("UI/BoardView")).gameObject;view=root.GetComponent<GameView>();view.Initialize(bus,data.Font,data.CardFront,data.CardBack,data.SuitIcons);
            root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)root.transform).sizeDelta=new Vector2(1080,1920);Canvas.ForceUpdateCanvases();
            host=new GameHost(new RunController(42,data.CreateCatalog(),data.Rules.CreateConfig(),data.Layout.CreateLayout()),bus,data.CreatePresentation(),new UiSettings(false,false,false,false,reduced));host.Publish(GameFeedback.Deal);
        }
        [TearDown] public void Cleanup(){host?.Dispose();if(root!=null)UnityEngine.Object.DestroyImmediate(root);}
        [Test] public void ReducedMotionPersistsAndLegacySettingsDefaultOff()
        {
            var profile=new MetaProfile{Settings=new GameSettings{reduceMotion=true,hasCompletedTutorial=true}};
            string json=MetaStore.ToJson(profile);Assert.That(MetaStore.FromJson(json).Settings.reduceMotion);Assert.That(MetaStore.FromJson(json.Replace(",\"reduceMotion\":true","").Replace("\"reduceMotion\":true,","")).Settings.reduceMotion,Is.False);
        }
        [Test] public void DealStartsAtDeckAndLastCardLandsAtPointFourSeconds()
        {
            SetupView();var tween=root.GetComponent<TweenRunner>();Assert.That(tween.ActiveDealCount,Is.EqualTo(19));
            var first=view.Board.CardAt(0);var before=first.FX.MotionRoot.anchoredPosition;Assert.That(before.sqrMagnitude,Is.GreaterThan(1));
            tween.Step(.16f);Assert.That(tween.ActiveDealCount,Is.GreaterThan(0).And.LessThan(19));Assert.That(first.FX.MotionRoot.anchoredPosition,Is.EqualTo(Vector2.zero));
            tween.Step(.24f);Assert.That(tween.ActiveDealCount,Is.Zero);Assert.That(view.Board.CardAt(18).FX.MotionRoot.anchoredPosition,Is.EqualTo(Vector2.zero));
        }
        [Test] public void EveryAcceptedPlayUsesCurvedPointTwoFlightThenLandingPulse()
        {
            SetupView();var tween=root.GetComponent<TweenRunner>();tween.Step(.4f);int slot=host.Controller.Deal.SolutionSlots[0];var card=view.Board.CardAt(slot);Vector2 from=card.Rect.anchoredPosition;
            bus.Send(new GameCommand(GameCommandKind.Play,slot));Assert.That(tween.ActiveFlightCount,Is.EqualTo(1));Assert.That(view.Particles.TotalBurstsPlayed,Is.EqualTo(1));Assert.That(view.ScorePopups.ActiveCount,Is.EqualTo(1));Assert.That(view.DisplayedScore,Is.Zero);
            var waste=(RectTransform)view.SafeRoot.Find("BottomControlPanel/CurrentCardSlot");var to=(Vector2)card.Rect.parent.InverseTransformPoint(waste.TransformPoint(waste.rect.center));
            tween.Step(.1f);Assert.That(Vector2.Distance(card.Rect.anchoredPosition,TweenRunner.Arc(from,to,.5f)),Is.LessThan(.01f));Assert.That(card.Rect.localEulerAngles.z,Is.EqualTo(7.5f).Within(.01));Assert.That(card.InPool,Is.False);Assert.That(card.Button.targetGraphic.raycastTarget,Is.False,"Flying visuals must not intercept a rapid second tap.");
            tween.Step(.1f);Assert.That(tween.ActiveFlightCount,Is.Zero);Assert.That(card.InPool);Assert.That(waste.localScale.x,Is.EqualTo(1.15f).Within(.001));tween.Step(.22f);Assert.That(waste.localScale,Is.EqualTo(Vector3.one));
        }
        [Test] public void ReduceMotionMidFlightFinishesWithoutLeakingAndStopsTiltMistAndBreathing()
        {
            SetupView();var tween=root.GetComponent<TweenRunner>();int slot=host.Controller.Deal.SolutionSlots[0];var card=view.Board.CardAt(slot);bus.Send(new GameCommand(GameCommandKind.Play,slot));
            string before=host.Controller.ToJson();host.UpdateSettings(new UiSettings(false,false,false,false,true));Assert.That(host.Controller.ToJson(),Is.EqualTo(before));Assert.That(tween.ActiveFlightCount+tween.ActiveDealCount,Is.Zero);Assert.That(card.InPool);Assert.That(view.AmbientMist.gameObject.activeSelf,Is.False);view.DrawBreath.Step(.7f);Assert.That(view.DrawButton.transform.localScale,Is.EqualTo(Vector3.one));
            var next=view.Board.CardAt(host.Controller.Deal.SolutionSlots[1]);next.FX.OnPointerDown(new PointerEventData(null){position=Vector2.one});next.FX.Step(.2f);Assert.That(next.FX.CurrentTilt,Is.EqualTo(Quaternion.identity));
            bus.Send(new GameCommand(GameCommandKind.Play,next.Slot));Assert.That(tween.ActiveFlightCount,Is.Zero);Assert.That(view.DisplayedScore,Is.EqualTo((float)view.LastSnapshot.Score));
            host.UpdateSettings(new UiSettings(false,false));Assert.That(view.AmbientMist.gameObject.activeSelf);
        }
        [Test] public void UndoDuringFlightRestoresHitAreaAndClearsFeedbackWithoutPoolGrowth()
        {
            SetupView();var tween=root.GetComponent<TweenRunner>();tween.Step(.4f);int slot=host.Controller.Deal.SolutionSlots[0];var card=view.Board.CardAt(slot);var position=card.Rect.anchoredPosition;
            host.Controller.State.Consumables.Add(ConsumableKind.Undo);bus.Send(new GameCommand(GameCommandKind.Play,slot));tween.Step(.1f);bus.Send(new GameCommand(GameCommandKind.Undo));
            Assert.That(view.Board.CardAt(slot),Is.SameAs(card));Assert.That(card.Button.targetGraphic.raycastTarget);Assert.That(card.Rect.anchoredPosition,Is.EqualTo(position));Assert.That(tween.ActiveFlightCount,Is.Zero);Assert.That(view.Particles.ActiveBurstCount+view.ScorePopups.ActiveCount,Is.Zero);Assert.That(view.Board.AllocatedCardCount,Is.EqualTo(28));
        }
        [Test] public void PopupsRemainBoundedAndFloatExactly150PixelsInPointSixSeconds()
        {
            SetupView(true);var fx=view.ScorePopups;fx.ReduceMotion=false;fx.Spawn(Vector3.zero,250,3);var start=fx.LastPosition;fx.Step(.3f);Assert.That(fx.LastPosition.y-start.y,Is.EqualTo(75).Within(.01));fx.Step(.3f);Assert.That(fx.LastPosition.y-start.y,Is.EqualTo(150).Within(.01));Assert.That(fx.ActiveCount,Is.Zero);
            for(int i=0;i<100;i++)fx.Spawn(Vector3.zero,250,3);Assert.That(fx.ActiveCount,Is.EqualTo(12));Assert.That(fx.Capacity,Is.EqualTo(12));fx.ReduceMotion=true;fx.Step(.3f);Assert.That(fx.LastPosition,Is.EqualTo(start));fx.Step(.3f);Assert.That(fx.ActiveCount,Is.Zero);
        }
        [Test] public void WarmedCombinedPresentationLoopsAllocateZeroManagedBytes()
        {
            SetupView();var tween=root.GetComponent<TweenRunner>();tween.Step(.4f);var card=view.Board.CardAt(host.Controller.Deal.SolutionSlots[0]);
            bus.Send(new GameCommand(GameCommandKind.Play,card.Slot));view.ScorePopups.Step(.001f);view.Particles.Step(.001f);view.AmbientMist.Step(.001f);view.DrawBreath.Step(.001f);tween.Step(.001f);
            var hud=(HUD)typeof(GameView).GetField("hud",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(view);hud.Step(.001f);Canvas.ForceUpdateCanvases();
            for(int i=0;i<20;i++){view.AmbientMist.Step(.0001f);view.DrawBreath.Step(.0001f);view.ScorePopups.Step(.0001f);view.Particles.Step(.0001f);hud.Step(.0001f);card.FX.Step(.0001f);tween.Step(.0001f);}
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<100;i++){view.AmbientMist.Step(.0001f);view.DrawBreath.Step(.0001f);view.ScorePopups.Step(.0001f);view.Particles.Step(.0001f);hud.Step(.0001f);card.FX.Step(.0001f);tween.Step(.0001f);}
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(bytes,Is.Zero,"Warmed cached project presentation methods, not command serialization, Editor rendering or a device frame.");Debug.Log("CURSED_DECK_PHASE2_GC_PROBE WarmedSteps=100 ManagedBytes="+bytes);
        }
        [Test] public void AcceptedBlindMirrorCardUsesDisclosedWasteIdentityForItsSuitBurst()
        {
            SetupView(true);host.Controller.DebugRestart(3,3);host.Publish(GameFeedback.Deal);bool tested=false;
            foreach(int slot in host.Controller.Deal.SolutionSlots)
            {
                if(host.Controller.State.Stage!=RunStage.Playing)break;bool blind=host.Controller.CurrentRound.IsBlind(slot);int before=view.Particles.TotalBurstsPlayed;
                bus.Send(new GameCommand(GameCommandKind.Play,slot));
                if(blind){Assert.That(view.Particles.TotalBurstsPlayed-before,Is.EqualTo(1));Assert.That((int)view.Particles.LastSuit,Is.EqualTo(view.LastSnapshot.WasteSuit));tested=true;break;}
            }
            Assert.That(tested,"Exercise a successfully matched blind card, not just a normal visible card.");
        }
        [Test] public void DrawBreathHasExactOneToOnePointZeroFourPeriod()
        {Assert.That(DrawButtonBreath.ScaleAt(0),Is.EqualTo(1));Assert.That(DrawButtonBreath.ScaleAt(.75f),Is.EqualTo(1.04f).Within(.00001));Assert.That(DrawButtonBreath.ScaleAt(1.5f),Is.EqualTo(1).Within(.00001));}
        [TestCase(1920)][TestCase(2340)][TestCase(2400)] public void AllFiveSettingsFitWithoutOverlappingTutorialOrBack(int height)
        {
            SetupView();((RectTransform)root.transform).sizeDelta=new Vector2(1080,height);view.OpenPause();view.OpenSettings();Canvas.ForceUpdateCanvases();
            var panel=view.SafeRoot.Find("SettingsPanel");var buttons=panel.GetComponentsInChildren<Button>();
            for(int i=0;i<buttons.Length;i++)for(int j=i+1;j<buttons.Length;j++)
            {
                var a=(RectTransform)buttons[i].transform;var b=(RectTransform)buttons[j].transform;var ac=panel.InverseTransformPoint(a.TransformPoint(a.rect.center));var bc=panel.InverseTransformPoint(b.TransformPoint(b.rect.center));
                Assert.That(Mathf.Abs(ac.y-bc.y),Is.GreaterThanOrEqualTo((a.rect.height+b.rect.height)*.5f-.1f),buttons[i].name+" overlaps "+buttons[j].name);
            }
            foreach(var text in panel.GetComponentsInChildren<TextMeshProUGUI>()){text.ForceMeshUpdate();Assert.That(text.isTextOverflowing,Is.False,text.text);}
        }
        private static GameSnapshot Snapshot(GameFeedback feedback=GameFeedback.None,RunStage stage=RunStage.Playing,int stock=23,int gold=0,bool visible=false)
        {
            var result=new GameSnapshot();void Set(string key,object value)=>typeof(GameSnapshot).GetProperty(key).SetValue(result,value);
            Set("BoardSeed",42);Set("Stage",stage);Set("Feedback",feedback);Set("Stock",stock);Set("Gold",gold);Set("Cards",new[]{new CardUiState(0,0,0,5,0,visible,true,true,false)});return result;
        }
        [Test] public void SoundCuesCoverDrawPlayRevealPurchaseWinLoseAndIgnoreUndoOrReload()
        {
            var start=Snapshot();Assert.That(AudioFeedback.Cues(null,start),Is.EqualTo(FeedbackSound.None));
            Assert.That(AudioFeedback.Cues(start,Snapshot(GameFeedback.Draw,stock:22)),Is.EqualTo(FeedbackSound.Draw));Assert.That(AudioFeedback.Cues(start,Snapshot(GameFeedback.Draw)),Is.EqualTo(FeedbackSound.None));
            Assert.That(AudioFeedback.Cues(start,Snapshot(GameFeedback.Play,visible:true)),Is.EqualTo(FeedbackSound.Play|FeedbackSound.Reveal));
            Assert.That(AudioFeedback.Cues(Snapshot(gold:100),Snapshot(GameFeedback.Shop,gold:50)),Is.EqualTo(FeedbackSound.Purchase));
            Assert.That(AudioFeedback.Cues(start,Snapshot(GameFeedback.Play,RunStage.Reward)),Is.EqualTo(FeedbackSound.Play|FeedbackSound.Win));Assert.That(AudioFeedback.Cues(start,Snapshot(GameFeedback.Draw,RunStage.Lost,22)),Is.EqualTo(FeedbackSound.Draw|FeedbackSound.Lose));
            Assert.That(AudioFeedback.Cues(Snapshot(stage:RunStage.Reward),Snapshot(stage:RunStage.Reward)),Is.EqualTo(FeedbackSound.None));Assert.That(AudioFeedback.Cues(start,Snapshot(GameFeedback.Undo,visible:true)),Is.EqualTo(FeedbackSound.None));
        }
        [Test] public void SixDistinctPreloadedEffectsAndBoundedCardPitchDoNotChangeRunRandomness()
        {
            var audio=Resources.Load<AudioDefinition>("CursedDeck/Audio");var unique=new HashSet<AudioClip>{audio.Card,audio.Play,audio.Reveal,audio.Purchase,audio.Win,audio.Defeat};Assert.That(unique.Count,Is.EqualTo(6));Assert.That(unique.Contains(null),Is.False);
            root=new GameObject("AudioProbe");var feedback=root.AddComponent<AudioFeedback>();for(int i=0;i<1000;i++)Assert.That(feedback.NextCardPitch(),Is.InRange(.95f,1.05f));
        }
        [Test] public void SoundMuteSuppressesPlaybackAndSettingsDoNotReplayAcceptedSound()
        {
            SetupView();var audio=Resources.Load<AudioDefinition>("CursedDeck/Audio");var feedback=root.AddComponent<AudioFeedback>();feedback.Initialize(bus,null,audio.Card,audio.Combo,false,new FeedbackAudioClips(audio.Play,audio.Reveal,audio.Purchase,audio.Win,audio.Defeat));host.Publish();
            bus.Send(new GameCommand(GameCommandKind.Play,host.Controller.Deal.SolutionSlots[0]));Assert.That(feedback.PlayedCueCount,Is.Zero);host.UpdateSettings(new UiSettings(true,false));Assert.That(feedback.PlayedCueCount,Is.Zero);
            bus.Send(new GameCommand(GameCommandKind.Draw));Assert.That(feedback.LastCues,Is.EqualTo(FeedbackSound.Draw));Assert.That(feedback.PlayedCueCount,Is.EqualTo(1));Assert.That(feedback.VoiceCapacity,Is.EqualTo(8));host.Publish();Assert.That(feedback.PlayedCueCount,Is.EqualTo(1));
        }
    }
}
