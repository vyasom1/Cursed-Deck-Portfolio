using UnityEngine;
using TMPro;
using CursedDeck.Run;
namespace CursedDeck.View
{
    public sealed partial class GameView
    {
        private RectTransform tutorial;
        private TextMeshProUGUI serviceNotice;
        
#if UNITY_EDITOR || CURSED_DECK_DEBUG
        private UnityEngine.UI.Button testDraw,testRevive;
#endif
        private void BuildPolish()
        {
#if UNITY_EDITOR || CURSED_DECK_DEBUG
            testDraw=UiFactory.Button("TestAdDraw",safe.Find("BottomControlPanel"),font,"TEST AD: +1 DRAW",18,front,UiFactory.Green);UiFactory.Place((RectTransform)testDraw.transform,.71f,.18f,380,36);testDraw.onClick.AddListener(()=>bus.Send(new GameCommand(GameCommandKind.TestAdDraw)));
            testRevive=UiFactory.Button("TestAdRevive",modal,font,"TEST AD: REVIVE",28,front,UiFactory.Green);UiFactory.Place((RectTransform)testRevive.transform,.5f,.27f,850,100);testRevive.onClick.AddListener(()=>bus.Send(new GameCommand(GameCommandKind.TestAdRevive)));testRevive.gameObject.SetActive(false);
            var mock=UiFactory.Button("MockPurchase",settingsPanel,font,"MOCK IAP: NO PAYMENT",22,front,UiFactory.Green);UiFactory.Place((RectTransform)mock.transform,.5f,.112f,850,40);mock.onClick.AddListener(()=>bus.Send(new GameCommand(GameCommandKind.MockPurchase)));
#endif
            serviceNotice=UiFactory.Text("ServiceNotice",settingsPanel,font,"",25,UiFactory.Gold);UiFactory.Anchor(serviceNotice.rectTransform,new Vector2(.05f,.70f),new Vector2(.95f,.76f));
            BuildInteractiveTutorial();
        }
        private void RenderPolish(GameSnapshot snapshot){
#if UNITY_EDITOR || CURSED_DECK_DEBUG
serviceNotice.text=snapshot.Notice??"Demo purchases take no payment.";UiFactory.Place((RectTransform)testDraw.transform,snapshot.Settings.LeftHanded?.27f:.71f,.18f,380,36);testDraw.interactable=snapshot.CanRewardDraw;testDraw.gameObject.SetActive(snapshot.Stage==RunStage.Playing);testRevive.gameObject.SetActive(snapshot.Stage==RunStage.Lost&&snapshot.CanRevive);
#endif
            RenderTutorial(snapshot);
        }
    }
}
