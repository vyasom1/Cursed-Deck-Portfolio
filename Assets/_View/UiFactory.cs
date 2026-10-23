using TMPro;
using UnityEngine;

namespace CursedDeck.View
{
    public static class UiFactory
    {
        public static readonly Color Gold=new Color(212/255f,175/255f,55/255f);
        public static readonly Color Cream=new Color(240/255f,230/255f,210/255f);
        public static readonly Color Muted=new Color(0.58f,0.72f,0.65f);
        public static readonly Color Green=new Color(0.06f,0.19f,0.17f);
        public static RectTransform Rect(string name,Transform parent)
        {
            var existing=parent.Find(name); if(existing!=null)return (RectTransform)existing;
            var go=new GameObject(name,typeof(RectTransform)); var rect=(RectTransform)go.transform;
            rect.SetParent(parent,false); rect.localScale=Vector3.one; return rect;
        }
        public static void Anchor(RectTransform rect,Vector2 min,Vector2 max)
        {
            rect.anchorMin=min; rect.anchorMax=max; rect.pivot=new Vector2(.5f,.5f); rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero;
        }
        public static void Place(RectTransform rect,float x,float y,float w,float h)
        {
            rect.anchorMin=rect.anchorMax=new Vector2(x,y); rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=Vector2.zero; rect.sizeDelta=new Vector2(w,h);
        }
        public static UnityEngine.UI.Image Image(RectTransform rect,Color color,Sprite sprite=null,bool raycast=false)
        {
            var image=rect.GetComponent<UnityEngine.UI.Image>();if(image==null)image=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color=color; image.sprite=sprite;
            image.type=sprite!=null ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.raycastTarget=raycast; return image;
        }
        public static TextMeshProUGUI Text(string name,Transform parent,TMP_FontAsset font,string value,float size,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.Center)
        {
            var rect=Rect(name,parent); Anchor(rect,Vector2.zero,Vector2.one);
            var label=rect.GetComponent<TextMeshProUGUI>();if(label==null)label=rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font=font; label.text=value;
            label.fontSize=size; label.color=color; label.alignment=alignment; label.raycastTarget=false;
            label.textWrappingMode=TextWrappingModes.Normal; label.overflowMode=TextOverflowModes.Ellipsis;
            return label;
        }
        public static UnityEngine.UI.Button Button(string name,Transform parent,TMP_FontAsset font,string value,float size,Sprite sprite,Color background)
        {
            var rect=Rect(name,parent); Image(rect,background,sprite,true);
            var button=rect.GetComponent<UnityEngine.UI.Button>();if(button==null)button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=rect.GetComponent<UnityEngine.UI.Image>();
            var colors=button.colors; colors.normalColor=Color.white; colors.highlightedColor=new Color(.9f,.96f,.91f);
            colors.pressedColor=new Color(.68f,.79f,.71f); colors.disabledColor=new Color(.5f,.58f,.52f); colors.fadeDuration=.08f; button.colors=colors;
            var label=Text("Label",rect,font,value,size,Cream); label.margin=new Vector4(18,6,18,6);
            return button;
        }
    }
}
