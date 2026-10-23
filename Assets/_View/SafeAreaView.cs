using UnityEngine;

namespace CursedDeck.View
{
    public sealed class SafeAreaView : MonoBehaviour
    {
        private RectTransform rect;
        private Rect last;
        private int width,height;
        private void Awake() { rect=GetComponent<RectTransform>(); }
        private void OnEnable() { Apply(Screen.safeArea,Screen.width,Screen.height); }
        private void Update()
        {
            if(last!=Screen.safeArea || width!=Screen.width || height!=Screen.height) Apply(Screen.safeArea,Screen.width,Screen.height);
        }
        public static Rect Normalize(Rect safe,int width,int height)
        {
            if(width<=0 || height<=0) return new Rect(0,0,1,1);
            float left=Mathf.Clamp01(safe.xMin/width),bottom=Mathf.Clamp01(safe.yMin/height);
            return Rect.MinMaxRect(left,bottom,Mathf.Clamp(safe.xMax/width,left,1),Mathf.Clamp(safe.yMax/height,bottom,1));
        }
        public void Apply(Rect safe,int screenWidth,int screenHeight)
        {
            if(rect==null) rect=GetComponent<RectTransform>();
            var normalized=Normalize(safe,screenWidth,screenHeight);
            rect.anchorMin=normalized.min; rect.anchorMax=normalized.max; rect.offsetMin=rect.offsetMax=Vector2.zero;
            last=safe; width=screenWidth; height=screenHeight;
        }
    }
}
