using TMPro;
using UnityEngine;
using CursedDeck.Run;
namespace CursedDeck.View
{
    /// <summary>Five-step FTUE presenter. Run owns accepted actions; this owns focus, hand motion and fade.</summary>
    public sealed class TutorialManager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private UnityEngine.UI.Image[] masks,borders;
        [SerializeField] private TextMeshProUGUI banner;
        [SerializeField] private UnityEngine.UI.Button next;
        [SerializeField] private RectTransform hand;
        private readonly Rect[] regions=new Rect[16],scratch=new Rect[16];
        private readonly Vector3[] corners=new Vector3[4];
        private RectTransform rect,score,waste,draw,chain;
        private BoardView board;private EventBus bus;private GameSnapshot snapshot;
        private int regionCount;private float age,fade;private bool fading,wasGuiding;
        private Vector2 handFrom,handTo;
        public TutorialStage Stage {get;private set;}
        public UnityEngine.UI.Button NextButton=>next;
        public RectTransform Hand=>hand;
        public bool IsFading=>fading;
        public int HighlightedSlot=>snapshot?.TutorialSlot??-1;
        public void BuildVisuals(TMP_FontAsset font,Sprite panel)
        {
            rect=(RectTransform)transform;UiFactory.Anchor(rect,Vector2.zero,Vector2.one);
            group=gameObject.AddComponent<CanvasGroup>();
            masks=new UnityEngine.UI.Image[16];borders=new UnityEngine.UI.Image[8];
            for(int i=0;i<masks.Length;i++)masks[i]=UiFactory.Image(UiFactory.Rect("Dim-"+i,rect),new Color(0,0,0,.8f),null,true);
            for(int i=0;i<borders.Length;i++)borders[i]=UiFactory.Image(UiFactory.Rect("Focus-"+i,rect),UiFactory.Gold);
            var plaque=UIPackLayout.Panel("Guide",rect,panel,new Vector2(.065f,.735f),new Vector2(.935f,.87f));
            banner=UiFactory.Text("Steps",plaque.transform,font,"",30,UiFactory.Cream);UiFactory.Anchor(banner.rectTransform,new Vector2(.07f,.13f),new Vector2(.93f,.88f));
            banner.textWrappingMode=TextWrappingModes.Normal;banner.enableAutoSizing=true;banner.fontSizeMin=22;banner.fontSizeMax=30;
            next=UiFactory.Button("Next",rect,font,"CONTINUE",28,panel,UiFactory.Green);UiFactory.Anchor((RectTransform)next.transform,new Vector2(.22f,.64f),new Vector2(.78f,.70f));
            hand=UiFactory.Rect("HandPointer",rect);hand.sizeDelta=new Vector2(70,90);hand.anchorMin=hand.anchorMax=rect.pivot;
            var graphic=hand.gameObject.AddComponent<TutorialHandGraphic>();graphic.color=UiFactory.Cream;graphic.raycastTarget=false;
        }
        public void Initialize(EventBus events,BoardView cards,RectTransform scoreTarget,RectTransform current,RectTransform deck,RectTransform chainTarget)
        {
            rect=(RectTransform)transform;bus=events;board=cards;score=scoreTarget;waste=current;draw=deck;chain=chainTarget;
            ApplyBannerContrast();next.onClick.AddListener(Advance);group.alpha=1;
        }
        public void ApplyBannerContrast()
        {
            var plaque=(RectTransform)banner.transform.parent;var image=plaque.GetComponent<UnityEngine.UI.Image>();image.sprite=null;image.color=new Color(0,0,0,230/255f);
            for(int i=0;i<4;i++)
            {
                var edge=UiFactory.Rect("GoldTrim-"+i,plaque);
                UiFactory.Anchor(edge,i==0?Vector2.zero:i==1?new Vector2(0,1):i==2?Vector2.zero:new Vector2(1,0),i<2?new Vector2(1,i):i==2?new Vector2(0,1):Vector2.one);
                edge.sizeDelta=i<2?new Vector2(0,3):new Vector2(3,0);UiFactory.Image(edge,UiFactory.Gold);
            }
            banner.transform.SetAsLastSibling();
        }
        private void OnDestroy(){if(next!=null)next.onClick.RemoveListener(Advance);}
        private void Advance()
        {if(fading||snapshot==null||!snapshot.TutorialActive)return;bus.Send(new GameCommand(Stage==TutorialStage.Complete?GameCommandKind.TutorialComplete:GameCommandKind.TutorialNext));}
        public void Render(GameSnapshot state)
        {
            snapshot=state;
            if(state.Menu!=MenuMode.Playing){gameObject.SetActive(false);return;}
            if(!state.TutorialActive)
            {
                if(wasGuiding&&gameObject.activeSelf){wasGuiding=false;fading=true;fade=0;next.gameObject.SetActive(false);hand.gameObject.SetActive(false);Shield();}
                else if(!fading)gameObject.SetActive(false);
                return;
            }
            bool changed=!wasGuiding||Stage!=state.TutorialStage;
            wasGuiding=true;fading=false;group.alpha=1;gameObject.SetActive(true);transform.SetAsLastSibling();
            Stage=state.TutorialStage;if(changed)age=0;
            switch(Stage)
            {
                case TutorialStage.Welcome:banner.text="1 / 5  WELCOME TO CURSED DECK!\nReach the Target Score before running out of draws.";break;
                case TutorialStage.Match:banner.text="2 / 5  YOUR FIRST MATCH\nTap a card that is ONE RANK HIGHER or LOWER than the current card.";break;
                case TutorialStage.Chain:banner.text="3 / 5  CHAIN 1!\nPlaying cards in a row builds a CHAIN. Higher chains MULTIPLY your score!";break;
                case TutorialStage.Draw:banner.text="4 / 5  THE DRAW DECK\nNo moves available on the board? Tap the DRAW DECK to reveal a new card.";break;
                default:banner.text="5 / 5  YOU'VE MASTERED THE BASICS!\nDefeat the 3 Dealers and clear the casino.";break;
            }
            bool acknowledge=Stage==TutorialStage.Welcome||Stage==TutorialStage.Chain||Stage==TutorialStage.Complete;
            next.gameObject.SetActive(acknowledge);next.GetComponentInChildren<TextMeshProUGUI>().text=Stage==TutorialStage.Complete?"ENTER CASINO":"CONTINUE";
            Focus();
        }
        private void Update()=>StepVisuals(Time.unscaledDeltaTime);
        public void StepVisuals(float delta)
        {
            if(fading){fade+=delta;group.alpha=1-Mathf.Clamp01(fade/.35f);if(fade>=.35f){fading=false;gameObject.SetActive(false);}return;}
            if(!wasGuiding)return;
            bool reduced=snapshot.Settings.ReduceMotion;age+=delta;float pulse=reduced?1:.72f+.28f*Mathf.Sin(age*3);
            for(int i=0;i<borders.Length;i++){var tint=UiFactory.Gold;tint.a=pulse;borders[i].color=tint;}
            if(hand.gameObject.activeSelf)
            {
                float t=reduced?0:Mathf.Repeat(age,1.6f)/1.2f;t=Mathf.SmoothStep(0,1,Mathf.Clamp01(t));
                hand.anchoredPosition=Vector2.Lerp(handFrom,handTo,t);hand.localScale=Vector3.one*(1+(reduced?0:.035f*Mathf.Sin(age*6)));
            }
        }
        private void OnRectTransformDimensionsChange(){if(bus!=null&&snapshot!=null&&snapshot.TutorialActive&&!fading)Focus();}
        private void Shield(){regionCount=1;regions[0]=rect.rect;for(int i=0;i<borders.Length;i++)borders[i].gameObject.SetActive(false);ApplyMasks();}
        private void Focus()
        {
            bool center=Stage==TutorialStage.Welcome||Stage==TutorialStage.Complete;
            UiFactory.Anchor((RectTransform)banner.transform.parent,new Vector2(.065f,center?.52f:.735f),new Vector2(.935f,center?.665f:.87f));
            UiFactory.Anchor((RectTransform)next.transform,new Vector2(.22f,center?.43f:.64f),new Vector2(.78f,center?.49f:.70f));
            RectTransform primary=null,secondary=null;
            if(Stage==TutorialStage.Welcome)primary=score;
            else if(Stage==TutorialStage.Match){primary=board.CardAt(snapshot.TutorialSlot)?.Rect;secondary=waste;}
            else if(Stage==TutorialStage.Chain)primary=chain;
            else if(Stage==TutorialStage.Draw)primary=draw;
            regionCount=1;regions[0]=rect.rect;
            for(int i=0;i<borders.Length;i++)borders[i].gameObject.SetActive(false);
            // Informational targets have no action; the Run gate rejects all unrelated commands.
            if(primary!=null){var r=Bounds(primary);Border(0,r);Cut(r);handFrom=r.center;}
            if(secondary!=null){var r=Bounds(secondary);Border(4,r);Cut(r);handTo=r.center;}
            hand.gameObject.SetActive(Stage==TutorialStage.Match);handFrom+=new Vector2(12,-45);handTo+=new Vector2(12,-45);
            if(hand.gameObject.activeSelf)hand.anchoredPosition=handFrom;
            ApplyMasks();
        }
        private Rect Bounds(RectTransform target)
        {target.GetWorldCorners(corners);var a=rect.InverseTransformPoint(corners[0]);var b=rect.InverseTransformPoint(corners[2]);return Rect.MinMaxRect(a.x-8,a.y-8,b.x+8,b.y+8);}
        private void Position(RectTransform target,Rect r)
        {target.anchorMin=target.anchorMax=rect.pivot;target.pivot=Vector2.zero;target.anchoredPosition=r.position;target.sizeDelta=r.size;}
        private void Border(int start,Rect r)
        {
            for(int i=start;i<start+4;i++)borders[i].gameObject.SetActive(true);
            Position(borders[start].rectTransform,new Rect(r.xMin,r.yMin,r.width,4));Position(borders[start+1].rectTransform,new Rect(r.xMin,r.yMax-4,r.width,4));
            Position(borders[start+2].rectTransform,new Rect(r.xMin,r.yMin,4,r.height));Position(borders[start+3].rectTransform,new Rect(r.xMax-4,r.yMin,4,r.height));
        }
        private void ApplyMasks(){for(int i=0;i<masks.Length;i++){masks[i].gameObject.SetActive(i<regionCount);if(i<regionCount)Position(masks[i].rectTransform,regions[i]);}}
        private void Cut(Rect hole)
        {
            int count=0;
            for(int i=0;i<regionCount;i++)
            {
                var r=regions[i];if(!r.Overlaps(hole)){scratch[count++]=r;continue;}
                float left=Mathf.Max(r.xMin,hole.xMin),right=Mathf.Min(r.xMax,hole.xMax),bottom=Mathf.Max(r.yMin,hole.yMin),top=Mathf.Min(r.yMax,hole.yMax);
                Add(ref count,new Rect(r.xMin,r.yMin,left-r.xMin,r.height));Add(ref count,new Rect(right,r.yMin,r.xMax-right,r.height));
                Add(ref count,new Rect(left,r.yMin,right-left,bottom-r.yMin));Add(ref count,new Rect(left,top,right-left,r.yMax-top));
            }
            regionCount=count;for(int i=0;i<count;i++)regions[i]=scratch[i];
        }
        private void Add(ref int count,Rect r){if(r.width>0&&r.height>0)scratch[count++]=r;}
    }
}
