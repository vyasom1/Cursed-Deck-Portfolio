using TMPro;
using UnityEngine;
namespace CursedDeck.View
{
    /// <summary>Prewarmed bounded TMP message pool; animation and reuse do not build strings.</summary>
    public sealed class FloatingScoreText:MonoBehaviour
    {
        private struct Entry { public TextMeshProUGUI Text;public RectTransform Rect;public Vector2 Origin;public float Age;public bool Active; }
        private Entry[] entries;private RectTransform rect;private int cursor;
        public bool ReduceMotion {get;set;}
        public int Capacity=>entries==null?0:entries.Length;
        public int ActiveCount {get;private set;}
        public Vector2 LastPosition {get;private set;}
        public void Initialize(TMP_FontAsset font,int capacity=12)
        {
            if(entries!=null)return;rect=(RectTransform)transform;entries=new Entry[Mathf.Clamp(capacity,1,16)];
            for(int i=0;i<entries.Length;i++)
            {
                var text=UiFactory.Text("ScorePopup-"+i,transform,font,"+0123456789\n0123456789x CHAIN!",32,UiFactory.Gold);
                text.raycastTarget=false;text.fontStyle=FontStyles.Bold;text.enableWordWrapping=false;text.overflowMode=TextOverflowModes.Overflow;
                var rt=text.rectTransform;rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.sizeDelta=new Vector2(420,100);
                text.ForceMeshUpdate(true,true);text.canvasRenderer.SetAlpha(0);
                entries[i]=new Entry{Text=text,Rect=rt};
            }
        }
        public void Spawn(Vector3 world,float score,int chain)
        {
            if(entries==null)return;int index=-1;
            for(int i=0;i<entries.Length;i++){int n=(cursor+i)%entries.Length;if(!entries[n].Active){index=n;break;}}
            if(index<0)index=cursor;cursor=(index+1)%entries.Length;var entry=entries[index];if(!entry.Active)ActiveCount++;
            var point=(Vector2)rect.InverseTransformPoint(world);entry.Origin=point;entry.Age=0;entry.Active=true;
            entry.Rect.anchoredPosition=point;
            if(chain>1)entry.Text.SetText("+{0:0}\n{1:0}x CHAIN!",score,chain);else entry.Text.SetText("+{0:0}",score);
            entry.Text.canvasRenderer.SetAlpha(1);entries[index]=entry;LastPosition=point;
        }
        public void ResetPool()
        {
            if(entries==null)return;for(int i=0;i<entries.Length;i++){entries[i].Active=false;entries[i].Text.canvasRenderer.SetAlpha(0);}ActiveCount=0;
        }
        private void Update(){Step(Time.unscaledDeltaTime);}
        public void Step(float delta)
        {
            if(entries==null)return;
            for(int i=0;i<entries.Length;i++)
            {
                if(!entries[i].Active)continue;var e=entries[i];e.Age+=Mathf.Max(0,delta);float t=Mathf.Clamp01(e.Age/.6f);
                e.Rect.anchoredPosition=e.Origin+Vector2.up*(ReduceMotion?0:150*t);e.Text.canvasRenderer.SetAlpha(1-t);LastPosition=e.Rect.anchoredPosition;
                if(t>=1){e.Active=false;ActiveCount--;}entries[i]=e;
            }
        }
    }
}
