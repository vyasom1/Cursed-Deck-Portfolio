using UnityEngine;
using CursedDeck.Run;
namespace CursedDeck.View
{
    public sealed partial class GameView
    {
        private CardCharacterVisual[] characterVisuals;
        private RectTransform cardInspection;private UnityEngine.UI.Image inspectedArt,currentArt;
        private TMPro.TextMeshProUGUI inspectCaption;
        public bool CardInspectionVisible=>cardInspection!=null&&cardInspection.gameObject.activeSelf;
        private void BuildCardInspection(CardCharacterVisual[] characters)
        {
            characterVisuals=characters;
            currentArt=UiFactory.Image(UiFactory.Rect("CharacterPortrait",wasteRect),Color.white);UiFactory.Anchor(currentArt.rectTransform,new Vector2(.065f,.065f),new Vector2(.935f,.935f));currentArt.preserveAspect=true;currentArt.transform.SetAsFirstSibling();
            CardReadability.Rank(wasteRank);wasteRank.fontSize=36;wasteRank.color=UiFactory.Cream;
            var hold=wasteRect.GetComponent<CardLongPress>();if(hold==null)hold=wasteRect.gameObject.AddComponent<CardLongPress>();hold.Initialize(bus,()=>-1);hold.CanInspect=true;
            wasteRect.GetComponent<UnityEngine.UI.Image>().raycastTarget=true;
            cardInspection=UiFactory.Rect("CardInspection",safe);UiFactory.Anchor(cardInspection,Vector2.zero,Vector2.one);UiFactory.Image(cardInspection,new Color(0,0,0,.90f),null,true);
            inspectedArt=UiFactory.Image(UiFactory.Rect("FullCharacterArt",cardInspection),Color.white);UiFactory.Anchor(inspectedArt.rectTransform,new Vector2(.15f,.23f),new Vector2(.85f,.86f));inspectedArt.preserveAspect=true;
            inspectCaption=UiFactory.Text("CharacterName",cardInspection,font,"",30,UiFactory.Gold);UiFactory.Anchor(inspectCaption.rectTransform,new Vector2(.08f,.14f),new Vector2(.92f,.23f));
            var close=UiFactory.Button("Close",cardInspection,font,"CLOSE",28,front,UiFactory.Green);UiFactory.Anchor((RectTransform)close.transform,new Vector2(.25f,.06f),new Vector2(.75f,.13f));close.onClick.AddListener(()=>CloseCardInspection());
            cardInspection.gameObject.SetActive(false);bus.CardInspectionRequested+=InspectCard;
        }
        private void RenderCurrentArt(GameSnapshot state)
        {
            int index=state.WasteSuit*13+state.WasteRank-1;bool valid=characterVisuals!=null&&index>=0&&index<characterVisuals.Length;
            currentArt.enabled=valid&&characterVisuals[index].Portrait!=null;if(valid)currentArt.sprite=characterVisuals[index].Portrait;
            wasteRank.color=state.WasteSuit==2?new Color(1,.36f,.40f):UiFactory.Cream;
            UiFactory.Anchor(wasteIcon.rectTransform,new Vector2(.08f,.46f),new Vector2(.35f,.67f));
            var hold=wasteRect.GetComponent<CardLongPress>();hold.CanInspect=valid&&state.Menu==MenuMode.Playing&&!state.TutorialActive;
            if(state.Menu!=MenuMode.Playing||state.TutorialActive)CloseCardInspection();
        }
        private void InspectCard(int slot)
        {
            if(previous==null||previous.Menu!=MenuMode.Playing||previous.TutorialActive)return;
            int rank=previous.WasteRank,suit=previous.WasteSuit;
            if(slot>=0){if(slot>=previous.Cards.Count)return;var card=previous.Cards[slot];if(!card.Visible||card.Removed)return;rank=card.Rank;suit=card.Suit;}
            int index=suit*13+rank-1;if(characterVisuals==null||index<0||index>=characterVisuals.Length)return;
            var visual=characterVisuals[index];inspectedArt.sprite=visual.FullPortrait;inspectCaption.text=visual.Name+"\n"+CardView.RankLabel(rank)+" / "+CardView.SuitLabel(suit,true);
            cardInspection.gameObject.SetActive(true);cardInspection.SetAsLastSibling();
        }
        private bool CloseCardInspection(){if(!CardInspectionVisible)return false;cardInspection.gameObject.SetActive(false);return true;}
    }
}
