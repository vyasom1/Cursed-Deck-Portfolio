using TMPro;
using UnityEngine;
using CursedDeck.Run;

namespace CursedDeck.View
{
    public sealed partial class GameView
    {
        private TextMeshProUGUI resultTitle,resultDetail,continueLabel;
        private UnityEngine.UI.Button continueButton,resultUndo;
        private readonly OptionRow[] optionRows=new OptionRow[6];
        private readonly TextMeshProUGUI[] settingsLabels=new TextMeshProUGUI[5];
        public string ResultTitle => resultTitle.text;
        public UnityEngine.UI.Button ContinueButton => continueButton;
        public UnityEngine.UI.Button OfferButton(int index) => optionRows[index].Button;
        private RectTransform Panel(string name)
        {
            var panel=UiFactory.Rect(name,safe); UiFactory.Anchor(panel,Vector2.zero,Vector2.one);
            UiFactory.Image(panel,new Color(0,0,0,217/255f),null,true); return panel;
        }
        private void BuildModal()
        {
            modal=Panel("Result");
            var crest=UiFactory.Text("Crest",modal,font,"THE HAUNTED CASINO",23,UiFactory.Gold); crest.characterSpacing=5;
            UiFactory.Anchor(crest.rectTransform,new Vector2(.07f,.92f),new Vector2(.93f,.96f));
            resultTitle=UiFactory.Text("ResultTitle",modal,font,"",67,UiFactory.Cream);
            UiFactory.Anchor(resultTitle.rectTransform,new Vector2(.04f,.81f),new Vector2(.96f,.91f));
            resultDetail=UiFactory.Text("Detail",modal,font,"",31,UiFactory.Muted);
            UiFactory.Anchor(resultDetail.rectTransform,new Vector2(.075f,.73f),new Vector2(.925f,.82f));
            for(int i=0;i<optionRows.Length;i++) optionRows[i]=new OptionRow(i,modal,font,front,bus);
            continueButton=UiFactory.Button("Continue",modal,font,"CONTINUE",36,front,new Color(.20f,.35f,.28f));
            UiFactory.Place((RectTransform)continueButton.transform,.5f,.065f,850,130); continueButton.onClick.AddListener(Continue);
            continueLabel=continueButton.GetComponentInChildren<TextMeshProUGUI>();
            resultUndo=UiFactory.Button("ResultUndo",modal,font,"USE UNDO",34,front,UiFactory.Green);
            UiFactory.Place((RectTransform)resultUndo.transform,.5f,.17f,850,120); resultUndo.onClick.AddListener(Undo);
            modal.gameObject.SetActive(false);
        }
        private void Continue()
        {
            if(previous.Stage==RunStage.Shop || previous.Stage==RunStage.Reward) bus.Send(new GameCommand(GameCommandKind.Continue));
            else bus.Send(new GameCommand(GameCommandKind.NewRun));
        }
        private void RenderModal(GameSnapshot state)
        {
            bool visible=state.Stage!=RunStage.Playing;
            modal.gameObject.SetActive(visible);
            for(int i=0;i<optionRows.Length;i++) optionRows[i].SetActive(false);
            if(!visible) return;
            modal.SetAsLastSibling();
            bool shop=state.Stage==RunStage.Shop;
            resultUndo.gameObject.SetActive(state.Stage==RunStage.Lost && state.CanUndo);
            if(state.Stage==RunStage.Reward)
            {
                resultTitle.text="ROUND WON";
                resultDetail.text=state.Offers.Count==0?"+"+state.Award+" GOLD\nAll eligible relics owned. Claim "+state.FallbackRewardGold+" bonus gold.":"+"+state.Award+" GOLD     ·     CHOOSE A RELIC\nA little luck. A new way to play.";
            }
            else if(shop) { resultTitle.text="BETWEEN FLOORS"; resultDetail.text="GOLD  "+state.Gold+"\nSpend wisely. The next Dealer is waiting."; }
            else if(state.Stage==RunStage.Won) { resultTitle.text="RUN WON"; resultDetail.text="Three floors cleared. You beat the house.\nGOLD  "+state.Gold+"     ·     "+state.RelicCount+" RELICS"; }
            else { resultTitle.text="ROUND LOST"; resultDetail.text="The Dealer keeps this hand.\nSCORE  "+state.Score+"  /  "+state.Target; }
            for(int i=0;i<state.Offers.Count && i<optionRows.Length;i++)
            {
                float y=shop ? .66f-i*.095f : .61f-i*.16f;
                optionRows[i].Bind(state.Offers[i],shop,y,shop ? 150 : 215);
            }
            bool canContinue=shop || state.Stage==RunStage.Lost || state.Stage==RunStage.Won || (state.Stage==RunStage.Reward && state.Offers.Count==0);
            continueButton.gameObject.SetActive(canContinue);
            continueLabel.text=shop ? "LEAVE SHOP" : state.Stage==RunStage.Reward ? "CLAIM "+state.FallbackRewardGold+" GOLD" : "NEW RUN";
        }
        private sealed class OptionRow
        {
            private readonly EventBus bus;
            private string id;
            private bool shop;
            private readonly RectTransform rect;
            private readonly TextMeshProUGUI title,body,price;
            public UnityEngine.UI.Button Button { get; }
            public OptionRow(int index,Transform parent,TMP_FontAsset font,Sprite sprite,EventBus bus)
            {
                this.bus=bus;
                Button=UiFactory.Button("Offer-"+index,parent,font,"",1,sprite,UiFactory.Green); rect=(RectTransform)Button.transform;
                Button.GetComponentInChildren<TextMeshProUGUI>().gameObject.SetActive(false);
                title=UiFactory.Text("Title",rect,font,"",39,UiFactory.Gold,TextAlignmentOptions.Left);
                UiFactory.Anchor(title.rectTransform,new Vector2(.05f,.57f),new Vector2(.78f,.94f));
                body=UiFactory.Text("Description",rect,font,"",28,UiFactory.Cream,TextAlignmentOptions.Left);
                UiFactory.Anchor(body.rectTransform,new Vector2(.05f,.08f),new Vector2(.94f,.58f));
                price=UiFactory.Text("Price",rect,font,"",31,UiFactory.Gold,TextAlignmentOptions.Right);
                UiFactory.Anchor(price.rectTransform,new Vector2(.79f,.58f),new Vector2(.95f,.94f));
                Button.onClick.AddListener(Choose);
            }
            private void Choose() => bus.Send(new GameCommand(shop ? GameCommandKind.Buy : GameCommandKind.ChooseReward,id:id));
            public void SetActive(bool active) => rect.gameObject.SetActive(active);
            public void Bind(OfferUiState offer,bool isShop,float y,float height)
            {
                id=offer.Id; shop=isShop; SetActive(true); UiFactory.Place(rect,.5f,y,920,height);
                title.text=offer.Title; body.text=offer.Detail; price.text=isShop ? offer.Price+" G" : "";
                Button.interactable=offer.Available;
            }
        }
        private void BuildSettings()
        {
            settingsPanel=Panel("SettingsPanel");
            var title=UiFactory.Text("Title",settingsPanel,font,"YOUR TABLE",63,UiFactory.Gold);
            UiFactory.Anchor(title.rectTransform,new Vector2(.05f,.84f),new Vector2(.95f,.94f));
            var subtitle=UiFactory.Text("Subtitle",settingsPanel,font,"Make room for your hand.",30,UiFactory.Muted);
            UiFactory.Anchor(subtitle.rectTransform,new Vector2(.05f,.77f),new Vector2(.95f,.84f));
            GameCommandKind[] kinds={GameCommandKind.ToggleSound,GameCommandKind.ToggleHaptics,GameCommandKind.ToggleLeftHanded,GameCommandKind.ToggleColorblind,GameCommandKind.ToggleReduceMotion};
            for(int i=0;i<5;i++)
            {
                var button=UiFactory.Button("Setting-"+i,settingsPanel,font,"",37,front,UiFactory.Green);
                UiFactory.Anchor((RectTransform)button.transform,new Vector2(.12f,.61f-i*.095f),new Vector2(.88f,.69f-i*.095f));
                var kind=kinds[i]; button.onClick.AddListener(()=>bus.Send(new GameCommand(kind)));
                settingsLabels[i]=button.GetComponentInChildren<TextMeshProUGUI>();
            }
            settingsTutorial=MenuButton("ReplayTutorial",settingsPanel,"REPLAY TUTORIAL",.17f,.07f);settingsTutorial.onClick.AddListener(ReplayTutorial);
            var close=UiFactory.Button("Close",settingsPanel,font,"BACK",34,front,UiFactory.Green);
            UiFactory.Anchor((RectTransform)close.transform,new Vector2(.12f,.025f),new Vector2(.88f,.085f)); close.onClick.AddListener(CloseSettings);
            settingsPanel.gameObject.SetActive(false);
        }
        private void RenderSettings(UiSettings settings)
        {
            settingsLabels[0].text="SOUND     "+(settings.Sound ? "ON" : "OFF");
            settingsLabels[1].text="HAPTICS     "+(settings.Haptics ? "ON" : "OFF");
            settingsLabels[2].text="LEFT-HANDED     "+(settings.LeftHanded ? "ON" : "OFF");
            settingsLabels[4].text="REDUCE MOTION     "+(settings.ReduceMotion ? "ON" : "OFF");
            settingsLabels[3].text="SUIT LABELS     "+(settings.Colorblind ? "ON" : "OFF");
        }
        public void OpenSettings() { settingsPanel.gameObject.SetActive(true);settingsPanel.SetAsLastSibling(); }
        public void CloseSettings() { settingsPanel.gameObject.SetActive(false); }
#if UNITY_EDITOR || CURSED_DECK_DEBUG
        private void BuildDebug()
        {
            var panel=Panel("DebugPanel");
            var title=UiFactory.Text("Title",panel,font,"DEVELOPMENT TABLE",52,UiFactory.Gold);
            UiFactory.Anchor(title.rectTransform,new Vector2(.04f,.84f),new Vector2(.96f,.94f));
            var note=UiFactory.Text("Note",panel,font,"Debug changes redeal the current board.",27,UiFactory.Muted);
            UiFactory.Anchor(note.rectTransform,new Vector2(.04f,.77f),new Vector2(.96f,.84f));
            var names=new[] {"REGENERATE BOARD","ADD 100 GOLD + REDEAL","ADD RELIC + REDEAL"};
            var kinds=new[] {GameCommandKind.DebugRegenerate,GameCommandKind.DebugGold,GameCommandKind.DebugRelic};
            for(int i=0;i<3;i++)
            {
                var button=UiFactory.Button("Debug-"+i,panel,font,names[i],30,front,UiFactory.Green);
                UiFactory.Place((RectTransform)button.transform,.5f,.69f-i*.105f,850,130);
                var kind=kinds[i]; button.onClick.AddListener(()=>{ bus.Send(new GameCommand(kind)); panel.gameObject.SetActive(false); });
            }
            for(int i=0;i<9;i++)
            {
                int index=i;
                var button=UiFactory.Button("Jump-"+i,panel,font,"F"+(i/3+1)+" · R"+(i%3+1),31,front,UiFactory.Green);
                UiFactory.Place((RectTransform)button.transform,.23f+(i%3)*.27f,.35f-(i/3)*.09f,255,115);
                button.onClick.AddListener(()=>{ bus.Send(new GameCommand(GameCommandKind.DebugJump,index)); panel.gameObject.SetActive(false); });
            }
            var close=UiFactory.Button("Close",panel,font,"BACK",34,front,UiFactory.Green); UiFactory.Place((RectTransform)close.transform,.5f,.06f,850,125);
            close.onClick.AddListener(()=>panel.gameObject.SetActive(false)); panel.gameObject.SetActive(false);
            var open=UiFactory.Button("Debug",safe,font,"DEV",20,front,UiFactory.Green);
            UiFactory.Place((RectTransform)open.transform,.05f,.694f,90,44); open.GetComponentInChildren<TextMeshProUGUI>().margin=new Vector4(4,2,4,2);open.onClick.AddListener(()=>{panel.gameObject.SetActive(true);panel.SetAsLastSibling();});
        }
#endif
    }
}
