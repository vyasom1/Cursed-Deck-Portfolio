using TMPro;
using UnityEngine;
namespace CursedDeck.View
{
    public sealed partial class GameView
    {
        private UIPackSkin skin;
        public UnityEngine.UI.Button StatsButton {get;private set;}
        public void BuildMainScreen(TMP_FontAsset typeface,Sprite cardFront,Sprite cardBack)
        {
            font=typeface;front=cardFront;back=cardBack;skin=UIPackSkin.Load();
            var canvas=GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=0;
            var scaler=GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            var bg=UIPackLayout.Panel("Background",transform,skin!=null?skin.Background:null,Vector2.zero,Vector2.one);bg.type=UnityEngine.UI.Image.Type.Simple;
            var aspect=bg.GetComponent<UnityEngine.UI.AspectRatioFitter>();if(aspect==null)aspect=bg.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();aspect.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;aspect.aspectRatio=941f/1672;
            var shade=UIPackLayout.Panel("BackgroundShade",transform,null,Vector2.zero,Vector2.one);shade.color=new Color(.02f,.035f,.027f,.44f);
            safe=UiFactory.Rect("SafeArea",transform);UiFactory.Anchor(safe,Vector2.zero,Vector2.one);if(safe.GetComponent<SafeAreaView>()==null)safe.gameObject.AddComponent<SafeAreaView>();
            var header=UiFactory.Rect("HeaderBar",safe);UiFactory.Anchor(header,new Vector2(.02f,.885f),new Vector2(.98f,1));
            var plaque=UIPackLayout.Panel("TitleBanner",header,skin!=null?skin.TitlePlaque:null,new Vector2(.10f,.06f),new Vector2(.90f,.94f));
            var title=UiFactory.Text("Title",plaque.transform,font,"CURSED DECK",59,UiFactory.Gold);UiFactory.Anchor(title.rectTransform,new Vector2(.14f,.31f),new Vector2(.86f,.69f));title.color=Color.white;title.fontStyle=FontStyles.Bold;title.characterSpacing=2;title.enableVertexGradient=true;title.colorGradient=new VertexGradient(new Color(1,.898f,.498f),new Color(1,.898f,.498f),new Color(.702f,.561f,0),new Color(.702f,.561f,0));UIPackLayout.Fit(title,28);
            var settings=UiFactory.Button("SettingsButton",header,font,"",20,front,UiFactory.Green);UiFactory.Place((RectTransform)settings.transform,.045f,.52f,78,86);
            if(skin!=null){UIPackLayout.Button(settings,skin.SecondaryButton);UIPackLayout.Icon("GearIcon",settings.transform,skin.Settings,new Vector2(.16f,.16f),new Vector2(.84f,.84f));}
            StatsButton=UiFactory.Button("StatsButton",header,font,"",20,front,UiFactory.Green);UiFactory.Place((RectTransform)StatsButton.transform,.955f,.52f,78,86);
            if(skin!=null)UIPackLayout.Button(StatsButton,skin.SecondaryButton);
            for(int i=0;i<3;i++){var bar=UiFactory.Rect("Bar-"+i,StatsButton.transform);UiFactory.Anchor(bar,new Vector2(.23f+i*.2f,.25f),new Vector2(.36f+i*.2f,.43f+i*.15f));UiFactory.Image(bar,UiFactory.Gold);}
            table=UiFactory.Rect("GameBoardZone",safe);UiFactory.Anchor(table,new Vector2(.01f,.355f),new Vector2(.99f,.733f));
            var instruction=UIPackLayout.Panel("InstructionBanner",safe,skin!=null?skin.InstructionPanel:null,new Vector2(.025f,.305f),new Vector2(.975f,.354f));
            if(skin!=null)UIPackLayout.Icon("InstructionEmblem",instruction.transform,skin.Suits[1],new Vector2(.08f,.30f),new Vector2(.14f,.70f)).material=skin.SuitMaterials[1];
            var instructionText=UiFactory.Text("InstructionText",instruction.transform,font,"MATCH ONE RANK UP OR DOWN",25,UiFactory.Gold);UiFactory.Anchor(instructionText.rectTransform,new Vector2(.26f,.30f),new Vector2(.90f,.70f));instructionText.fontStyle=FontStyles.Bold;instructionText.characterSpacing=2;UIPackLayout.Fit(instructionText,16);
            var bottom=UiFactory.Rect("BottomControlPanel",safe);UiFactory.Anchor(bottom,new Vector2(.025f,.068f),new Vector2(.975f,.295f));
            draw=UiFactory.Button("DrawButton",bottom,font,"DRAW\n23 IN STOCK",38,front,UiFactory.Green);drawRect=(RectTransform)draw.transform;
            UiFactory.Place(drawRect,.71f,.46f,510,205);drawLabel=draw.GetComponentInChildren<TextMeshProUGUI>();drawLabel.fontStyle=FontStyles.Bold;drawLabel.color=UiFactory.Gold;drawLabel.margin=new Vector4(70,35,70,35);drawLabel.enableAutoSizing=true;drawLabel.fontSizeMin=25;drawLabel.fontSizeMax=38;
            if(skin!=null)
            {
                UIPackLayout.DrawStates(draw,skin);
                var glow=UIPackLayout.Panel("BreathingGlow",drawRect,skin.DrawHover,Vector2.zero,Vector2.one);glow.color=new Color(.1f,1,.55f,.05f);if(glow.GetComponent<DrawButtonBreath>()==null)glow.gameObject.AddComponent<DrawButtonBreath>();glow.transform.SetAsFirstSibling();
            }
            wasteRect=UiFactory.Rect("CurrentCardSlot",bottom);UiFactory.Place(wasteRect,.22f,.44f,160,224);UiFactory.Image(wasteRect,Color.white,front);
            wasteRank=UiFactory.Text("WasteRank",wasteRect,font,"J",52,UiFactory.Green,TextAlignmentOptions.TopLeft);UiFactory.Anchor(wasteRank.rectTransform,new Vector2(.13f,.59f),new Vector2(.87f,.94f));
            wasteSuit=UiFactory.Text("WasteSuit",wasteRect,font,"",25,UiFactory.Green);UiFactory.Anchor(wasteSuit.rectTransform,new Vector2(.10f,.05f),new Vector2(.90f,.20f));
            wasteIcon=UIPackLayout.Icon("SuitIcon",wasteRect,null,new Vector2(.25f,.26f),new Vector2(.75f,.63f));
            if(skin!=null){var frame=UIPackLayout.Panel("CurrentFrameOverlay",wasteRect,skin.CardFrame,Vector2.zero,Vector2.one);frame.type=UnityEngine.UI.Image.Type.Simple;}
            var current=UiFactory.Text("Current",wasteRect,font,"CURRENT",21,UiFactory.Gold);UiFactory.Anchor(current.rectTransform,new Vector2(-.15f,-.19f),new Vector2(1.15f,-.04f));UIPackLayout.Fit(current,16);
            reroll=UiFactory.Button("Reroll",bottom,font,"REROLL · 10 GOLD",20,front,UiFactory.Green);UiFactory.Place((RectTransform)reroll.transform,.71f,.08f,380,40);
            var utilities=UiFactory.Rect("UtilityControls",safe);UiFactory.Anchor(utilities,new Vector2(.035f,.021f),new Vector2(.965f,.063f));
            undo=UiFactory.Button("Undo",utilities,font,"UNDO 0",24,front,UiFactory.Green);peek=UiFactory.Button("Peek",utilities,font,"PEEK 0",24,front,UiFactory.Green);shuffle=UiFactory.Button("Shuffle",utilities,font,"SHUFFLE 0",24,front,UiFactory.Green);
            UiFactory.Anchor((RectTransform)undo.transform,new Vector2(0,0),new Vector2(.30f,1));UiFactory.Anchor((RectTransform)peek.transform,new Vector2(.35f,0),new Vector2(.65f,1));UiFactory.Anchor((RectTransform)shuffle.transform,new Vector2(.70f,0),Vector2.one);
            undoLabel=undo.GetComponentInChildren<TextMeshProUGUI>();peekLabel=peek.GetComponentInChildren<TextMeshProUGUI>();shuffleLabel=shuffle.GetComponentInChildren<TextMeshProUGUI>();
            if(skin!=null){UIPackLayout.Button(reroll,skin.SecondaryButton);UIPackLayout.Button(undo,skin.SecondaryButton);UIPackLayout.Button(peek,skin.SecondaryButton);UIPackLayout.Button(shuffle,skin.SecondaryButton);}
            notice=UiFactory.Text("Notice",safe,font,"",20,UiFactory.Gold);UiFactory.Anchor(notice.rectTransform,new Vector2(.08f,.370f),new Vector2(.92f,.395f));UIPackLayout.Fit(notice,14);
            hud=new HUD(safe,font);
            BuildMenuScreens();
            safe.GetComponent<SafeAreaView>().Apply(Screen.safeArea,Screen.width,Screen.height);
        }
        private void PositionControls(bool left)
        {
            UiFactory.Place(drawRect,left?.29f:.71f,.46f,510,205);UiFactory.Place(wasteRect,left?.78f:.22f,.44f,160,224);
            UiFactory.Place((RectTransform)reroll.transform,left?.29f:.71f,.08f,380,40);
        }
    }
}
