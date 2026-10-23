using System.Collections;
using CursedDeck.Services;
using UnityEngine;
using UnityEngine.EventSystems;
namespace CursedDeck.View
{
    /// <summary>Fixed-state unscaled motion. Root hit area/layout and TweenRunner pop scale stay independent.</summary>
    public sealed class CardFX:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IDragHandler,IInitializePotentialDragHandler
    {
        [SerializeField] private RectTransform motionRoot,shadow;
        [SerializeField] private GameObject frontView,backView;
        [SerializeField] private UnityEngine.UI.Image glowOverlay;
        [SerializeField,Range(0,15)] private float maxTilt=15;
        [SerializeField,Min(.05f)] private float flipDuration=.3f;
        private RectTransform hitArea;private bool reduceMotion;private Vector2 dealOffset;private float dealScale=1;
        public bool HasTapPosition {get;private set;}public Vector3 TapWorldPosition {get;private set;}
        private Quaternion tilt=Quaternion.identity,targetTilt=Quaternion.identity;
        private float angle,startAngle,endAngle,elapsed,punchTime,layoutScale=1;
        private bool flipping,swapped,front,targetFront,pressed,punching;
        private int pointerId;
        private Vector2 targetDegrees;
        private Color glowColor=Color.white;
        public bool IsFlipping=>flipping; public bool FaceFront=>front; public bool TargetFront=>targetFront;
        public float FlipDegrees=>Mathf.Abs(angle-startAngle); public Vector2 TargetTiltDegrees=>targetDegrees;
        public Quaternion CurrentTilt=>tilt;
        public float GlowOpacity=>glowOverlay!=null?glowOverlay.color.a:0;
        public RectTransform MotionRoot=>motionRoot;
        private void Awake(){hitArea=(RectTransform)transform;if(glowOverlay!=null)glowColor=glowOverlay.color;}
        public void SetReduceMotion(bool value){if(reduceMotion==value)return;reduceMotion=value;if(value)SnapFace(targetFront);}
        public void SetDealPose(Vector2 offset,float scale){dealOffset=offset;dealScale=scale;ApplyPose();}
        public void SetLayoutScale(float scale){layoutScale=Mathf.Max(.001f,scale);ApplyPose();}
        public void SnapFace(bool showFront)
        {
            dealOffset=Vector2.zero;dealScale=1;HasTapPosition=false;targetFront=front=showFront;angle=showFront?0:180;startAngle=angle;endAngle=angle;flipping=false;swapped=false;punching=false;pressed=false;
            tilt=targetTilt=Quaternion.identity;targetDegrees=Vector2.zero;SetViews();ApplyPose();
        }
        public void FlipTo(bool showFront)
        {
            if(reduceMotion){SnapFace(showFront);return;}
            if(targetFront==showFront)return;
            targetFront=showFront;startAngle=angle;endAngle=showFront?0:180;elapsed=0;flipping=true;swapped=false;punching=false;
            pressed=false;targetDegrees=Vector2.zero;targetTilt=Quaternion.identity;
        }
        private void SetViews(){if(frontView!=null)frontView.SetActive(front);if(backView!=null)backView.SetActive(!front);}
        private void Update(){Step(Time.unscaledDeltaTime);}
        public void Step(float delta)
        {
            float dt=Mathf.Max(0,delta);
            if(!flipping&&!punching&&!pressed&&Quaternion.Angle(tilt,Quaternion.identity)<.01f)return;
            tilt=Quaternion.Slerp(tilt,targetTilt,1-Mathf.Exp(-15*dt));
            if(!pressed&&Quaternion.Angle(tilt,Quaternion.identity)<.01f)tilt=Quaternion.identity;
            if(flipping)
            {
                elapsed+=dt;float t=Mathf.Clamp01(elapsed/flipDuration);float smooth=t*t*(3-2*t);float next=Mathf.Lerp(startAngle,endAngle,smooth);
                // Split even a long frame at the exact edge-on angle before swapping faces.
                bool crosses=(angle<90&&next>=90)||(angle>90&&next<=90)||Mathf.Approximately(angle,90);
                if(!swapped&&crosses){angle=90;ApplyPose();front=targetFront;SetViews();swapped=true;}
                angle=next;
                if(t>=1){angle=endAngle;front=targetFront;SetViews();flipping=false;punching=true;punchTime=0;}
            }
            else if(punching){punchTime+=dt;if(punchTime>=.20f)punching=false;}
            ApplyPose();
        }
        private void ApplyPose()
        {
            if(motionRoot==null)return;
            motionRoot.anchoredPosition=dealOffset;motionRoot.localRotation=tilt*Quaternion.Euler(0,angle,0);
            float punch=punching?Mathf.Sin(Mathf.Clamp01(punchTime/.20f)*Mathf.PI)*.065f:0;
            motionRoot.localScale=Vector3.one*(layoutScale*dealScale*(1+punch));
            float strength=Mathf.Clamp01(Quaternion.Angle(Quaternion.identity,tilt)/(Mathf.Max(1,maxTilt)*1.414214f));
            if(glowOverlay!=null){var c=glowColor;c.a=strength*.28f;glowOverlay.color=c;}
            if(shadow!=null){var euler=tilt.eulerAngles;shadow.anchoredPosition=new Vector2(7-Mathf.DeltaAngle(0,euler.y)*.22f,-10+Mathf.DeltaAngle(0,euler.x)*.22f);}
        }
        public void OnInitializePotentialDrag(PointerEventData data){data.useDragThreshold=false;}
        public void OnPointerDown(PointerEventData data){if(pressed)return;pressed=true;pointerId=data.pointerId;Track(data);}
        public void OnDrag(PointerEventData data){data.eligibleForClick=false;if(pressed&&data.pointerId==pointerId)Track(data);}
        public void OnPointerUp(PointerEventData data){if(!pressed||data.pointerId!=pointerId)return;pressed=false;targetDegrees=Vector2.zero;targetTilt=Quaternion.identity;}
        private void Track(PointerEventData data)
        {
            if(hitArea==null)hitArea=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(hitArea,data.position,data.pressEventCamera,out var local))return;
            HasTapPosition=true;TapWorldPosition=hitArea.TransformPoint(local);if(reduceMotion)return;
            var rect=hitArea.rect;float nx=Mathf.Clamp((local.x-rect.center.x)/Mathf.Max(1,rect.width*.5f),-1,1),ny=Mathf.Clamp((local.y-rect.center.y)/Mathf.Max(1,rect.height*.5f),-1,1);
            targetDegrees=new Vector2(-ny*maxTilt,nx*maxTilt);targetTilt=Quaternion.Euler(targetDegrees.x,targetDegrees.y,0);
        }
        /// <summary>Optional coroutine API; gameplay uses FlipTo to avoid iterator allocations.</summary>
        public IEnumerator FlipCardRoutine(bool faceUp){FlipTo(faceUp);while(IsFlipping)yield return null;}
        private void OnDisable(){SnapFace(targetFront);}
    }
}
