using System;
using NUnit.Framework;
using CursedDeck.Data;
using CursedDeck.Run;
using CursedDeck.View;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
namespace CursedDeck.ViewTests
{
    public sealed class VisualPhase1Tests
    {
        [TestCase(1920)][TestCase(2340)][TestCase(2400)]
        public void ActiveTwoPeaksFitsReadableCardsAndLiftInsideBoard(int height)
        {
            var data=Resources.Load<GameContent>("CursedDeck/Content");Assert.That(data.Layout.CreateLayout().Count,Is.EqualTo(19));
            var bus=new EventBus();var run=new RunController(42,data.CreateCatalog(),data.Rules.CreateConfig(),data.Layout.CreateLayout());
            var root=UnityEngine.Object.Instantiate(Resources.Load<GameView>("UI/BoardView"));
            try
            {
                root.Initialize(bus,data.Font,data.CardFront,data.CardBack,data.SuitIcons);root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;((RectTransform)root.transform).sizeDelta=new Vector2(1080,height);
                root.SafeRoot.GetComponent<SafeAreaView>().Apply(new Rect(0,0,1080,height),1080,height);Canvas.ForceUpdateCanvases();
                using(var host=new GameHost(run,bus,data.CreatePresentation(),new UiSettings()))
                {
                    host.Publish(GameFeedback.Deal);root.GetComponent<TweenRunner>().Step(.4f);
                    foreach(var state in root.LastSnapshot.Cards){var fx=root.Board.CardAt(state.Slot).FX;fx.Step(.5f);fx.Step(.21f);}
                    Canvas.ForceUpdateCanvases();Assert.That(root.Board.ActiveCardCount,Is.EqualTo(19));
                    var board=(RectTransform)root.Board.transform;var corners=new Vector3[4];
                    foreach(var state in root.LastSnapshot.Cards)
                    {
                        var card=root.Board.CardAt(state.Slot);Assert.That(card.Rect.rect.width,Is.GreaterThanOrEqualTo(140));card.Rect.GetWorldCorners(corners);
                        foreach(var point in corners){var p=board.InverseTransformPoint(point);Assert.That(p.x,Is.InRange(board.rect.xMin-.01f,board.rect.xMax+.01f));Assert.That(p.y,Is.InRange(board.rect.yMin-.01f,board.rect.yMax+.01f));}
                        float step=BoardGeometry.RowStep(card.Rect.rect.height,board.rect.height,3);Assert.That(card.Rect.anchoredPosition.y,Is.EqualTo((1.5f-state.Y)*step+(state.Playable?12:0)).Within(.01));
                        var front=card.transform.Find("CardMotionRoot/CardFront");Assert.That(front.Find("PlayableGlow").GetComponent<CardPlayableGlow>().enabled,Is.EqualTo(state.Playable));Assert.That(front.Find("BlockedShade").GetComponent<UnityEngine.UI.Image>().enabled,Is.EqualTo(!state.Playable));
                        var rank=front.Find("HeaderBanner/RankBadge/RankNumber").GetComponent<TextMeshProUGUI>();Assert.That(rank.fontSize,Is.EqualTo(36));Assert.That(rank.fontSharedMaterial.GetColor("_OutlineColor"),Is.EqualTo(Color.black));Assert.That(rank.fontSharedMaterial.GetFloat("_OutlineSoftness"),Is.Zero);ShaderUtilities.UpdateShaderRatios(rank.fontSharedMaterial);Assert.That(rank.fontSharedMaterial.GetFloat("_ScaleRatioA"),Is.EqualTo(1));Assert.That(rank.fontSharedMaterial.GetFloat("_FaceDilate"),Is.EqualTo(rank.fontSharedMaterial.GetFloat("_OutlineWidth")),"Outline must grow outward without consuming the bright numeral.");Assert.That(rank.fontSharedMaterial.GetColor("_FaceColor"),Is.EqualTo(Color.white));
                        var emblem=(RectTransform)front.Find("SuitBadgeTopRight");Assert.That(emblem.rect.width*card.FX.MotionRoot.localScale.x,Is.EqualTo(44).Within(.01));
                    }
                    var instruction=root.SafeRoot.Find("InstructionBanner/InstructionText").GetComponent<TextMeshProUGUI>();Assert.That(instruction.font,Is.SameAs(data.Font));
                    var guide=root.Tutorial.transform.Find("Guide").GetComponent<UnityEngine.UI.Image>();Assert.That(guide.sprite,Is.Null);Assert.That(guide.color,Is.EqualTo(new Color(0,0,0,230/255f)));
                    var sample=root.Board.CardAt(run.Deal.SolutionSlots[0]);sample.StepHighlight(.1f);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)sample.StepHighlight(.01f);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero,"Cached project-side highlight loop only.");
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root.gameObject);}
        }
        [TestCase(1920,0)][TestCase(2340,0)][TestCase(2400,0)][TestCase(1920,160)]
        public void Standard360DpPortraitDevicesReachFiftyDp(int height,int excludedVerticalPixels)
        {
            float scale=Mathf.Sqrt(height/1920f); // 1080px wide, CanvasScaler Match=.5
            float width=BoardGeometry.CardWidth(1080*.98f/scale,(height-excludedVerticalPixels)*.378f/scale,6,3,true,scale,3);
            Assert.That(width*scale/3,Is.GreaterThanOrEqualTo(50-.001f));
        }
        [Test]public void PhysicalSizingRequestsFiftyDpAndDocumentsNarrowScreenLimit()
        {
            Assert.That(BoardGeometry.CardWidth(1058.4f,735.36f,6,3,true,1,3),Is.EqualTo(150).Within(.01));
            float small=BoardGeometry.CardWidth(320*.98f,800,6,3,true,1,1);Assert.That(small,Is.LessThan(50));
        }
        [Test]public void LongPressIsPresentationOnlyAndConsumesTheFollowingClick()
        {
            var root=new GameObject("Hold",typeof(RectTransform),typeof(CardLongPress));
            try{var hold=root.GetComponent<CardLongPress>();var bus=new EventBus();int inspected=-1;bus.CardInspectionRequested+=slot=>inspected=slot;hold.Initialize(bus,()=>12);hold.CanInspect=true;hold.OnPointerDown(new PointerEventData(null));hold.Step(.51f);Assert.That(inspected,Is.EqualTo(12));Assert.That(hold.ConsumeClick());Assert.That(hold.ConsumeClick(),Is.False);hold.ResetHold();hold.OnPointerDown(new PointerEventData(null));hold.Step(1);Assert.That(hold.ConsumeClick(),Is.False);}
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
