using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CursedDeck.Bootstrap.Editor;
using CursedDeck.Data;
using CursedDeck.Run;
using CursedDeck.View;
namespace CursedDeck.ViewTests
{
    public sealed class UIPackTests
    {
        private GameObject root;private GameHost host;private GameView view;private RunController controller;
        [TearDown]public void Cleanup(){host?.Dispose();if(root!=null)Object.DestroyImmediate(root);}
        private void Screen(int width=1080,int height=1920,bool left=false)
        {
            var content=Resources.Load<GameContent>("CursedDeck/Content");var bus=new EventBus();controller=new RunController(42,content.CreateCatalog(),content.Rules.CreateConfig(),content.Layout.CreateLayout());host=new GameHost(controller,bus,content.CreatePresentation(),new UiSettings(true,false,left));
            root=Object.Instantiate(Resources.Load<GameView>("UI/BoardView")).gameObject;view=root.GetComponent<GameView>();view.Initialize(bus,content.Font,content.CardFront,content.Characters.DefaultCardBack,content.SuitIcons);
            root.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;float scale=Mathf.Sqrt((width/1080f)*(height/1920f));((RectTransform)root.transform).sizeDelta=new Vector2(width/scale,height/scale);view.SafeRoot.GetComponent<SafeAreaView>().Apply(new Rect(0,50,width,height-100),width,height);Canvas.ForceUpdateCanvases();host.Publish(GameFeedback.Deal);Canvas.ForceUpdateCanvases();
        }
        [Test]public void All27PngsAreAlphaSpritesWithAppropriateBorders()
        {
            var files=Directory.GetFiles(UIWireframePipeline.Source,"*.png",SearchOption.AllDirectories);Assert.That(files.Length,Is.EqualTo(27));
            foreach(var raw in files)
            {
                string file=raw.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(file);Assert.That(importer.textureType,Is.EqualTo(TextureImporterType.Sprite),file);Assert.That(importer.spriteImportMode,Is.EqualTo(UIWireframeSprites.Trimmed(file)?SpriteImportMode.Multiple:SpriteImportMode.Single));Assert.That(importer.alphaIsTransparency);Assert.That(importer.mipmapEnabled,Is.False);Assert.That(importer.isReadable,Is.False);Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(file),Is.Not.Null);
                bool panel=file.Contains("/Panels/")||file.Contains("/Buttons/")||file.EndsWith("TitlePlaque.png");Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(file).border.x>0,Is.EqualTo(panel),file);Assert.That(AssetDatabase.LoadAssetAtPath<Sprite>(file).border.y>0,Is.EqualTo(panel),file);Assert.That(importer.GetPlatformTextureSettings("Android").format,Is.EqualTo(TextureImporterFormat.ASTC_6x6));
            }
        }
        [Test]public void SkinDatabaseAndCardPrefabUseTheSuppliedArt()
        {
            var skin=UIPackSkin.Load();Assert.That(skin,Is.Not.Null);skin.Validate();Assert.That(skin.SuitMaterials[3].GetFloat("_Mode"),Is.EqualTo(1));Assert.That(skin.SuitMaterials[1].GetFloat("_Mode"),Is.EqualTo(2));Assert.That(ShaderUtil.ShaderHasError(skin.SuitMaterials[1].shader),Is.False);var db=AssetDatabase.LoadAssetAtPath<CardCharacterDatabase>(CardArtPipeline.DatabasePath);Assert.That(db.DefaultCardBack,Is.SameAs(skin.CardBack));Assert.That(db.Characters.Count,Is.EqualTo(52));db.Validate();
            var card=Resources.Load<CardView>("TCG/CardView");var front=card.transform.Find("CardMotionRoot/CardFront");Assert.That(front.Find("GothicFrameOverlay").GetComponent<Image>().sprite,Is.SameAs(skin.CardFrame));Assert.That(front.Find("OrnateFrame").GetComponent<Image>().enabled,Is.False);Assert.That(front.Find("GothicFrameOverlay").GetSiblingIndex(),Is.LessThan(front.Find("HeaderBanner").GetSiblingIndex()));Assert.That(front.Find("SuitBadgeTopRight"),Is.Not.Null);Assert.That(front.Find("SuitBadgeBottomLeft"),Is.Not.Null);
        }
        [Test]public void All52CardsBindColoredCornerSuitsWithoutChangingNumericIdentity()
        {
            var skin=UIPackSkin.Load();var content=Resources.Load<GameContent>("CursedDeck/Content");root=Object.Instantiate(Resources.Load<CardView>("TCG/CardView")).gameObject;var card=root.GetComponent<CardView>();card.Initialize(null,new EventBus(),content.Font,content.CardFront,content.CardBack,content.SuitIcons);
            for(int suit=0;suit<4;suit++)for(int rank=1;rank<=13;rank++)
            {card.Bind(new CardUiState(0,0,0,rank,suit,true,true,true,false),false);var emblem=root.transform.Find("CardMotionRoot/CardFront/SuitBadgeTopRight").GetComponent<Image>();Assert.That(emblem.sprite,Is.SameAs(skin.Suits[suit]));Assert.That(emblem.color,Is.EqualTo(Color.white));if(skin.SuitMaterials[suit]!=null)Assert.That(emblem.material,Is.SameAs(skin.SuitMaterials[suit]));else Assert.That(emblem.material.HasProperty("_Mode"),Is.False);Assert.That(card.DisplayedName,Is.Not.Empty);}
        }
        [Test]public void PrefabDrawUsesNativeSpriteSwapForAllStates()
        {
            Screen();var skin=UIPackSkin.Load();var button=view.DrawButton;Assert.That(button.transition,Is.EqualTo(Selectable.Transition.SpriteSwap));Assert.That(((Image)button.targetGraphic).sprite,Is.SameAs(skin.DrawDefault));Assert.That(((Image)button.targetGraphic).type,Is.EqualTo(Image.Type.Sliced));Assert.That(button.spriteState.highlightedSprite,Is.SameAs(skin.DrawHover));Assert.That(button.spriteState.pressedSprite,Is.SameAs(skin.DrawHover));Assert.That(button.spriteState.disabledSprite,Is.SameAs(skin.DrawDisabled));Assert.That(button.targetGraphic.raycastTarget);
        }
        [Test]public void ReusingAuthoringLayoutDoesNotDuplicateVisualsOrBindings()
        {
            var content=Resources.Load<GameContent>("CursedDeck/Content");root=Object.Instantiate(Resources.Load<GameView>("UI/BoardView")).gameObject;view=root.GetComponent<GameView>();int before=root.GetComponentsInChildren<RectTransform>(true).Length;view.BuildMainScreen(content.Font,content.CardFront,content.CardBack);view.BuildMainScreen(content.Font,content.CardFront,content.CardBack);Assert.That(root.GetComponentsInChildren<RectTransform>(true).Length,Is.EqualTo(before));Assert.That(root.GetComponentsInChildren<HUDView>(true).Length,Is.EqualTo(1));
        }
        [TestCase(1080,1920,false)][TestCase(1080,2340,false)][TestCase(1080,2400,false)]
        [TestCase(1080,1920,true)][TestCase(1080,2340,true)][TestCase(1080,2400,true)]
        public void PortraitZonesAndActionsDoNotOverlapAndLabelsFit(int width,int height,bool left)
        {
            Screen(width,height,left);var hud=view.GetComponentInChildren<HUDView>();Assert.That(hud.Floor.text,Is.EqualTo("FLOOR 1 / ROUND 1"));Assert.That(hud.Venue.text,Is.EqualTo("HAUNTED CASINO"));string[] zones={"HeaderBar","HUDStatsPanel","GameBoardZone","InstructionBanner","BottomControlPanel","UtilityControls"};
            for(int i=0;i<zones.Length-1;i++)
            {var upper=(RectTransform)view.SafeRoot.Find(zones[i]);var lower=(RectTransform)view.SafeRoot.Find(zones[i+1]);Assert.That(Bounds(upper).yMin,Is.GreaterThanOrEqualTo(Bounds(lower).yMax-.2f),zones[i]+" / "+zones[i+1]);}
            var notice=Bounds((RectTransform)view.SafeRoot.Find("Notice"));Assert.That(notice.Overlaps(Bounds((RectTransform)view.SafeRoot.Find("InstructionBanner"))),Is.False);Assert.That(notice.Overlaps(Bounds((RectTransform)view.SafeRoot.Find("BottomControlPanel"))),Is.False);
            var draw=Bounds((RectTransform)view.DrawButton.transform);var waste=Bounds((RectTransform)view.SafeRoot.Find("BottomControlPanel/CurrentCardSlot"));Assert.That(draw.Overlaps(waste),Is.False);
            foreach(var label in root.GetComponentsInChildren<TextMeshProUGUI>())
            {if(label.transform.IsChildOf(view.Board.transform)||label.name=="Notice"||label.name=="Label"&&label.transform.parent.name=="Reroll")continue;label.ForceMeshUpdate();Assert.That(label.isTextOverflowing,Is.False,label.transform.parent.name+"/"+label.name+": "+label.text);}
            Assert.That(view.Board.AllocatedCardCount,Is.EqualTo(28));
        }
        [Test]public void ModalsUse85PercentBlackAndStayAboveAllGameplayGraphics()
        {
            Screen();foreach(int slot in controller.Deal.SolutionSlots){if(controller.State.Stage!=RunStage.Playing)break;view.Board.CardAt(slot).Button.onClick.Invoke();}
            Assert.That(view.ResultVisible);var modal=view.SafeRoot.Find("Result");Assert.That(modal.GetSiblingIndex(),Is.EqualTo(view.SafeRoot.childCount-1));var tint=modal.GetComponent<Image>().color;Assert.That(tint,Is.EqualTo(new Color(0,0,0,217/255f)));view.OpenSettings();Assert.That(view.SafeRoot.Find("SettingsPanel").GetSiblingIndex(),Is.EqualTo(view.SafeRoot.childCount-1));
        }
        [Test]public void WireMenuCanRunTwiceWithoutChangingPrefabGuidsOrSourceBytes()
        {
            string board=AssetDatabase.AssetPathToGUID(UIWireframePipeline.BoardPath),card=AssetDatabase.AssetPathToGUID(CardArtPipeline.PrefabPath);
            var paths=Directory.GetFiles(UIWireframePipeline.Source,"*.png",SearchOption.AllDirectories);var bytes=paths.Select(File.ReadAllBytes).ToArray();
            UIWireframePipeline.Prepare();UIWireframePipeline.Prepare();
            Assert.That(AssetDatabase.AssetPathToGUID(UIWireframePipeline.BoardPath),Is.EqualTo(board));Assert.That(AssetDatabase.AssetPathToGUID(CardArtPipeline.PrefabPath),Is.EqualTo(card));var front=Resources.Load<CardView>("TCG/CardView").transform.Find("CardMotionRoot/CardFront");Assert.That(front.Find("GothicFrameOverlay").GetSiblingIndex(),Is.LessThan(front.Find("HeaderBanner").GetSiblingIndex()));
            for(int i=0;i<paths.Length;i++)Assert.That(File.ReadAllBytes(paths[i]),Is.EqualTo(bytes[i]),paths[i]);
        }
        private static Rect Bounds(RectTransform rect){var corners=new Vector3[4];rect.GetWorldCorners(corners);return Rect.MinMaxRect(corners.Min(c=>c.x),corners.Min(c=>c.y),corners.Max(c=>c.x),corners.Max(c=>c.y));}
    }
}
