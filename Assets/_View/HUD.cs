using TMPro;
using UnityEngine;
using CursedDeck.Run;
namespace CursedDeck.View
{
    public sealed class HUD
    {
        private readonly HUDView view;
        private float shown,from,target,age;private bool rolling;
        public float DisplayedScore=>shown;
        public bool IsRolling=>rolling;
        private readonly TextMeshProUGUI draws,chain,seed;
        public RectTransform ChainRect=>chain.rectTransform;
        public RectTransform ScoreRect=>(RectTransform)view.Score.transform.parent;
        public TextMeshProUGUI RelicsText=>view.Relics;
        public HUD(Transform parent,TMP_FontAsset font)
        {
            var skin=UIPackSkin.Load();view=HUDView.Ensure(parent,font,skin);view.Score.SetText("01234567890123456789");view.Score.ForceMeshUpdate(true,true);view.Score.SetText("0");view.Score.ForceMeshUpdate(true,true);
            var bottom=parent.Find("BottomControlPanel");
            var status=UIPackLayout.Panel("StatusBar",bottom,skin!=null?skin.StatusBar:null,new Vector2(.015f,.82f),new Vector2(.985f,1));
            chain=UiFactory.Text("ChainGroup",status.transform,font,"CHAIN 0",32,UiFactory.Gold,TextAlignmentOptions.Left);UiFactory.Anchor(chain.rectTransform,new Vector2(.07f,.22f),new Vector2(.46f,.78f));UIPackLayout.Fit(chain,20);
            draws=UiFactory.Text("DrawsLeftGroup",status.transform,font,"23 DRAWS LEFT",27,UiFactory.Gold,TextAlignmentOptions.Right);UiFactory.Anchor(draws.rectTransform,new Vector2(.53f,.22f),new Vector2(.93f,.78f));UIPackLayout.Fit(draws,18);
            chain.fontStyle=draws.fontStyle=FontStyles.Bold;chain.characterSpacing=draws.characterSpacing=2;
            seed=UiFactory.Text("Seed",parent,font,"",15,UiFactory.Muted);UiFactory.Anchor(seed.rectTransform,new Vector2(.12f,.002f),new Vector2(.88f,.016f));UIPackLayout.Fit(seed,12);
        }
        public void Step(float delta)
        {
            if(!rolling)return;age+=Mathf.Max(0,delta);float t=Mathf.Clamp01(age/.25f);shown=Mathf.Lerp(from,target,t*t*(3-2*t));view.Score.SetText("{0:0}",shown);if(t>=1)rolling=false;
        }
        public void Render(GameSnapshot state,bool animate=false)
        {
            view.Floor.text="FLOOR "+state.Floor+" / ROUND "+state.Round;view.Venue.text=!string.IsNullOrEmpty(state.DealerName)?state.DealerName:state.Boss?"DEALER ROUND":"HAUNTED CASINO";
            target=(float)state.Score;from=shown;age=0;rolling=animate&&shown!=target;
            if(!rolling){shown=target;view.Score.SetText("{0:0}",shown);}
            view.Target.SetText("TARGET {0:0}",(float)state.Target);
            view.Gold.SetText("GOLD {0}",state.Gold);view.Relics.SetText("{0} RELICS",state.RelicCount);
            draws.SetText("{0} DRAWS LEFT",state.Draws);chain.SetText("CHAIN {0}",state.Chain);
            seed.text=string.IsNullOrEmpty(state.DealerRule)?"SEED "+state.Seed+" · K / A":state.DealerRule;
        }
    }
}
