using System;
using System.IO;
using CursedDeck.Data;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace CursedDeck.Bootstrap.Editor
{
    /// <summary>Derived portrait crops; original supplied PNG bytes and GUIDs are never changed.</summary>
    public static class PortfolioCardPresentation
    {
        public const string Folder="Assets/_View/Art/TCG/Portraits";
        public static void Prepare(CardCharacterDatabase db)
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            for(int i=0;i<db.Characters.Count;i++)
            {
                var c=db.Characters[i];if(c.SourceArt==null){db.SetDisplayPortrait(i,null);continue;}
                string source=AssetDatabase.GetAssetPath(c.SourceArt),path=Folder+"/"+Path.GetFileName(source);
                // The supplied images contain rank, frame and captions. Center crop excludes them.
                // Use original PNG bytes in Editor; no readable full-size textures or Sprite.Create in player.
                if(!File.Exists(path)||File.GetLastWriteTimeUtc(source)>File.GetLastWriteTimeUtc(path)||File.GetLastWriteTimeUtc("Assets/_Bootstrap/Editor/PortfolioCardPresentation.cs")>File.GetLastWriteTimeUtc(path))
                {
                    var input=new Texture2D(2,2,TextureFormat.RGBA32,false);Texture2D output=null;
                    try
                    {
                        if(!input.LoadImage(File.ReadAllBytes(source)))throw new InvalidOperationException("Invalid art "+source);
                        int x=Mathf.RoundToInt(input.width*.28f),y=Mathf.RoundToInt(input.height*.29f),w=Mathf.RoundToInt(input.width*.44f),h=Mathf.RoundToInt(input.height*.61f);
                        output=new Texture2D(w,h,TextureFormat.RGBA32,false);output.SetPixels(input.GetPixels(x,y,w,h));output.Apply();File.WriteAllBytes(path,output.EncodeToPNG());
                    }
                    finally{UnityEngine.Object.DestroyImmediate(input);if(output!=null)UnityEngine.Object.DestroyImmediate(output);}
                }
                
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            for(int i=0;i<db.Characters.Count;i++){var c=db.Characters[i];if(c.SourceArt!=null)db.SetDisplayPortrait(i,CardArtPipeline.ConfigureSprite(Folder+"/"+Path.GetFileName(AssetDatabase.GetAssetPath(c.SourceArt))));}
            EditorUtility.SetDirty(db);AssetDatabase.SaveAssetIfDirty(db);
        }
        [MenuItem("Tools/Cursed Deck/Refresh Readable Portfolio Cards")]
        public static void Refresh(){CardArtPipeline.ImportAll();}
        public static void ConfigurePrefab()
        {
            if(!File.Exists(CardArtPipeline.PrefabPath))return;
            var root=PrefabUtility.LoadPrefabContents(CardArtPipeline.PrefabPath);
            try
            {
                var front=root.transform.Find("CardMotionRoot/CardFront");
                var viewport=(RectTransform)front.Find("PortraitViewport");viewport.sizeDelta=new Vector2(260,240);viewport.anchoredPosition=Vector2.zero;
                var portrait=viewport.Find("CharacterPortrait").GetComponent<Image>();portrait.preserveAspect=true;
                var header=(RectTransform)front.Find("HeaderBanner");header.sizeDelta=new Vector2(280,62);header.anchorMin=header.anchorMax=new Vector2(.5f,.905f);
                var rank=header.Find("RankBadge/RankNumber").GetComponent<TextMeshProUGUI>();rank.fontSize=30;rank.enableAutoSizing=false;rank.color=new Color(1,.94f,.78f);rank.fontStyle=FontStyles.Bold;
                var badge=(RectTransform)header.Find("RankBadge");badge.anchoredPosition=new Vector2(-110,0);
                // Board-scale cards prioritize their protected rank/suit badges. Names remain on full cards only.
                var name=header.Find("CharacterNameText").GetComponent<TextMeshProUGUI>();name.fontSize=18;
                foreach(string path in new[]{"SuitBadgeTopRight","SuitBadgeBottomLeft"})
                {
                    var r=(RectTransform)front.Find(path);r.anchorMin=r.anchorMax=path.EndsWith("TopRight")?new Vector2(.79f,.89f):new Vector2(.21f,.18f);r.sizeDelta=new Vector2(40,40);r.anchoredPosition=Vector2.zero;
                }
                PrepareVisualPhase1.ConfigureCard(root);
                PrefabUtility.SaveAsPrefabAsset(root,CardArtPipeline.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
