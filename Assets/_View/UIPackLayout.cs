using TMPro;
using UnityEngine;
namespace CursedDeck.View
{
    public static class UIPackLayout
    {
        public static UnityEngine.UI.Image Panel(string name,Transform parent,Sprite sprite,Vector2 min,Vector2 max)
        {
            var rect=UiFactory.Rect(name,parent);UiFactory.Anchor(rect,min,max);
            var image=UiFactory.Image(rect,sprite!=null?Color.white:UiFactory.Green,sprite);
            image.pixelsPerUnitMultiplier=4;return image;
        }
        public static UnityEngine.UI.Image Icon(string name,Transform parent,Sprite sprite,Vector2 min,Vector2 max)
        {
            var image=Panel(name,parent,sprite,min,max);image.type=UnityEngine.UI.Image.Type.Simple;image.preserveAspect=true;return image;
        }
        public static void Fit(TextMeshProUGUI text,float min=14)
        {text.enableAutoSizing=true;text.fontSizeMin=min;text.fontSizeMax=text.fontSize;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;}
        public static void Button(UnityEngine.UI.Button button,Sprite sprite)
        {var image=(UnityEngine.UI.Image)button.targetGraphic;image.sprite=sprite;image.color=Color.white;image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=4;}
        public static void DrawStates(UnityEngine.UI.Button button,UIPackSkin skin)
        {
            Button(button,skin.DrawDefault);button.transition=UnityEngine.UI.Selectable.Transition.SpriteSwap;
            button.spriteState=new UnityEngine.UI.SpriteState{highlightedSprite=skin.DrawHover,selectedSprite=skin.DrawHover,pressedSprite=skin.DrawHover,disabledSprite=skin.DrawDisabled};
        }
    }
}
