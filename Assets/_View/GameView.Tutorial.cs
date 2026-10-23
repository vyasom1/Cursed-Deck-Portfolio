using UnityEngine;
using CursedDeck.Run;
namespace CursedDeck.View
{
    public sealed partial class GameView
    {
        private TutorialManager tutorialManager;
        public TutorialManager Tutorial=>tutorialManager;
        public int TutorialStep=>tutorialManager!=null?(int)tutorialManager.Stage:0;
        private void BuildInteractiveTutorial()
        {
            var prefab=Resources.Load<TutorialManager>("TutorialOverlay");
            if(prefab==null)throw new System.InvalidOperationException("TutorialOverlay.prefab missing. Run Tools > Cursed Deck > Prepare Guided Tutorial.");
            tutorialManager=Instantiate(prefab,safe,false);tutorialManager.name="Tutorial";tutorial=(RectTransform)tutorialManager.transform;
            tutorialManager.Initialize(bus,Board,hud.ScoreRect,wasteRect,drawRect,hud.ChainRect);
            tutorial.gameObject.SetActive(false);
        }
        public void ShowTutorial()=>bus.Send(new GameCommand(GameCommandKind.StartTutorial));
        private void RenderTutorial(GameSnapshot state)=>tutorialManager.Render(state);
        private void UpdateTutorialFocus(GameSnapshot state)=>tutorialManager.Render(state);
    }
}
