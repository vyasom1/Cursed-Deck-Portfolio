using UnityEngine;
namespace CursedDeck.View
{
    // Presentation-only authored data; View remains independent of Core and Data.
    [CreateAssetMenu(menuName="Cursed Deck/UI Pack Skin")]
    public sealed class UIPackSkin:ScriptableObject
    {
        public Sprite Background,TitlePlaque,FloorInfo,ScorePanel,ResourcePanel,InstructionPanel,StatusBar;
        public Sprite DrawDefault,DrawHover,DrawDisabled,SecondaryButton,CardBack,CardFrame,Settings,Gold,Relic;
        public Material[] SuitMaterials=new Material[4];
        public Sprite[] Suits=new Sprite[4]; // Clubs, Diamonds, Hearts, Spades (DTO order).
        public static UIPackSkin Load()=>Resources.Load<UIPackSkin>("CursedDeck/UIPackSkin");
        public void Validate()
        {
            if(Background==null||TitlePlaque==null||FloorInfo==null||ScorePanel==null||ResourcePanel==null||InstructionPanel==null||StatusBar==null||DrawDefault==null||DrawHover==null||DrawDisabled==null||SecondaryButton==null||CardBack==null||CardFrame==null||Settings==null||Gold==null||Relic==null||Suits==null||Suits.Length!=4)
                throw new System.InvalidOperationException("UI pack has missing required sprites. Run Wire UI Pack to Prefabs.");
            for(int i=0;i<4;i++)if(Suits[i]==null)throw new System.InvalidOperationException("UI pack suit icon missing.");
        }
    }
}
