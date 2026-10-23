using UnityEngine;
namespace CursedDeck.View
{
    /// <summary>Fixed slots and unscaled time. No iterators, delegates or growing collections in Step.</summary>
    public sealed class TweenRunner:MonoBehaviour
    {
        private struct Slot
        {
            public RectTransform Target,Landing;
            public Vector3 From;
            public Vector2 Position,Destination;
            public float Time,Duration,Delay;
            public int Kind;
            public CardView Card;
        }
        private readonly Slot[] slots=new Slot[64];
        public bool ReduceMotion {get;private set;}
        public int ActiveDealCount {get{int n=0;for(int i=0;i<slots.Length;i++)if(slots[i].Target!=null&&slots[i].Kind==4)n++;return n;}}
        public int ActiveFlightCount {get{int n=0;for(int i=0;i<slots.Length;i++)if(slots[i].Target!=null&&slots[i].Kind==3)n++;return n;}}
        public static Vector2 Arc(Vector2 from,Vector2 to,float t)
        {float u=1-t;var control=(from+to)*.5f+Vector2.up*180;return u*u*from+2*u*t*control+t*t*to;}
        public void SetReduceMotion(bool value)
        {
            if(ReduceMotion==value)return;ReduceMotion=value;if(!value)return;
            for(int i=0;i<slots.Length;i++)if(slots[i].Target!=null)Finish(i,true);
        }
        public void Cancel(RectTransform target)
        {
            for(int i=0;i<slots.Length;i++)if(slots[i].Target==target)
            {
                var s=slots[i];slots[i]=default;
                if(s.Kind==2)target.anchoredPosition=s.Position;
                if(s.Kind==4){s.Card.CancelDeal();continue;}
                target.localRotation=Quaternion.identity;target.localScale=Vector3.one;
            }
        }
        private int Free(RectTransform target){Cancel(target);for(int i=0;i<slots.Length;i++)if(slots[i].Target==null)return i;return -1;}
        public void Pulse(RectTransform target,float scale=1.15f)
        {if(ReduceMotion)return;int i=Free(target);if(i<0)return;target.localScale=Vector3.one*scale;slots[i]=new Slot{Target=target,From=target.localScale,Duration=.22f};}
        public void Pop(CardView card,float delay=0)
        {int i=Free(card.Rect);if(i<0||ReduceMotion){card.CompleteRemoval();return;}slots[i]=new Slot{Target=card.Rect,Duration=.18f+Mathf.Max(0,delay),Delay=Mathf.Max(0,delay),Kind=1,Card=card};}
        public void FlyToWaste(CardView card,Vector2 destination,RectTransform landing=null)
        {
            Cancel(card.FX.MotionRoot);int i=Free(card.Rect);
            if(i<0||ReduceMotion){card.CompleteRemoval();return;}
            slots[i]=new Slot{Target=card.Rect,Position=card.Rect.anchoredPosition,Destination=destination,Duration=.20f,Kind=3,Card=card,Landing=landing};
        }
        public void Deal(CardView card,Vector2 offset,float delay)
        {
            int i=Free(card.FX.MotionRoot);if(i<0||ReduceMotion){card.CompleteDeal(true);return;}
            card.BeginDeal();card.FX.SetDealPose(offset,.55f);
            slots[i]=new Slot{Target=card.FX.MotionRoot,Position=offset,Duration=.16f,Delay=delay,Kind=4,Card=card};
        }
        public void Shake(RectTransform target)
        {if(ReduceMotion)return;int i=Free(target);if(i<0)return;slots[i]=new Slot{Target=target,Position=target.anchoredPosition,Duration=.22f,Kind=2};}
        private void Update()=>Step(Time.unscaledDeltaTime);
        public void Step(float delta)
        {
            for(int i=0;i<slots.Length;i++)
            {
                ref var s=ref slots[i];if(s.Target==null)continue;s.Time+=Mathf.Max(0,delta);
                float t=Mathf.Clamp01((s.Time-s.Delay)/s.Duration),ease=t*t*(3-2*t);
                if(s.Kind==4)s.Card.FX.SetDealPose(Vector2.LerpUnclamped(s.Position,Vector2.zero,ease),Mathf.Lerp(.55f,1,ease));
                else if(s.Kind==3){s.Target.anchoredPosition=Arc(s.Position,s.Destination,ease);s.Target.localRotation=Quaternion.Euler(0,0,15*ease);}
                else if(s.Kind==2)s.Target.anchoredPosition=s.Position+new Vector2(Mathf.Sin(t*63)*9*(1-t),Mathf.Cos(t*47)*3*(1-t));
                else if(s.Kind==1)s.Target.localScale=Vector3.one*(1+.12f*Mathf.Sin(t*Mathf.PI))*(1-t*t);
                else s.Target.localScale=Vector3.LerpUnclamped(s.From,Vector3.one,1-(1-t)*(1-t));
                if(s.Time>=s.Delay+s.Duration-0.000001f)Finish(i,false);
            }
        }
        private void Finish(int index,bool instant)
        {
            var s=slots[index];slots[index]=default;if(s.Target==null)return;
            if(s.Kind==4){s.Card.CompleteDeal(instant);return;}
            if(s.Kind==2)s.Target.anchoredPosition=s.Position;
            if(s.Kind==3)s.Target.anchoredPosition=s.Destination;
            s.Target.localScale=Vector3.one;s.Target.localRotation=Quaternion.identity;
            if(s.Card!=null)s.Card.CompleteRemoval();
            if(s.Kind==3&&s.Landing!=null&&!instant)Pulse(s.Landing,1.15f);
        }
    }
}
