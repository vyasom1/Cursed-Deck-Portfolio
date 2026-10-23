using System;
using CursedDeck.Data;
using CursedDeck.View;
using UnityEngine;
namespace CursedDeck.Bootstrap
{
    public sealed partial class GameBootstrap
    {
        private CardCharacterVisual[] CharacterVisuals()
        {
            var database=content.Characters;
            if(database==null)throw new InvalidOperationException("Card character database missing. Run Tools > Cursed Deck > Card Art Pipeline.");
            database.Validate();var visuals=new CardCharacterVisual[52];
            foreach(var c in database.Characters)visuals[c.Suit*13+c.Rank-1]=new CardCharacterVisual(c.CharacterName,c.FactionTitle,CardView.SuitLabel(c.Suit,true),c.FactionColor,c.DisplayPortrait,c.Emblem,c.SourceArt);
            return visuals;
        }
    }
}
