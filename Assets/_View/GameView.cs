using TMPro;
using UnityEngine;
using CursedDeck.Services;
using CursedDeck.Run;

namespace CursedDeck.View
{
    public sealed partial class GameView : MonoBehaviour
    {
        private EventBus bus;
        private TMP_FontAsset font;
        private Sprite front,back;
        private Sprite[] suitIcons;
        private UnityEngine.UI.Image wasteIcon;
        private RectTransform safe,table,drawRect,wasteRect,modal,settingsPanel;
        private UnityEngine.UI.Button draw,undo,peek,shuffle,reroll;
        private TextMeshProUGUI drawLabel,wasteRank,wasteSuit,undoLabel,peekLabel,shuffleLabel,notice;
        private HUD hud;
        private TweenRunner tweens;
        private GameSnapshot previous;
        private bool initialized;
        public BoardView Board { get; private set; }
        public GameSnapshot LastSnapshot => previous;
        public UnityEngine.UI.Button DrawButton => draw;
        public RectTransform SafeRoot => safe;
        public bool ResultVisible => modal!=null && modal.gameObject.activeSelf;
        public EventBus Events => bus;

        public void Initialize(EventBus events,TMP_FontAsset typeface,Sprite cardFront,Sprite cardBack,Sprite[] icons=null,CardView cardPrefab=null,CardCharacterVisual[] characters=null)
        {
            if(initialized) throw new System.InvalidOperationException("View already initialized.");
            initialized=true; bus=events; font=typeface; front=cardFront; back=cardBack;
            suitIcons=icons;
            BuildMainScreen(typeface,cardFront,cardBack);
            var settings=safe.Find("HeaderBar/SettingsButton").GetComponent<UnityEngine.UI.Button>();settings.onClick.AddListener(OpenPause);
            draw.onClick.AddListener(Draw);reroll.onClick.AddListener(Reroll);undo.onClick.AddListener(Undo);peek.onClick.AddListener(Peek);shuffle.onClick.AddListener(Shuffle);
            tweens=gameObject.AddComponent<TweenRunner>();
            Board=table.gameObject.AddComponent<BoardView>();Board.Initialize(bus,font,front,back,tweens,suitIcons,cardPrefab,characters);Board.SetWasteTarget(wasteRect);
            BuildCardInspection(characters);
            BuildModal(); BuildSettings(); BuildMeta(); BuildPolish(); BuildOwnedRelics(); BindMenus();
#if UNITY_EDITOR || CURSED_DECK_DEBUG
            BuildDebug();
#endif
            var particleRect=UiFactory.Rect("CardParticles",safe);UiFactory.Anchor(particleRect,Vector2.zero,Vector2.one);
            particleRect.SetSiblingIndex(table.GetSiblingIndex()+1);
            Particles=particleRect.gameObject.AddComponent<CardParticleFX>();Particles.Initialize();Board.SetParticleFX(Particles);
            BuildGameFeel();bus.GameChanged+=Render;
        }
        private void OnDestroy() { if(bus!=null){bus.GameChanged-=Render;bus.CardInspectionRequested-=InspectCard;} if(bus!=null)bus.MetaChanged-=RenderMeta; }
        private void Draw() => bus.Send(new GameCommand(GameCommandKind.Draw));
        private void Reroll() => bus.Send(new GameCommand(GameCommandKind.Reroll));
        private void Undo() => bus.Send(new GameCommand(GameCommandKind.Undo));
        private void Peek() => bus.Send(new GameCommand(GameCommandKind.Peek));
        private void Shuffle() => bus.Send(new GameCommand(GameCommandKind.Shuffle));
        public CardParticleFX Particles {get;private set;}
        private void Render(GameSnapshot state)
        {
            HapticService.Enabled=state.Settings.Haptics;if(HapticService.Enabled)HapticService.Prepare();
            bool deal=previous==null || previous.Floor!=state.Floor || previous.Round!=state.Round || previous.BoardSeed!=state.BoardSeed || state.Feedback==GameFeedback.Deal;
            ApplyMotionPreference(state.Settings.ReduceMotion);
            if(deal||state.Feedback==GameFeedback.Undo){Particles.ResetPool();ScorePopups.ResetPool();}
            bool accepted=state.Feedback==GameFeedback.Play&&!deal;
            float gained=accepted?(float)(state.Score-(previous?.Score??0)):0;
            bool chainIncrement=accepted&&state.Chain>(previous?.Chain??0);
            Board.Render(state,deal); hud.Render(state,accepted&&!state.Settings.ReduceMotion);
            if(accepted)ScorePopups.Spawn(Board.LastPlayWorldPosition,gained,state.Chain);
            draw.interactable=state.CanDraw; drawLabel.SetText("DRAW\n{0} IN STOCK",state.Stock);
            wasteRank.text=CardView.RankLabel(state.WasteRank); wasteSuit.text=state.Settings.Colorblind || suitIcons==null ? CardView.SuitLabel(state.WasteSuit,state.Settings.Colorblind) : "";
            wasteSuit.fontSize=state.Settings.Colorblind ? 25 : 67;
            UiFactory.Anchor(wasteSuit.rectTransform,new Vector2(.05f,.04f),new Vector2(.95f,.22f));
            wasteIcon.enabled=suitIcons!=null && suitIcons.Length==4; if(wasteIcon.enabled) wasteIcon.sprite=skin!=null?skin.Suits[state.WasteSuit]:suitIcons[state.WasteSuit];
            Color ink=state.WasteSuit==1 || state.WasteSuit==2 ? new Color(.67f,.16f,.22f) : UiFactory.Green; wasteRank.color=wasteSuit.color=ink;
            wasteIcon.color=skin!=null?Color.white:ink;if(skin!=null)wasteIcon.material=skin.SuitMaterials[state.WasteSuit];
            RenderCurrentArt(state);
            PositionControls(state.Settings.LeftHanded);
            reroll.gameObject.SetActive(state.CanReroll);
            undoLabel.SetText("UNDO  {0}",state.UndoCount); peekLabel.SetText("PEEK  {0}",state.PeekCount); shuffleLabel.SetText("SHUFFLE  {0}",state.ShuffleCount);
            undo.interactable=state.CanUndo; peek.interactable=state.Stage==RunStage.Playing && state.PeekCount>0; shuffle.interactable=state.Stage==RunStage.Playing && state.ShuffleCount>0 && state.Stock>1;
            notice.text=state.Notice ?? "";
            previous=state; RenderOwnedRelics(state); RenderPolish(state); RenderModal(state); RenderSettings(state.Settings); RenderMenus(state);
            if(state.Feedback==GameFeedback.Play)
            {
                if(chainIncrement)tweens.Pulse(hud.ChainRect);
                if(state.Chain>=5&&!state.Settings.ReduceMotion) tweens.Shake(table);

            }
            else if(state.Feedback==GameFeedback.Draw) tweens.Pulse(wasteRect,1.08f);
        }
    }
}
