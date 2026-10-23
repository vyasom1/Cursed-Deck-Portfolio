using UnityEngine;
using TMPro;
using CursedDeck.Run;
namespace CursedDeck.View
{
    public sealed partial class GameView
    {
        private RectTransform recoveryPanel;
        public bool RecoveryVisible=>recoveryPanel!=null&&recoveryPanel.gameObject.activeSelf;
        public void ShowRecovery(string detail)
        {
            if(recoveryPanel==null)
            {
                recoveryPanel=Panel("SaveRecovery");
                var frame=UIPackLayout.Panel("Frame",recoveryPanel,skin!=null?skin.ResourcePanel:null,new Vector2(.05f,.25f),new Vector2(.95f,.76f));
                var title=UiFactory.Text("Title",frame.transform,font,"SAVE DATA RECOVERED",42,UiFactory.Gold);UiFactory.Anchor(title.rectTransform,new Vector2(.06f,.74f),new Vector2(.94f,.94f));
                var text=UiFactory.Text("Detail",frame.transform,font,"",31,UiFactory.Cream);UiFactory.Anchor(text.rectTransform,new Vector2(.09f,.40f),new Vector2(.91f,.75f));text.textWrappingMode=TextWrappingModes.Normal;
                var keep=UiFactory.Button("Continue",frame.transform,font,"CONTINUE",29,front,UiFactory.Green);UiFactory.Place((RectTransform)keep.transform,.5f,.28f,740,95);keep.onClick.AddListener(DismissRecovery);
                var reset=UiFactory.Button("ResetProgress",frame.transform,font,"RESET PROGRESS",26,front,UiFactory.Gold);UiFactory.Place((RectTransform)reset.transform,.5f,.10f,740,85);reset.onClick.AddListener(()=>bus.Send(new GameCommand(GameCommandKind.ResetProgress)));
            }
            recoveryPanel.Find("Frame/Detail").GetComponent<TextMeshProUGUI>().text=detail;
            recoveryPanel.gameObject.SetActive(true);recoveryPanel.SetAsLastSibling();
        }
        public void DismissRecovery(){if(recoveryPanel!=null)recoveryPanel.gameObject.SetActive(false);}
    }
}
