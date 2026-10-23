using System;
using System.Collections.Generic;
using UnityEngine;
namespace CursedDeck.Data
{
    [Serializable]
    public struct CardCharacterData
    {
        [SerializeField] private int suit,rank;
        [SerializeField] private string characterName,factionTitle;
        [SerializeField,TextArea] private string factionDescription,pose;
        [SerializeField] private Color factionColor;
        [SerializeField] private Sprite sourceArt,fallbackArt,emblem,portraitArt;
        public int Suit=>suit; public int Rank=>rank;
        public string CharacterName=>characterName; public string FactionTitle=>factionTitle;
        public string FactionDescription=>factionDescription; public string Pose=>pose;
        public Color FactionColor=>factionColor;
        public Sprite DisplayPortrait=>portraitArt!=null?portraitArt:Portrait;
        public Sprite SourceArt=>sourceArt; public Sprite FallbackArt=>fallbackArt;
        public Sprite Portrait=>sourceArt!=null?sourceArt:fallbackArt; public Sprite Emblem=>emblem;
        public CardCharacterData(int suit,int rank,string name,string faction,string description,string pose,Color color)
        {portraitArt=sourceArt=fallbackArt=emblem=null;this.suit=suit;this.rank=rank;characterName=name;factionTitle=faction;factionDescription=description;this.pose=pose;factionColor=color;}
#if UNITY_EDITOR
        public void SetDisplayPortrait(Sprite sprite){portraitArt=sprite;}
        public void SetImportedArt(Sprite source,Sprite fallback,Sprite suitEmblem){if(sourceArt!=source)portraitArt=null;sourceArt=source;fallbackArt=fallback;emblem=suitEmblem;}
#endif
    }
    [CreateAssetMenu(menuName="Cursed Deck/Card Character Database")]
    public sealed class CardCharacterDatabase:ScriptableObject
    {
        [SerializeField] private int catalogVersion;
        [SerializeField] private Sprite defaultCardBack;
        public Sprite DefaultCardBack=>defaultCardBack;
        [SerializeField] private CardCharacterData[] characters=Array.Empty<CardCharacterData>();
        public IReadOnlyList<CardCharacterData> Characters=>characters;
        public CardCharacterData Find(int suit,int rank)
        {
            if(suit<0||suit>3||rank<1||rank>13)return default;
            for(int i=0;i<characters.Length;i++)if(characters[i].Suit==suit&&characters[i].Rank==rank)return characters[i];
            return default;
        }
        public void Validate(bool requireArt=true)
        {
            if(characters==null||characters.Length!=52)throw new InvalidOperationException("Character database must contain exactly 52 cards.");
            var ids=new HashSet<int>();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var c in characters)
            {
                if(c.Suit<0||c.Suit>3||c.Rank<1||c.Rank>13||!ids.Add(c.Suit*13+c.Rank-1)||string.IsNullOrWhiteSpace(c.CharacterName)||!names.Add(c.CharacterName)||string.IsNullOrWhiteSpace(c.FactionTitle)||string.IsNullOrWhiteSpace(c.FactionDescription)||string.IsNullOrWhiteSpace(c.Pose))throw new InvalidOperationException("Character keys, unique names and prompt fields must be valid.");
                if(requireArt&&(c.Portrait==null||c.FallbackArt==null||c.Emblem==null))throw new InvalidOperationException("Missing fallback, emblem or portrait for "+c.CharacterName);
            }
        }
#if UNITY_EDITOR
        public void SetDisplayPortrait(int index,Sprite sprite){characters[index].SetDisplayPortrait(sprite);}
        public void SetImportedArt(int index,Sprite source,Sprite fallback,Sprite icon){characters[index].SetImportedArt(source,fallback,icon);}
        public bool UpgradeCatalog(CardCharacterData[] defaults,int version)
        {
            if(catalogVersion>=version)return false;
            for(int i=0;i<defaults.Length;i++){var old=Find(defaults[i].Suit,defaults[i].Rank);defaults[i].SetImportedArt(old.SourceArt,old.FallbackArt,old.Emblem);defaults[i].SetDisplayPortrait(old.DisplayPortrait==old.Portrait?null:old.DisplayPortrait);}
            characters=defaults;catalogVersion=version;Validate(false);return true;
        }
        public void SetDefaultCardBack(Sprite value){defaultCardBack=value;}
        public void SetCharacters(CardCharacterData[] value){characters=value;Validate(false);}
#endif
    }
}
