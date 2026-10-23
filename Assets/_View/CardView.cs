using TMPro;
using CursedDeck.Services;
using UnityEngine;
using CursedDeck.Run;
namespace CursedDeck.View
{
    public sealed class CardView:MonoBehaviour
    {
        private static readonly string[] Ranks={"","A","2","3","4","5","6","7","8","9","10","J","Q","K"};
        private static readonly string[] Suits={"C","D","H","S"};
        private static readonly string[] Labels={"CLUB","DIAMOND","HEART","SPADE"};
        private static readonly Color[] FactionColors={new Color(.67f,.42f,1),new Color(.29f,.66f,1),new Color(.90f,.24f,.40f),new Color(.21f,.89f,.63f)};
        [SerializeField] private UnityEngine.UI.Image characterPortrait,suitEmblem,backImage,frameBorder;
        [SerializeField] private UnityEngine.UI.Image[] factionGems;
        [SerializeField] private TextMeshProUGUI rankBadge,characterName,factionTitle;
        [SerializeField] private CanvasGroup frontGroup;
        [SerializeField] private Sprite genericPortrait;
        private UnityEngine.UI.Image[] cornerEmblems;private UIPackSkin skin;
        private BoardView owner;private EventBus bus;private Sprite[] icons;
        private CardCharacterVisual[] characters;
        private bool hasBound,playable,dealing,targetVisible,reduceMotion;private CardArtShade artShade;private UnityEngine.UI.Image blockedShade;private CardPlayableGlow playableGlow;private float glowAge;private CardLongPress longPress;
        private int boundRank,boundSuit,burstToken;private CardParticleFX particles;
        public Color FrameColor=>frameBorder!=null?frameBorder.color:Color.clear;
        public void SetParticleFX(CardParticleFX value){particles=value;}
        public RectTransform Rect{get;private set;} public UnityEngine.UI.Button Button{get;private set;}
        public CardFX FX{get;private set;}
        public int Slot{get;private set;}=-1;public bool InPool{get;internal set;}
        public string DisplayedName=>FX!=null&&FX.TargetFront?characterName.text:"";public Sprite RenderedPortrait=>characterPortrait.sprite;
        public void Initialize(BoardView owner,EventBus bus,TMP_FontAsset font,Sprite front,Sprite back,Sprite[] suitIcons=null,CardCharacterVisual[] catalog=null)
        {
            this.owner=owner;this.bus=bus;icons=suitIcons;characters=catalog;Rect=(RectTransform)transform;Button=GetComponent<UnityEngine.UI.Button>();FX=GetComponent<CardFX>();
            rankBadge.font=characterName.font=factionTitle.font=font;backImage.sprite=back;
            skin=UIPackSkin.Load();cornerEmblems=new[]{transform.Find("CardMotionRoot/CardFront/SuitBadgeTopRight")?.GetComponent<UnityEngine.UI.Image>(),transform.Find("CardMotionRoot/CardFront/SuitBadgeBottomLeft")?.GetComponent<UnityEngine.UI.Image>()};
            if(skin!=null)backImage.sprite=skin.CardBack;
            CardReadability.Rank(rankBadge);
            var face=frontGroup.transform;
            artShade=face.Find("PortraitViewport/BoardArtShade")?.GetComponent<CardArtShade>();
            blockedShade=face.Find("BlockedShade")?.GetComponent<UnityEngine.UI.Image>();
            playableGlow=face.Find("PlayableGlow")?.GetComponent<CardPlayableGlow>();
            longPress=GetComponent<CardLongPress>();if(longPress!=null)longPress.Initialize(bus,()=>Slot);
            Button.onClick.AddListener(Click);FX.SnapFace(false);SetLayout(300,420);
        }
        private void Click(){if(longPress!=null&&longPress.ConsumeClick())return;bus?.Send(new GameCommand(GameCommandKind.Play,Slot));}
        public void Bind(CardUiState state,bool colorblind)
        {
            targetVisible=state.Visible;Slot=state.Slot;InPool=false;gameObject.SetActive(true);Rect.localScale=Vector3.one;Button.interactable=state.Playable;Button.targetGraphic.raycastTarget=true;
            if(state.Visible)
            {
                boundRank=state.Rank;boundSuit=state.Suit;
                int index=state.Suit*13+state.Rank-1;bool valid=state.Rank>=1&&state.Rank<=13&&state.Suit>=0&&state.Suit<4;
                var visual=valid&&characters!=null&&characters.Length==52?characters[index]:default;
                Color color=visual.Name!=null?visual.Color:FactionColors[Mathf.Clamp(state.Suit,0,3)];
                rankBadge.text=RankLabel(state.Rank);characterName.text=visual.Name??(RankLabel(state.Rank)+" OF "+SuitLabel(state.Suit,true));
                factionTitle.text=visual.Name!=null?(colorblind?visual.AccessibleFaction:visual.Faction):SuitLabel(state.Suit,true);
                if(frameBorder!=null)frameBorder.color=Color.white;rankBadge.color=state.Suit==2?new Color(1,.36f,.40f):state.Suit==1?UiFactory.Gold:UiFactory.Cream;factionTitle.color=color;characterPortrait.sprite=visual.Portrait!=null?visual.Portrait:genericPortrait;
                suitEmblem.sprite=visual.Emblem!=null?visual.Emblem:icons!=null&&icons.Length==4?icons[state.Suit]:null;suitEmblem.color=color;
                if(skin!=null){suitEmblem.sprite=skin.Suits[state.Suit];suitEmblem.color=Color.white;suitEmblem.material=skin.SuitMaterials[state.Suit];for(int i=0;i<cornerEmblems.Length;i++)if(cornerEmblems[i]!=null){cornerEmblems[i].sprite=skin.Suits[state.Suit];cornerEmblems[i].color=Color.white;cornerEmblems[i].material=skin.SuitMaterials[state.Suit];}}
                for(int i=0;i<factionGems.Length;i++)factionGems[i].color=color;
            }
            else if(!hasBound||!FX.FaceFront){rankBadge.text=characterName.text=factionTitle.text="";characterPortrait.sprite=genericPortrait;}
            frontGroup.alpha=1;playable=state.Playable;backImage.color=playable?Color.white:new Color(.5f,.5f,.5f,1);
            if(blockedShade!=null)blockedShade.enabled=!playable;
            if(playableGlow!=null){playableGlow.enabled=playable;if(playable)playableGlow.Pulse(glowAge);}
            if(longPress!=null)longPress.CanInspect=state.Visible;
            if(!hasBound){FX.SnapFace(state.Visible);hasBound=true;}else if(!dealing)FX.FlipTo(state.Visible);
        }
        public void SetLayout(float width,float height)
        {
            Rect.sizeDelta=new Vector2(width,height);
            float scale=Mathf.Min(width/300,height/420),inverse=1/Mathf.Max(.001f,scale);
            FX.SetLayoutScale(scale);
            bool board=width<200;
            characterName.enabled=!board;factionTitle.enabled=!board;suitEmblem.enabled=!board;
            if(artShade!=null)artShade.enabled=board;
            var face=(RectTransform)frontGroup.transform;
            var header=face.Find("HeaderBanner").GetComponent<UnityEngine.UI.Image>();if(header!=null)header.enabled=!board;
            var bottom=face.Find("BottomBanner").GetComponent<UnityEngine.UI.Image>();if(bottom!=null)bottom.enabled=!board;
            var badge=(RectTransform)rankBadge.transform.parent;
            float badgeScale=Mathf.Max(1,inverse);
            badge.anchorMin=badge.anchorMax=new Vector2(.5f,.5f);badge.sizeDelta=new Vector2(58,84);badge.localScale=Vector3.one*badgeScale;
            rankBadge.rectTransform.localScale=Vector3.one;UiFactory.Anchor(rankBadge.rectTransform,new Vector2(0,.50f),Vector2.one);
            float x=face.rect.xMin+35*badgeScale,y=face.rect.yMax-48*badgeScale;
            badge.localPosition=badge.parent.InverseTransformPoint(face.TransformPoint(new Vector3(x,y,0)));
            if(cornerEmblems==null)return;
            for(int i=0;i<cornerEmblems.Length;i++)
            {
                if(cornerEmblems[i]==null)continue;
                var emblem=cornerEmblems[i].rectTransform;emblem.anchorMin=emblem.anchorMax=new Vector2(.5f,.5f);
                emblem.sizeDelta=Vector2.one*44*badgeScale;
                // Dominant rank/suit stack in the same top-left badge; secondary lower-right mark stays quiet.
                emblem.anchoredPosition=i==0?new Vector2(x,face.rect.yMax-68*badgeScale):new Vector2(face.rect.xMax-28*badgeScale,face.rect.yMin+28*badgeScale);
                cornerEmblems[i].enabled=i==0; // one 44px emblem, no duplicate badge competing with the rank
            }
        }
        private void Update(){StepHighlight(Time.unscaledDeltaTime);}
        public void StepHighlight(float delta){if(reduceMotion||!playable||playableGlow==null)return;glowAge+=Mathf.Max(0,delta);playableGlow.Pulse(glowAge);}
        public void SetReduceMotion(bool value){reduceMotion=value;FX.SetReduceMotion(value);if(value&&playableGlow!=null)playableGlow.Pulse(0);}
        public void BeginDeal(){dealing=true;FX.SnapFace(false);}
        public void CancelDeal(){dealing=false;FX.SetDealPose(Vector2.zero,1);}
        public void CompleteDeal(bool instant){CancelDeal();if(instant||reduceMotion)FX.SnapFace(targetVisible);else FX.FlipTo(targetVisible);}
        public void OnCardPlayed(int currentChain=0)
        {
            Button.interactable=false;Button.targetGraphic.raycastTarget=false;dealing=false;playable=false;if(playableGlow!=null)playableGlow.enabled=false;
            var position=FX.HasTapPosition?FX.TapWorldPosition:Rect.position;HapticService.TriggerLightImpact();FX.SnapFace(true);
            if(particles!=null&&CardParticleFX.ShouldBurst(boundRank,currentChain))burstToken=particles.PlayOwnedSuitBurst((Suit)boundSuit,position);
        }
        public void ResetForPool(){dealing=false;hasBound=false;playable=false;glowAge=0;if(playableGlow!=null)playableGlow.enabled=false;if(longPress!=null)longPress.ResetHold();boundRank=boundSuit=0;if(particles!=null)particles.StopBurst(burstToken);burstToken=0;if(FX!=null)FX.SnapFace(FX.TargetFront);}
        private void OnDisable(){ResetForPool();}
        public void CompleteRemoval(){burstToken=0;if(owner!=null)owner.Release(this);}
        public static string RankLabel(int rank)=>rank>0&&rank<Ranks.Length?Ranks[rank]:"?";
        public static string SuitLabel(int suit,bool colorblind)=>suit>=0&&suit<4?(colorblind?Labels[suit]:Suits[suit]):"";
    }
}
