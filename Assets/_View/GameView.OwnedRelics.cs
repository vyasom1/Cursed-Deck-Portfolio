using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using CursedDeck.Run;
namespace CursedDeck.View
{
    public sealed partial class GameView
    {
        private IReadOnlyDictionary<string,Sprite> relicIcons;
        private RectTransform ownedPanel,relicDetailPanel;
        private readonly OwnedRelicCell[] ownedCells=new OwnedRelicCell[6];
        private TextMeshProUGUI ownedPageText,relicName,relicDescription;
        private Image inspectedIcon;private Button ownedPrevious,ownedNext,ownedBadge;
        private int ownedPage;
        public bool OwnedRelicsVisible=>ownedPanel!=null&&ownedPanel.gameObject.activeSelf;
        public bool RelicDetailVisible=>relicDetailPanel!=null&&relicDetailPanel.gameObject.activeSelf;
        public Button OwnedRelicsButton=>ownedBadge;
        public Button OwnedRelicButton(int index)=>ownedCells[index].Button;
        public void SetRelicIcons(IReadOnlyDictionary<string,Sprite> value){relicIcons=value;}
        private Sprite RelicIcon(string id)=>relicIcons!=null&&relicIcons.TryGetValue(id,out var icon)&&icon!=null?icon:skin!=null?skin.Relic:null;
        private void BuildOwnedRelics()
        {
            var label=hud.RelicsText;label.raycastTarget=true;ownedBadge=label.GetComponent<Button>();if(ownedBadge==null)ownedBadge=label.gameObject.AddComponent<Button>();ownedBadge.targetGraphic=label;ownedBadge.onClick.AddListener(OpenOwnedRelics);
            ownedPanel=Panel("OwnedRelics");
            var frame=UIPackLayout.Panel("Frame",ownedPanel,skin!=null?skin.ResourcePanel:null,new Vector2(.04f,.045f),new Vector2(.96f,.96f));MenuSurface(frame.transform);
            var title=UiFactory.Text("Title",ownedPanel,font,"OWNED RELICS",49,UiFactory.Gold);UiFactory.Anchor(title.rectTransform,new Vector2(.08f,.85f),new Vector2(.92f,.94f));
            for(int i=0;i<ownedCells.Length;i++)ownedCells[i]=new OwnedRelicCell(i,ownedPanel,font,front,skin,InspectOwnedRelic);
            ownedPageText=UiFactory.Text("Page",ownedPanel,font,"",25,UiFactory.Cream);UiFactory.Anchor(ownedPageText.rectTransform,new Vector2(.35f,.155f),new Vector2(.65f,.215f));
            ownedPrevious=MenuButton("Previous",ownedPanel,"PREV",.185f,.055f);UiFactory.Anchor((RectTransform)ownedPrevious.transform,new Vector2(.08f,.1575f),new Vector2(.32f,.2125f));ownedPrevious.onClick.AddListener(()=>{ownedPage--;RenderOwnedRelics(previous);});
            ownedNext=MenuButton("Next",ownedPanel,"NEXT",.185f,.055f);UiFactory.Anchor((RectTransform)ownedNext.transform,new Vector2(.68f,.1575f),new Vector2(.92f,.2125f));ownedNext.onClick.AddListener(()=>{ownedPage++;RenderOwnedRelics(previous);});
            var close=MenuButton("Close",ownedPanel,"BACK TO HAND",.09f,.06f);close.onClick.AddListener(()=>ownedPanel.gameObject.SetActive(false));ownedPanel.gameObject.SetActive(false);
            relicDetailPanel=Panel("RelicDetail");var detailFrame=UIPackLayout.Panel("Frame",relicDetailPanel,skin!=null?skin.ResourcePanel:null,new Vector2(.06f,.14f),new Vector2(.94f,.87f));MenuSurface(detailFrame.transform);
            var iconRect=UiFactory.Rect("Icon",relicDetailPanel);UiFactory.Place(iconRect,.5f,.70f,240,240);inspectedIcon=UiFactory.Image(iconRect,Color.white);inspectedIcon.preserveAspect=true;
            relicName=UiFactory.Text("Name",relicDetailPanel,font,"",44,UiFactory.Gold);UiFactory.Anchor(relicName.rectTransform,new Vector2(.10f,.52f),new Vector2(.90f,.61f));relicName.textWrappingMode=TextWrappingModes.Normal;
            relicDescription=UiFactory.Text("ActiveBuff",relicDetailPanel,font,"",34,UiFactory.Cream);UiFactory.Anchor(relicDescription.rectTransform,new Vector2(.12f,.30f),new Vector2(.88f,.51f));relicDescription.textWrappingMode=TextWrappingModes.Normal;
            var backButton=MenuButton("Back",relicDetailPanel,"BACK TO RELICS",.225f,.065f);backButton.onClick.AddListener(()=>CloseOwnedModal());relicDetailPanel.gameObject.SetActive(false);
        }
        private void OpenOwnedRelics(){if(previous==null||previous.Menu!=MenuMode.Playing)return;ownedPage=0;ownedPanel.gameObject.SetActive(true);RenderOwnedRelics(previous);ownedPanel.SetAsLastSibling();}
        private void RenderOwnedRelics(GameSnapshot state)
        {
            if(state.Menu!=MenuMode.Playing){ownedPanel.gameObject.SetActive(false);relicDetailPanel.gameObject.SetActive(false);return;}
            if(!OwnedRelicsVisible)return;
            int count=state.OwnedRelics?.Count??0,pages=Mathf.Max(1,(count+5)/6);ownedPage=Mathf.Clamp(ownedPage,0,pages-1);
            for(int i=0;i<ownedCells.Length;i++){int index=ownedPage*6+i;if(index<count)ownedCells[i].Bind(state.OwnedRelics[index],RelicIcon(state.OwnedRelics[index].Id));else ownedCells[i].Button.gameObject.SetActive(false);}
            ownedPageText.text=count==0?"NO RELICS YET":(ownedPage+1)+" / "+pages;ownedPrevious.interactable=ownedPage>0;ownedNext.interactable=ownedPage+1<pages;
        }
        private void InspectOwnedRelic(string id)
        {
            foreach(var relic in previous.OwnedRelics)if(relic.Id==id){relicName.text=relic.Name;relicDescription.text=relic.Description;inspectedIcon.sprite=RelicIcon(id);inspectedIcon.enabled=inspectedIcon.sprite!=null;ownedPanel.gameObject.SetActive(false);relicDetailPanel.gameObject.SetActive(true);relicDetailPanel.SetAsLastSibling();return;}
        }
        private bool CloseOwnedModal()
        {
            if(RelicDetailVisible){relicDetailPanel.gameObject.SetActive(false);ownedPanel.gameObject.SetActive(true);RenderOwnedRelics(previous);ownedPanel.SetAsLastSibling();return true;}
            if(OwnedRelicsVisible){ownedPanel.gameObject.SetActive(false);return true;}return false;
        }
        private sealed class OwnedRelicCell
        {
            public Button Button{get;}
            private readonly Image icon;private readonly TextMeshProUGUI title;private string id;
            public OwnedRelicCell(int index,RectTransform parent,TMP_FontAsset font,Sprite front,UIPackSkin skin,System.Action<string> inspect)
            {
                Button=UiFactory.Button("Relic-"+index,parent,font,"",1,front,UiFactory.Green);Button.GetComponentInChildren<TextMeshProUGUI>().gameObject.SetActive(false);if(skin!=null)UIPackLayout.Button(Button,skin.SecondaryButton);
                float x=index%2==0?.08f:.52f,y=.65f-index/2*.19f;UiFactory.Anchor((RectTransform)Button.transform,new Vector2(x,y),new Vector2(x+.40f,y+.16f));
                var imageRect=UiFactory.Rect("Icon",Button.transform);UiFactory.Place(imageRect,.5f,.68f,118,118);icon=UiFactory.Image(imageRect,Color.white);icon.preserveAspect=true;
                title=UiFactory.Text("Name",Button.transform,font,"",29,UiFactory.Gold);UiFactory.Anchor(title.rectTransform,new Vector2(.06f,.06f),new Vector2(.94f,.40f));title.textWrappingMode=TextWrappingModes.Normal;UIPackLayout.Fit(title,23);
                Button.onClick.AddListener(()=>inspect(id));
            }
            public void Bind(RelicPresentation relic,Sprite sprite){id=relic.Id;title.text=relic.Name;icon.sprite=sprite;icon.enabled=sprite!=null;Button.gameObject.SetActive(true);}
        }
    }
}
