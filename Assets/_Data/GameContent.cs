using System;
using CursedDeck.Run;
using TMPro;
using UnityEngine;

namespace CursedDeck.Data
{
    public sealed class GameContent : ScriptableObject
    {
        [SerializeField] private RelicDefinition[] relics;
        [SerializeField] private BoardLayoutDefinition layout;
        [SerializeField] private RunConfigDefinition rules;
        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite cardFront,cardBack;
        [SerializeField] private Sprite[] suitIcons;
        [SerializeField] private CardCharacterDatabase characters;
        public CardCharacterDatabase Characters=>characters;
#if UNITY_EDITOR
        public void SetCharacters(CardCharacterDatabase value){characters=value;}
#endif
        public System.Collections.Generic.IReadOnlyList<RelicDefinition> Relics=>relics;
        public BoardLayoutDefinition Layout => layout;
        public RunConfigDefinition Rules => rules;
        public TMP_FontAsset Font => font;
        public Sprite CardFront => cardFront;
        public Sprite CardBack => cardBack;
        public Sprite[] SuitIcons => suitIcons;
        public RelicSpec[] CreateCatalog()
        {
            if(relics==null) throw new InvalidOperationException("Relics are missing.");
            var result=new RelicSpec[relics.Length]; for(int i=0;i<result.Length;i++) result[i]=relics[i].CreateSpec(); return result;
        }
        public RelicPresentation[] CreatePresentation()
        {
            var result=new RelicPresentation[relics.Length];
            for(int i=0;i<result.Length;i++) result[i]=new RelicPresentation(relics[i].CreateSpec().Id,relics[i].DisplayName,relics[i].Description);
            return result;
        }
        public void Validate()
        {
            if(layout==null || rules==null || font==null || cardFront==null || cardBack==null || suitIcons==null || suitIcons.Length!=4) throw new InvalidOperationException("Game content references are incomplete.");
            layout.CreateLayout(); rules.CreateConfig(); CreateCatalog();
        }
    }
}
