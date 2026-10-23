using UnityEngine;
namespace CursedDeck.View
{
    public sealed class DrawButtonBreath:MonoBehaviour
    {
        private UnityEngine.UI.Image glow;private UnityEngine.UI.Button button;private RectTransform target;private float age;
        public bool ReduceMotion {get;private set;}
        private void Awake(){Initialize();}
        public void Initialize(){if(target!=null)return;glow=GetComponent<UnityEngine.UI.Image>();button=GetComponentInParent<UnityEngine.UI.Button>();target=button!=null?(RectTransform)button.transform:(RectTransform)transform;}
        public void SetReduceMotion(bool value){ReduceMotion=value;if(value){age=0;Apply(0);}}
        public static float ScaleAt(float seconds)=>1+.02f*(1-Mathf.Cos(seconds*Mathf.PI*2/1.5f));
        private void Update()=>Step(Time.unscaledDeltaTime);
        public void Step(float delta){if(target==null||glow==null)return;if(ReduceMotion||button!=null&&!button.interactable){Apply(0);return;}age+=Mathf.Max(0,delta);Apply((ScaleAt(age)-1)/.04f);}
        private void Apply(float wave){if(target==null)return;target.localScale=Vector3.one*(1+.04f*wave);if(glow!=null)glow.color=new Color(.1f,1,.55f,ReduceMotion?0:.035f+.035f*wave);}
        private void OnDisable(){age=0;Apply(0);}
    }
}
