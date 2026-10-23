using System;
using NUnit.Framework;
using CursedDeck.Data;
using CursedDeck.Run;
using CursedDeck.View;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.EventSystems;
namespace CursedDeck.ViewTests
{
    public sealed class CardFXTests
    {
        private CardView card;private GameObject eventObject;
        [SetUp] public void Setup()
        {
            card=UnityEngine.Object.Instantiate(Resources.Load<CardView>("TCG/CardView"));var content=Resources.Load<GameContent>("CursedDeck/Content");var db=Resources.Load<GameContent>("CursedDeck/Content").Characters;var visuals=new CardCharacterVisual[52];foreach(var c in db.Characters)visuals[c.Suit*13+c.Rank-1]=new CardCharacterVisual(c.CharacterName,c.FactionTitle,CardView.SuitLabel(c.Suit,true),c.FactionColor,c.Portrait,c.Emblem);
            card.Initialize(null,new EventBus(),content.Font,content.CardFront,content.CardBack,content.SuitIcons,visuals);
        }
        [TearDown] public void Cleanup(){if(card!=null)UnityEngine.Object.DestroyImmediate(card.gameObject);if(eventObject!=null)UnityEngine.Object.DestroyImmediate(eventObject);}
        [Test] public void PrefabHasDesignDimensionsMaskedPortraitAndSharedAdditiveGlow()
        {
            var prefab=Resources.Load<CardView>("TCG/CardView");Assert.That(((RectTransform)prefab.transform).sizeDelta,Is.EqualTo(new Vector2(300,420)));
            var front=card.transform.Find("CardMotionRoot/CardFront");var viewport=front.Find("PortraitViewport");Assert.That(((RectTransform)viewport).anchorMin,Is.EqualTo(new Vector2(.04f,.04f)));Assert.That(((RectTransform)viewport).anchorMax,Is.EqualTo(new Vector2(.96f,.96f)));Assert.That(viewport.GetComponent<UnityEngine.UI.RectMask2D>(),Is.Not.Null);Assert.That(viewport.Find("CharacterPortrait").GetComponent<UnityEngine.UI.Image>().preserveAspect);
            Assert.That(front.Find("HeaderBanner/RankBadge"),Is.Not.Null);Assert.That(front.Find("HeaderBanner/CharacterNameText"),Is.Not.Null);Assert.That(front.Find("BottomBanner/FactionTitleText"),Is.Not.Null);
            var glow=card.transform.Find("CardMotionRoot/SpecularGlowOverlay").GetComponent<UnityEngine.UI.Image>();Assert.That(glow.material.shader.name,Is.EqualTo("Cursed Deck/UI/Card Specular Glow"));Assert.That(glow.raycastTarget,Is.False);
            Assert.That(ShaderUtil.ShaderHasError(glow.material.shader),Is.False);
        }
        [Test] public void CompactBoardKeepsRankGlyphReadableWithoutGrowingHitArea()
        {
            card.SetLayout(100,140);var number=card.transform.Find("CardMotionRoot/CardFront/HeaderBanner/RankBadge/RankNumber");Assert.That(card.Rect.rect.width,Is.EqualTo(100));Assert.That(card.FX.MotionRoot.localScale.x,Is.EqualTo(1f/3).Within(.001));Assert.That(number.lossyScale.x,Is.EqualTo(1).Within(.001));card.SetLayout(300,420);Assert.That(number.localScale,Is.EqualTo(Vector3.one));
        }
        [Test] public void All52CharactersBindWithExactNamesAndNonNullPortraits()
        {
            var db=Resources.Load<GameContent>("CursedDeck/Content").Characters;foreach(var c in db.Characters){card.Bind(new CardUiState(0,0,0,c.Rank,c.Suit,true,true,true,false),false);Assert.That(card.DisplayedName,Is.EqualTo(c.CharacterName));Assert.That(card.RenderedPortrait,Is.SameAs(c.Portrait));}
            card.Bind(new CardUiState(0,0,0,0,0,false,false,false,false),false);Assert.That(card.DisplayedName,Is.Empty);
        }
        [Test] public void FaceSwapsAt90DegreesAndCompletionPunchSettles()
        {
            var fx=card.FX;fx.SnapFace(false);fx.FlipTo(true);fx.Step(.149f);Assert.That(fx.FaceFront,Is.False);fx.Step(.001f);Assert.That(fx.FaceFront,Is.True);Assert.That(fx.FlipDegrees,Is.EqualTo(90).Within(.01));fx.Step(.15f);Assert.That(fx.IsFlipping,Is.False);fx.Step(.05f);Assert.That(fx.MotionRoot.localScale.x,Is.GreaterThan(1));fx.Step(.3f);Assert.That(fx.MotionRoot.localScale.x,Is.EqualTo(1).Within(.001));
            fx.FlipTo(false);fx.Step(1);Assert.That(fx.FaceFront,Is.False);Assert.That(fx.FlipDegrees,Is.EqualTo(180).Within(.01));var back=card.transform.Find("CardMotionRoot/CardBack");Assert.That(Quaternion.Angle(back.rotation,Quaternion.identity),Is.LessThan(.01),"Back content must not finish mirrored.");
        }
        [Test] public void InterruptedFlipAndPoolDisableReturnToValidPose()
        {
            var fx=card.FX;fx.SnapFace(false);fx.FlipTo(true);fx.Step(.12f);fx.FlipTo(false);fx.Step(1);Assert.That(fx.FaceFront,Is.False);fx.FlipTo(true);fx.Step(.27f);Assert.That(fx.FaceFront);card.ResetForPool();card.gameObject.SetActive(false);card.gameObject.SetActive(true);Assert.That(fx.IsFlipping,Is.False);Assert.That(fx.FaceFront);Assert.That(Quaternion.Angle(fx.CurrentTilt,Quaternion.identity),Is.LessThan(.001));
        }
        private PointerEventData Press()
        {
            eventObject=new GameObject("FX events",typeof(EventSystem));var data=new PointerEventData(eventObject.GetComponent<EventSystem>()){pointerId=7,position=new Vector2(150,210)};card.FX.OnPointerDown(data);return data;
        }
        [Test] public void TouchTiltClampsTo15AndGlowResetsOnRelease()
        {
            var fx=card.FX;fx.SnapFace(true);var data=Press();Assert.That(Mathf.Abs(fx.TargetTiltDegrees.x),Is.LessThanOrEqualTo(15));Assert.That(Mathf.Abs(fx.TargetTiltDegrees.y),Is.LessThanOrEqualTo(15));fx.Step(.1f);Assert.That(fx.GlowOpacity,Is.GreaterThan(0));data.position=new Vector2(2000,-2000);fx.OnDrag(data);Assert.That(fx.TargetTiltDegrees,Is.EqualTo(new Vector2(15,15)));fx.OnPointerUp(data);fx.Step(1);Assert.That(fx.GlowOpacity,Is.Zero);Assert.That(Quaternion.Angle(fx.CurrentTilt,Quaternion.identity),Is.LessThan(.001));
        }
        [Test] public void WarmAnimatedStepHasNoManagedAllocations()
        {
            card.FX.SnapFace(true);Press();for(int i=0;i<50;i++)card.FX.Step(.001f);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)card.FX.Step(.001f);long allocated=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(allocated,Is.Zero,"Measures the CardFX step, not the complete Unity frame.");
        }
        [Test] public void CharacterAtlasesAreBuiltInAndKeepUiRotationDisabled()
        {
            foreach(string suit in new[]{"Clubs","Diamonds","Hearts","Spades"}){var importer=(SpriteAtlasImporter)AssetImporter.GetAtPath("Assets/_View/Art/TCG/Atlases/Characters-"+suit+".spriteatlasv2");Assert.That(importer.includeInBuild);Assert.That(importer.packingSettings.enableRotation,Is.False);Assert.That(importer.GetPlatformSettings("Android").maxTextureSize,Is.EqualTo(2048));}
        }
    }
}
