using System;
using System.IO;
using CursedDeck.Data;
using UnityEditor;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static class PreparePublicContent
    {
        [MenuItem("Tools/Cursed Deck/Validate Public Card Content")]
        public static void Prepare()
        {
            Debug.Log("CURSED_DECK_PUBLIC_ART "+CardArtPipeline.ImportAll());
            var db=AssetDatabase.LoadAssetAtPath<CardCharacterDatabase>(CardArtPipeline.DatabasePath);db.Validate();
            int sources=0,fallbacks=0;
            foreach(var card in db.Characters)
            {
                if(card.SourceArt!=null)sources++;else fallbacks++;
                if(card.Portrait==null||card.DisplayPortrait==null)throw new InvalidOperationException("Missing reviewed card presentation");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("CURSED_DECK_PUBLIC_CONTENT_OK Characters="+db.Characters.Count+" Sources="+sources+" Fallbacks="+fallbacks);
        }
    }
}
