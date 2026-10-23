using System;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using CursedDeck.Data;
using CursedDeck.Bootstrap.Editor;
using CursedDeck.Services;
using CursedDeck.Run;
using CursedDeck.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CursedDeck.ViewTests
{
    public sealed class RecordingHaptics:IHapticOutput
    {
        public int Lights,Heavies;public bool Disposed;
        public void Light(){Lights++;}public void Heavy(){Heavies++;}public void Dispose(){Disposed=true;}
    }
    public sealed class FullCardSystemTests
    {
        private RecordingHaptics haptics;private GameObject root;private GameHost host;
        [SetUp] public void Setup(){haptics=new RecordingHaptics();HapticService.SetOutputForTests(haptics);HapticService.Enabled=true;}
        [TearDown] public void Cleanup(){host?.Dispose();if(root!=null)UnityEngine.Object.DestroyImmediate(root);HapticService.Shutdown();HapticService.Enabled=true;}
        private CardParticleFX Particles(int capacity=2)
        {
            root=new GameObject("Particle Canvas",typeof(RectTransform),typeof(Canvas));root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var go=new GameObject("Particles",typeof(RectTransform),typeof(CanvasRenderer),typeof(CardParticleFX));go.transform.SetParent(root.transform,false);var fx=go.GetComponent<CardParticleFX>();fx.Initialize(capacity,16);return fx;
        }
        [Test] public void DisabledHapticsSuppressBothImpactKindsAndDisposeOwnedBackend()
        {
            HapticService.Enabled=false;HapticService.TriggerLightImpact();HapticService.TriggerHeavyImpact();Assert.That(haptics.Lights+haptics.Heavies,Is.Zero);
            HapticService.Enabled=true;HapticService.TriggerLightImpact();HapticService.TriggerHeavyImpact();Assert.That(haptics.Lights,Is.EqualTo(1));Assert.That(haptics.Heavies,Is.EqualTo(1));HapticService.Shutdown();Assert.That(haptics.Disposed);
        }
        [Test] public void All52CardsBindReviewedArtOrExplicitFallbackAtRequestedDatabasePath()
        {
            var db=AssetDatabase.LoadAssetAtPath<CardCharacterDatabase>(CardArtPipeline.DatabasePath);Assert.That(db,Is.Not.Null);Assert.That(typeof(CardCharacterData).IsValueType);
            Assert.That(CardArtPipeline.DatabasePath,Is.EqualTo("Assets/_Data/CardCharacterDatabase.asset"));Assert.That(Resources.Load<GameContent>("CursedDeck/Content").Characters,Is.SameAs(db));
            Assert.That(AssetDatabase.AssetPathToGUID(CardArtPipeline.DatabasePath),Is.EqualTo("17f72047ada49e74bb95fa9dad78d187"),"Migration preserves existing references.");
            foreach(var card in db.Characters)
            {
                string filename=CardArtPipeline.FileName(card.Suit,card.Rank),path=CardArtPipeline.SourceFolder+"/"+filename;
                bool awaitingArt=Array.IndexOf(new[]{"Hearts_1.png","Hearts_12.png","Hearts_13.png","Spades_11.png","Spades_8.png","Spades_9.png","Diamonds_5.png","Diamonds_6.png"},filename)>=0;
                if(awaitingArt)
                {
                    Assert.That(File.Exists(path),Is.False,"Outdated baked names/ranks must not be included.");
                    Assert.That(card.SourceArt,Is.Null);Assert.That(card.Portrait,Is.SameAs(card.FallbackArt));Assert.That(card.DisplayPortrait,Is.SameAs(card.FallbackArt));
                }
                else
                {
                    Assert.That(card.SourceArt,Is.Not.Null);Assert.That(card.Portrait,Is.SameAs(card.SourceArt));
                    Assert.That(AssetDatabase.GetAssetPath(card.SourceArt),Is.EqualTo(path));var importer=(TextureImporter)AssetImporter.GetAtPath(path);Assert.That(importer.textureType,Is.EqualTo(TextureImporterType.Sprite));Assert.That(importer.maxTextureSize,Is.EqualTo(512));Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.CompressedHQ));
                }
                Assert.That(card.FactionColor,Is.EqualTo(CardParticleFX.SuitColor((Suit)card.Suit)));
            }
            Assert.That(db.Find(2,1).CharacterName,Is.EqualTo("Vaelkris"));Assert.That(db.Find(2,12).CharacterName,Is.EqualTo("Serynthra"));Assert.That(db.Find(3,11).CharacterName,Is.EqualTo("Ossvyrn"));Assert.That(db.Find(3,1).CharacterName,Is.EqualTo("Morcant"));Assert.That(db.Find(2,13).CharacterName,Is.EqualTo("Vhorzath"));Assert.That(db.Find(1,12).CharacterName,Is.EqualTo("Elyra"));Assert.That(db.Find(0,11).CharacterName,Is.EqualTo("Malrik"));
        }
        [Test] public void CatalogUpgradeIsIdempotentAndPreservesExistingArt()
        {
            var actual=Resources.Load<GameContent>("CursedDeck/Content").Characters;var db=ScriptableObject.CreateInstance<CardCharacterDatabase>();
            try{db.SetCharacters(actual.Characters.ToArray());Assert.That(db.UpgradeCatalog(CardCharacterMatrix.Create(),2));Assert.That(db.Find(3,1).Portrait,Is.SameAs(actual.Find(3,1).Portrait));var serialized=new SerializedObject(db);serialized.FindProperty("characters").GetArrayElementAtIndex(0).FindPropertyRelative("characterName").stringValue="Authored name";serialized.ApplyModifiedPropertiesWithoutUndo();Assert.That(db.UpgradeCatalog(CardCharacterMatrix.Create(),2),Is.False);Assert.That(db.Find(0,1).CharacterName,Is.EqualTo("Authored name"));}
            finally{UnityEngine.Object.DestroyImmediate(db);}
        }
        [TestCase(1,0,true)] [TestCase(11,1,true)] [TestCase(12,1,true)] [TestCase(13,1,true)]
        [TestCase(2,4,true)] [TestCase(2,5,true)] [TestCase(10,6,true)] [TestCase(0,5,false)] [TestCase(14,5,false)] [TestCase(3,-1,false)]
        public void EveryValidRankAndChainControlsBurst(int rank,int chain,bool expected){Assert.That(CardParticleFX.ShouldBurst(rank,chain),Is.EqualTo(expected));}
        [TestCase(Suit.Spades,"00E676")] [TestCase(Suit.Hearts,"FF1744")] [TestCase(Suit.Diamonds,"00E5FF")] [TestCase(Suit.Clubs,"D500F9")]
        public void BurstsUseExactSuitColorsWithoutOwningHaptics(Suit suit,string hex)
        {
            var fx=Particles();fx.PlaySuitBurst(suit,Vector3.one);Assert.That(fx.ActiveBurstCount,Is.EqualTo(1));Assert.That(fx.LiveParticleCount,Is.EqualTo(16));Assert.That(fx.LastSuit,Is.EqualTo(suit));Assert.That(ColorUtility.ToHtmlStringRGB(CardParticleFX.SuitColor(suit)),Is.EqualTo(hex));Assert.That(haptics.Heavies,Is.Zero);Assert.That(fx.raycastTarget,Is.False);
        }
        [Test] public void OverflowRecyclesWithoutGrowthAndOldOwnerCannotCancelNewBurst()
        {
            var fx=Particles(1);int old=fx.PlayOwnedSuitBurst(Suit.Hearts,Vector3.zero);int latest=fx.PlayOwnedSuitBurst(Suit.Diamonds,Vector3.one);fx.StopBurst(old);Assert.That(fx.ActiveBurstCount,Is.EqualTo(1));Assert.That(root.GetComponentsInChildren<ParticleSystem>().Length,Is.EqualTo(1));Assert.That(fx.AllocatedBurstCount,Is.EqualTo(1));Assert.That(fx.LiveParticleCount,Is.LessThanOrEqualTo(16));fx.StopBurst(latest);Assert.That(fx.ActiveBurstCount,Is.Zero);
            fx.PlaySuitBurst(Suit.Clubs,Vector3.zero);fx.Step(1);Assert.That(fx.LiveParticleCount,Is.Zero);fx.PlaySuitBurst(Suit.Spades,Vector3.zero);fx.gameObject.SetActive(false);Assert.That(fx.ActiveBurstCount,Is.Zero);
        }
        [Test] public void WarmParticleSimulationStepDoesNotAllocateManagedMemory()
        {
            var fx=Particles();fx.PlaySuitBurst(Suit.Spades,Vector3.zero);for(int i=0;i<20;i++)fx.Step(.001f);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)fx.Step(.001f);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero,"Measures the warmed simulation bridge, not a complete Canvas frame.");
        }
        [Test] public void OnlyAcceptedPlaysTriggerEffectsAndUndoResetsPool()
        {
            var content=Resources.Load<GameContent>("CursedDeck/Content");var bus=new EventBus();var controller=new RunController(42,content.CreateCatalog(),content.Rules.CreateConfig(),content.Layout.CreateLayout());host=new GameHost(controller,bus,content.CreatePresentation(),new UiSettings(true,true));
            root=new GameObject("Feedback",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(GameView));var view=root.GetComponent<GameView>();view.Initialize(bus,content.Font,content.CardFront,content.CardBack,content.SuitIcons);host.Publish(GameFeedback.Deal);
            int invalid=Enumerable.Range(0,28).First(i=>!controller.CurrentRound.CanPlay(i));bus.Send(new GameCommand(GameCommandKind.Play,invalid));Assert.That(haptics.Lights+haptics.Heavies,Is.Zero);Assert.That(view.Particles.TotalBurstsPlayed,Is.Zero);
            int played=0;foreach(int slot in controller.Deal.SolutionSlots)
            {
                if(controller.State.Stage!=RunStage.Playing)break;
                var card=view.Board.CardAt(slot);int rank=(int)controller.CurrentRound.GetCard(slot).Rank;int before=view.Particles.TotalBurstsPlayed;int heavies=haptics.Heavies,lightsBefore=haptics.Lights;
                bus.Send(new GameCommand(GameCommandKind.Play,slot));played++;bool qualifies=CardParticleFX.ShouldBurst(rank,view.LastSnapshot.Chain);Assert.That(view.Particles.TotalBurstsPlayed-before,Is.EqualTo(qualifies?1:0));Assert.That(haptics.Heavies-heavies,Is.Zero);Assert.That(haptics.Lights-lightsBefore,Is.EqualTo(1));Assert.That(card.FX.IsFlipping,Is.False);Assert.That(card.FX.TargetFront);
                if(played==5)break;
            }
            Assert.That(played,Is.GreaterThanOrEqualTo(5));Assert.That(view.Particles.TotalBurstsPlayed,Is.GreaterThan(0));controller.State.Consumables.Add(ConsumableKind.Undo);host.Publish();int effects=view.Particles.TotalBurstsPlayed;bus.Send(new GameCommand(GameCommandKind.Undo));Assert.That(view.Particles.ActiveBurstCount,Is.Zero);Assert.That(view.Particles.TotalBurstsPlayed,Is.EqualTo(effects));
            host.UpdateSettings(new UiSettings(true,false));int lights=haptics.Lights;HapticService.TriggerLightImpact();Assert.That(haptics.Lights,Is.EqualTo(lights));Assert.That(HapticService.Enabled,Is.False);Assert.That(view.Particles.AllocatedBurstCount,Is.EqualTo(12));
        }
        [Test] public void FrameTintTouchAndFlipMidpointDoNotDuplicateAcceptedPlayHaptics()
        {
            root=UnityEngine.Object.Instantiate(Resources.Load<CardView>("TCG/CardView")).gameObject;var card=root.GetComponent<CardView>();var content=Resources.Load<GameContent>("CursedDeck/Content");var db=content.Characters;var visuals=new CardCharacterVisual[52];foreach(var c in db.Characters)visuals[c.Suit*13+c.Rank-1]=new CardCharacterVisual(c.CharacterName,c.FactionTitle,CardView.SuitLabel(c.Suit,true),c.FactionColor,c.Portrait,c.Emblem);card.Initialize(null,new EventBus(),content.Font,content.CardFront,content.CardBack,content.SuitIcons,visuals);
            foreach(var c in db.Characters){card.Bind(new CardUiState(0,0,0,c.Rank,c.Suit,true,true,true,false),false);Assert.That(card.FrameColor,Is.EqualTo(Color.white),"Preserve the supplied gold outer frame; the old tint was wired to an invisible duplicate frame.");}
            var data=new PointerEventData(null){pointerId=7,position=Vector2.one};card.FX.OnPointerDown(data);card.FX.OnPointerDown(data);Assert.That(haptics.Lights,Is.Zero);card.FX.OnPointerUp(data);
            card.FX.SnapFace(false);var routine=card.FX.FlipCardRoutine(true);Assert.That(routine.MoveNext());card.FX.Step(.149f);Assert.That(haptics.Lights,Is.Zero);card.FX.Step(.001f);Assert.That(haptics.Lights,Is.Zero);card.FX.Step(.15f);Assert.That(routine.MoveNext(),Is.False);Assert.That(card.FX.FaceFront);
        }
        [Test] public void AndroidPermissionPatchIsIdempotentAndPreservesActivities()
        {
            string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".xml");try{File.WriteAllText(path,"<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"><application><activity android:name=\"existing.Activity\" /></application></manifest>");CardHapticsManifest.EnsurePermission(path);CardHapticsManifest.EnsurePermission(path);var xml=new XmlDocument();xml.Load(path);Assert.That(xml.SelectNodes("/manifest/uses-permission").Count,Is.EqualTo(1));Assert.That(xml.SelectNodes("/manifest/application/activity").Count,Is.EqualTo(1));Assert.That(File.ReadAllText(path),Does.Contain("android.permission.VIBRATE"));}finally{File.Delete(path);}
        }
    }
}
