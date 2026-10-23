using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using CursedDeck.Data;
using CursedDeck.Meta;
using CursedDeck.Run;
using CursedDeck.View;

namespace CursedDeck.ViewTests
{
    public sealed class PlayableViewTests
    {
        private GameObject root;
        private GameHost host;
        private GameView view;
        private EventBus bus;
        private RunController controller;
        private GameContent content;
        [SetUp]
        public void SetUp()
        {
            content=Resources.Load<GameContent>("CursedDeck/Content"); Assert.That(content,Is.Not.Null);
            content.Validate(); bus=new EventBus();
            controller=new RunController(42,content.CreateCatalog(),content.Rules.CreateConfig(),content.Layout.CreateLayout());
            host=new GameHost(controller,bus,content.CreatePresentation(),new UiSettings(true,true));
            root=new GameObject("TestCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(GameView));
            view=root.GetComponent<GameView>(); view.Initialize(bus,content.Font,content.CardFront,content.CardBack,content.SuitIcons);
            root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace; ((RectTransform)root.transform).sizeDelta=new Vector2(1080,1920);
            view.SafeRoot.GetComponent<SafeAreaView>().Apply(new Rect(0,0,1080,1920),1080,1920);
            Canvas.ForceUpdateCanvases(); host.Publish(GameFeedback.Deal); Canvas.ForceUpdateCanvases();
        }
        [TearDown]
        public void TearDown() { host?.Dispose(); if(root!=null) UnityEngine.Object.DestroyImmediate(root); }
        private void WinViaCardButtons()
        {
            foreach(int slot in controller.Deal.SolutionSlots)
            {
                if(controller.State.Stage!=RunStage.Playing) break;
                var card=view.Board.CardAt(slot); Assert.That(card,Is.Not.Null); Assert.That(card.Button.interactable,Is.True);
                card.Button.onClick.Invoke();
            }
            Assert.That(controller.State.Stage,Is.EqualTo(RunStage.Reward));
        }

        [Test]
        public void RealCardButtonsDriveScoreChainAndWinThenRewardStartsNextDeal()
        {
            Assert.That(view.Board.AllocatedCardCount,Is.EqualTo(28)); Assert.That(view.Board.ActiveCardCount,Is.EqualTo(controller.Deal.Layout.Count));
            int first=controller.Deal.SolutionSlots[0]; view.Board.CardAt(first).Button.onClick.Invoke();
            Assert.That(view.LastSnapshot.Score,Is.EqualTo(100m)); Assert.That(view.LastSnapshot.Chain,Is.EqualTo(1));
            Assert.That(view.Board.ActiveCardCount,Is.EqualTo(controller.Deal.Layout.Count-1));
            foreach(int slot in controller.Deal.SolutionSlots)
                if(controller.State.Stage==RunStage.Playing && !controller.CurrentRound.IsRemoved(slot)) view.Board.CardAt(slot).Button.onClick.Invoke();
            Assert.That(view.ResultVisible,Is.True); Assert.That(view.ResultTitle,Is.EqualTo("ROUND WON"));
            view.OfferButton(0).onClick.Invoke();
            Assert.That(controller.State.Round,Is.EqualTo(2)); Assert.That(view.ResultVisible,Is.False);
            Assert.That(view.Board.AllocatedCardCount,Is.EqualTo(28)); Assert.That(view.Board.ActiveCardCount,Is.EqualTo(controller.Deal.Layout.Count));
        }

        [Test]
        public void DrawButtonUpdatesWasteDrawBudgetAndChainThroughTheBus()
        {
            int slot=controller.Deal.SolutionSlots[0]; view.Board.CardAt(slot).Button.onClick.Invoke();
            Assert.That(view.LastSnapshot.Chain,Is.EqualTo(1));
            var previousWaste=controller.CurrentRound.Waste;
            view.DrawButton.onClick.Invoke();
            Assert.That(view.LastSnapshot.Draws,Is.EqualTo(22)); Assert.That(view.LastSnapshot.Chain,Is.Zero);
            Assert.That(controller.CurrentRound.Waste,Is.Not.EqualTo(previousWaste));
            Assert.That(view.LastSnapshot.Stock,Is.EqualTo(50-controller.Deal.Layout.Count));
        }

        [Test]
        public void ImmediateUndoDuringPopReusesThePoppingCardWithoutInstantiation()
        {
            controller.State.Consumables.Add(ConsumableKind.Undo); host.Publish();
            int slot=controller.Deal.SolutionSlots[0]; var original=view.Board.CardAt(slot);
            original.Button.onClick.Invoke(); bus.Send(new GameCommand(GameCommandKind.Undo));
            Assert.That(view.Board.CardAt(slot),Is.SameAs(original));
            Assert.That(view.Board.ActiveCardCount,Is.EqualTo(controller.Deal.Layout.Count)); Assert.That(view.Board.AllocatedCardCount,Is.EqualTo(28));
            Assert.That(view.LastSnapshot.Score,Is.Zero); Assert.That(view.LastSnapshot.UndoCount,Is.Zero);
        }

        [Test]
        public void ButtonInteractivityMatchesValidatedMovesAndHiddenRanksAreNotSent()
        {
            foreach(var card in view.LastSnapshot.Cards)
            {
                Assert.That(view.Board.CardAt(card.Slot).Button.interactable,Is.EqualTo(card.Playable));
                if(!card.Visible) Assert.That(card.Rank,Is.Zero);
            }
            string before=controller.ToJson();
            int invalid=Enumerable.Range(0,controller.Deal.Layout.Count).First(i=>!controller.CurrentRound.CanPlay(i));
            view.Board.CardAt(invalid).Button.onClick.Invoke();
            Assert.That(controller.ToJson(),Is.EqualTo(before));
        }

        [Test]
        public void LeftHandedAndColorblindSettingsOnlyChangePresentation()
        {
            string before=controller.ToJson(); float original=((RectTransform)view.DrawButton.transform).anchorMin.x;
            host.UpdateSettings(new UiSettings(true,false,true,true));
            Assert.That(((RectTransform)view.DrawButton.transform).anchorMin.x,Is.LessThan(original));
            Assert.That(view.LastSnapshot.Settings.Colorblind,Is.True); Assert.That(controller.ToJson(),Is.EqualTo(before));
            var settings=new GameSettings(); Assert.That(settings.Sound,Is.True); Assert.That(settings.Haptics,Is.True);
        }

        [TestCase(1080,1920,0,80,1080,1760)]
        [TestCase(1080,2400,0,110,1080,2220)]
        [TestCase(750,1334,0,0,750,1334)]
        public void SafeAreaRespectsNotchesAndCardsRemainWithinBoard(int width,int height,int x,int y,int w,int h)
        {
            Rect safe=new Rect(x,y,w,h); Rect expected=SafeAreaView.Normalize(safe,width,height);
            var area=view.SafeRoot.GetComponent<SafeAreaView>(); area.Apply(safe,width,height); Canvas.ForceUpdateCanvases();
            Assert.That(view.SafeRoot.anchorMin.x,Is.EqualTo(expected.xMin).Within(.0001f));
            Assert.That(view.SafeRoot.anchorMax.y,Is.EqualTo(expected.yMax).Within(.0001f));
            var board=(RectTransform)view.Board.transform;
            var corners=new Vector3[4];
            for(int i=0;i<controller.Deal.Layout.Count;i++)
            {
                var card=view.Board.CardAt(i); card.Rect.GetWorldCorners(corners);
                foreach(var corner in corners)
                {
                    Vector3 local=board.InverseTransformPoint(corner);
                    Assert.That(local.x,Is.InRange(board.rect.xMin-.1f,board.rect.xMax+.1f));
                    Assert.That(local.y,Is.InRange(board.rect.yMin-.1f,board.rect.yMax+.1f));
                }
            }
        }

        [Test]
        public void TweenCompletionAndRepeatedDealsDoNotGrowCardPool()
        {
            WinViaCardButtons(); root.GetComponent<TweenRunner>().Step(1);
            Assert.That(view.Board.AllocatedCardCount,Is.EqualTo(28));
            view.OfferButton(0).onClick.Invoke();
            Assert.That(root.GetComponentsInChildren<CardView>(true).Length,Is.EqualTo(28));
            Assert.That(view.Board.ActiveCardCount,Is.EqualTo(controller.Deal.Layout.Count));
        }

        [Test]
        public void LossScreenIsRenderedAndRestartRequestIsWired()
        {
            RunController lost=null;
            for(int seed=1;seed<=30 && lost==null;seed++)
            {
                var candidate=new RunController(seed,content.CreateCatalog(),new RunConfig(drawLimit:0,baseTarget:1000000));
                for(int move=0;move<candidate.Deal.Layout.Count && candidate.State.Stage==RunStage.Playing;move++)
                    for(int i=0;i<candidate.Deal.Layout.Count;i++) if(candidate.CurrentRound.CanPlay(i)) { candidate.Execute(new RunAction(RunActionKind.Play,i)); break; }
                if(candidate.State.Stage==RunStage.Lost) lost=candidate;
            }
            Assert.That(lost,Is.Not.Null); host.Replace(lost); Assert.That(view.ResultTitle,Is.EqualTo("ROUND LOST"));
            int requests=0; host.NewRunRequested+=seed=>requests++; view.ContinueButton.onClick.Invoke(); Assert.That(requests,Is.EqualTo(1));
        }

        [Test]
        public void ShippedSceneAndAtlasAreReadyAndViewHasNoCoreReference()
        {
            Assert.That(EditorBuildSettings.scenes[0].path,Is.EqualTo("Assets/Scenes/CursedDeck.unity"));
            var importer=(SpriteAtlasImporter)AssetImporter.GetAtPath("Assets/_View/Art/CursedDeck.spriteatlasv2");
            Assert.That(importer,Is.Not.Null); Assert.That(importer.includeInBuild,Is.True);
            Assert.That(EditorSettings.spritePackerMode,Is.EqualTo(SpritePackerMode.SpriteAtlasV2));
            var atlas=AssetDatabase.LoadAssetAtPath<SpriteAtlas>("Assets/_View/Art/CursedDeck.spriteatlasv2");
            Assert.That(atlas.CanBindTo(content.CardFront),Is.True); Assert.That(atlas.CanBindTo(content.CardBack),Is.True);
            foreach(var assembly in typeof(GameView).Assembly.GetReferencedAssemblies())
            {
                Assert.That(assembly.Name,Is.Not.EqualTo("CursedDeck.Core")); Assert.That(assembly.Name,Is.Not.EqualTo("CursedDeck.Data"));
            }
            Assert.That(root.GetComponent<UnityEngine.UI.GraphicRaycaster>(),Is.Not.Null);
        }

        [Test]
        public void DebugCommandsCanJumpAllRoundsAndSaveTheirRedealtBaseline()
        {
            for(int i=0;i<9;i++)
            {
                bus.Send(new GameCommand(GameCommandKind.DebugJump,i));
                Assert.That(controller.State.Floor,Is.EqualTo(i/3+1)); Assert.That(controller.State.Round,Is.EqualTo(i%3+1));
            }
            bus.Send(new GameCommand(GameCommandKind.DebugGold)); bus.Send(new GameCommand(GameCommandKind.DebugRelic));
            Assert.That(controller.State.Gold,Is.EqualTo(100)); Assert.That(controller.State.Relics.Count,Is.EqualTo(1));
            string json=controller.ToJson(); Assert.That(RunController.FromJson(json).ToJson(),Is.EqualTo(json));
            Assert.That(root.transform.Find("SafeArea/DebugPanel"),Is.Not.Null);
        }
    }
}
