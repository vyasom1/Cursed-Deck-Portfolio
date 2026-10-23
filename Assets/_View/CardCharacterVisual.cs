using UnityEngine;
namespace CursedDeck.View
{
    /// <summary>Presentation-only data injected by Bootstrap. View has no Data/Core dependency.</summary>
    public readonly struct CardCharacterVisual
    {
        public readonly string Name,Faction,AccessibleFaction;
        public readonly Color Color;
        public readonly Sprite Portrait,Emblem,FullPortrait;
        public CardCharacterVisual(string name,string faction,string suitLabel,Color color,Sprite portrait,Sprite emblem,Sprite fullPortrait=null)
        {Name=name;Faction=faction;AccessibleFaction=faction+" / "+suitLabel;Color=color;Portrait=portrait;Emblem=emblem;FullPortrait=fullPortrait!=null?fullPortrait:portrait;}
    }
}
