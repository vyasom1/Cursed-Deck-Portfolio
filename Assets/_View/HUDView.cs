using TMPro;
using UnityEngine;
namespace CursedDeck.View
{
    public sealed class HUDView:MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI Floor,Venue,Score,Target,Gold,Relics;
        public static HUDView Ensure(Transform parent,TMP_FontAsset font,UIPackSkin skin)
        {
            var found=parent.Find("HUDStatsPanel");
            if(found==null)
            {
                var prefab=Resources.Load<HUDView>("UI/HUDView");
                if(prefab!=null){found=Instantiate(prefab,parent,false).transform;found.name="HUDStatsPanel";}
            }
            var rect=found!=null?(RectTransform)found:UiFactory.Rect("HUDStatsPanel",parent);
            UiFactory.Anchor(rect,new Vector2(.025f,.735f),new Vector2(.975f,.885f));
            var view=rect.GetComponent<HUDView>();if(view==null)view=rect.gameObject.AddComponent<HUDView>();
            var bar=UIPackLayout.Panel("FloorRoundBar",rect,skin!=null?skin.FloorInfo:null,new Vector2(0,.71f),Vector2.one);
            view.Floor=UiFactory.Text("Floor",bar.transform,font,"FLOOR 1 / ROUND 1",22,UiFactory.Gold,TextAlignmentOptions.Right);UiFactory.Anchor(view.Floor.rectTransform,new Vector2(.08f,.23f),new Vector2(.48f,.77f));UIPackLayout.Fit(view.Floor,15);
            view.Venue=UiFactory.Text("Venue",bar.transform,font,"HAUNTED CASINO",22,UiFactory.Cream,TextAlignmentOptions.Left);UiFactory.Anchor(view.Venue.rectTransform,new Vector2(.52f,.23f),new Vector2(.92f,.77f));UIPackLayout.Fit(view.Venue,15);
            var separator=UiFactory.Rect("DiamondSeparator",bar.transform);UiFactory.Place(separator,.5f,.5f,8,8);separator.localRotation=Quaternion.Euler(0,0,45);UiFactory.Image(separator,UiFactory.Gold);
            var score=UIPackLayout.Panel("ScoreGroup",rect,skin!=null?skin.ScorePanel:null,Vector2.zero,new Vector2(.56f,.73f));
            view.Score=UiFactory.Text("Score",score.transform,font,"0",60,UiFactory.Cream,TextAlignmentOptions.Left);UiFactory.Anchor(view.Score.rectTransform,new Vector2(.13f,.37f),new Vector2(.87f,.83f));UIPackLayout.Fit(view.Score,30);
            view.Target=UiFactory.Text("Target",score.transform,font,"TARGET 2000",25,UiFactory.Gold,TextAlignmentOptions.Left);UiFactory.Anchor(view.Target.rectTransform,new Vector2(.13f,.16f),new Vector2(.87f,.39f));UIPackLayout.Fit(view.Target,16);
            var resource=UIPackLayout.Panel("ResourceGroup",rect,skin!=null?skin.ResourcePanel:null,new Vector2(.57f,0),new Vector2(1,.73f));
            UIPackLayout.Icon("GoldIcon",resource.transform,skin!=null?skin.Gold:null,new Vector2(.13f,.50f),new Vector2(.28f,.78f));
            UIPackLayout.Icon("RelicIcon",resource.transform,skin!=null?skin.Relic:null,new Vector2(.13f,.20f),new Vector2(.28f,.48f));
            view.Gold=UiFactory.Text("Gold",resource.transform,font,"GOLD 0",30,UiFactory.Gold,TextAlignmentOptions.Right);UiFactory.Anchor(view.Gold.rectTransform,new Vector2(.30f,.50f),new Vector2(.85f,.79f));UIPackLayout.Fit(view.Gold,16);
            view.Relics=UiFactory.Text("Relics",resource.transform,font,"0 RELICS",24,UiFactory.Cream,TextAlignmentOptions.Right);UiFactory.Anchor(view.Relics.rectTransform,new Vector2(.30f,.20f),new Vector2(.85f,.49f));UIPackLayout.Fit(view.Relics,16);
            return view;
        }
    }
}
