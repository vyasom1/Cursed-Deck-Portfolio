using CursedDeck.Run;
using TMPro;
using UnityEngine;
namespace CursedDeck.View
{
    public sealed partial class GameView
    {
        private RectTransform metaPanel;
        private TextMeshProUGUI metaSummary,metaDaily,metaNotice;
        private readonly TextMeshProUGUI[] unlockLabels=new TextMeshProUGUI[6];
        private UnityEngine.UI.Button redStart;
        private bool redUnlocked;
        private void BuildMeta()
        {
            metaPanel=Panel("MetaPanel");
            metaSummary=UiFactory.Text("Stats",metaPanel,font,"SOUL SHARDS",34,UiFactory.Gold);UiFactory.Anchor(metaSummary.rectTransform,new Vector2(.04f,.77f),new Vector2(.96f,.96f));
            metaDaily=UiFactory.Text("Daily",metaPanel,font,"",25,UiFactory.Muted);UiFactory.Anchor(metaDaily.rectTransform,new Vector2(.04f,.72f),new Vector2(.96f,.78f));
            var kinds=new[]{GameCommandKind.MetaClassic,GameCommandKind.MetaRed,GameCommandKind.MetaDaily};var names=new[]{"CLASSIC","RED DECK","DAILY"};
            for(int i=0;i<3;i++){var k=kinds[i];var button=UiFactory.Button(names[i],metaPanel,font,names[i],25,front,UiFactory.Green);UiFactory.Place((RectTransform)button.transform,.19f+.31f*i,.67f,310,100);button.onClick.AddListener(()=>{bus.Send(new GameCommand(k));metaPanel.gameObject.SetActive(false);});if(i==1)redStart=button;}
            for(int i=0;i<6;i++){int index=i;var button=UiFactory.Button("Unlock-"+i,metaPanel,font,"",25,front,UiFactory.Green);UiFactory.Place((RectTransform)button.transform,.5f,.575f-i*.067f,900,105);unlockLabels[i]=button.GetComponentInChildren<TextMeshProUGUI>();button.onClick.AddListener(()=>bus.Send(new GameCommand(GameCommandKind.MetaUnlock,id:CursedDeck.Meta.MetaUnlocks.Ids[index])));}
            var share=UiFactory.Button("ShareDaily",metaPanel,font,"COPY DAILY RESULT",27,front,UiFactory.Green);UiFactory.Place((RectTransform)share.transform,.5f,.12f,900,100);share.onClick.AddListener(()=>bus.Send(new GameCommand(GameCommandKind.MetaShare)));
            metaNotice=UiFactory.Text("Notice",metaPanel,font,"",24,UiFactory.Gold);UiFactory.Anchor(metaNotice.rectTransform,new Vector2(.03f,.145f),new Vector2(.97f,.19f));
            var close=UiFactory.Button("Close",metaPanel,font,"BACK",27,front,UiFactory.Green);UiFactory.Place((RectTransform)close.transform,.5f,.045f,900,85);close.onClick.AddListener(()=>metaPanel.gameObject.SetActive(false));metaPanel.gameObject.SetActive(false);
            StatsButton.onClick.AddListener(()=>{metaPanel.gameObject.SetActive(true);metaPanel.SetAsLastSibling();bus.Send(new GameCommand(GameCommandKind.MetaRefresh));});
            bus.MetaChanged+=RenderMeta;
        }
        private void RenderMeta(MetaPresentation state){metaSummary.text=state.Summary;metaDaily.text=state.Daily;metaNotice.text=state.Notice??"Starting a new run requires a fresh or finished checkpoint.";redUnlocked=state.RedUnlocked;redStart.interactable=state.RedUnlocked&&(previous==null||previous.Menu==MenuMode.Playing);for(int i=0;i<6;i++)unlockLabels[i].text=state.UnlockLabels[i];}
    }
}
