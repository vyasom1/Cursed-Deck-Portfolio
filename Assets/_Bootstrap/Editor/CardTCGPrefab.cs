using System;
using System.IO;
using CursedDeck.Data;
using CursedDeck.View;
using TMPro;
using UnityEditor;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static class CardTCGPrefab
    {
        public static void Ensure(CardCharacterDatabase db)
        {
            if(File.Exists(CardArtPipeline.PrefabPath))return; // Artist overrides survive art reimports.
            Directory.CreateDirectory(Path.GetDirectoryName(CardArtPipeline.PrefabPath));AssetDatabase.Refresh();
            var content=Resources.Load<GameContent>("CursedDeck/Content");if(content==null)throw new InvalidOperationException("Base game content missing.");
            var frame=CardArtTextures.Style("OrnateFrame",2);var shadow=CardArtTextures.Style("CardShadow",3);var gem=CardArtTextures.Style("SocketGem",4);var badge=CardArtTextures.Style("RankBadge",5);var glow=CardArtTextures.Style("SpecularGlow",6);
            string materialPath="Assets/_View/Resources/TCG/CardSpecularGlow.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/_View/Shaders/CardSpecularGlow.shader");if(shader==null)throw new InvalidOperationException("Card glow shader missing.");material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
            var root=new GameObject("CardView",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button),typeof(CardView),typeof(CardFX));
            try
            {
                var rect=(RectTransform)root.transform;rect.sizeDelta=new Vector2(300,420);var hit=root.GetComponent<UnityEngine.UI.Image>();hit.color=Color.clear;hit.raycastTarget=true;
                var button=root.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=hit;button.transition=UnityEngine.UI.Selectable.Transition.None;var nav=button.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.None;button.navigation=nav;
                var motion=UiFactory.Rect("CardMotionRoot",rect);UiFactory.Place(motion,.5f,.5f,300,420);
                var shadowRect=UiFactory.Rect("CardShadow",motion);UiFactory.Place(shadowRect,.5f,.5f,320,440);shadowRect.anchoredPosition=new Vector2(7,-10);Pic(shadowRect,shadow,Color.white);
                var front=UiFactory.Rect("CardFront",motion);UiFactory.Anchor(front,Vector2.zero,Vector2.one);var group=front.gameObject.AddComponent<CanvasGroup>();
                var baseRect=UiFactory.Rect("CardBase",front);UiFactory.Anchor(baseRect,Vector2.zero,Vector2.one);Pic(baseRect,null,new Color(.035f,.041f,.057f));
                var viewport=UiFactory.Rect("PortraitViewport",front);UiFactory.Place(viewport,.5f,.54f,260,240);Pic(viewport,null,new Color(.055f,.065f,.09f));viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                var portraitRect=UiFactory.Rect("CharacterPortrait",viewport);UiFactory.Anchor(portraitRect,Vector2.zero,Vector2.one);var portrait=Pic(portraitRect,db.Find(3,1).Portrait,Color.white);portrait.preserveAspect=true;
                var ornate=UiFactory.Rect("OrnateFrame",front);UiFactory.Anchor(ornate,Vector2.zero,Vector2.one);Pic(ornate,frame,Color.white);
                var gems=new UnityEngine.UI.Image[4];for(int i=0;i<4;i++){var g=UiFactory.Rect("GemSocket-"+i,ornate);UiFactory.Place(g,i%2==0?16/300f:284/300f,i<2?16/420f:404/420f,24,24);gems[i]=Pic(g,gem,db.Find(3,1).FactionColor);}
                var header=UiFactory.Rect("HeaderBanner",front);UiFactory.Place(header,.5f,.905f,280,62);Pic(header,null,new Color(.035f,.04f,.055f,.96f));
                var rankRect=UiFactory.Rect("RankBadge",header);UiFactory.Place(rankRect,.11f,.5f,52,52);Pic(rankRect,badge,Color.white);var rank=UiFactory.Text("RankNumber",rankRect,content.Font,"A",30,UiFactory.Gold);rank.fontStyle=FontStyles.Bold;
                var name=UiFactory.Text("CharacterNameText",header,content.Font,db.Find(3,1).CharacterName,18,UiFactory.Cream);UiFactory.Anchor(name.rectTransform,new Vector2(.24f,.04f),new Vector2(.98f,.96f));name.fontStyle=FontStyles.Bold;name.richText=false;
                var bottom=UiFactory.Rect("BottomBanner",front);UiFactory.Place(bottom,.5f,.125f,280,82);Pic(bottom,null,new Color(.04f,.045f,.065f,.96f));
                var emblemRect=UiFactory.Rect("SuitEmblem",bottom);UiFactory.Place(emblemRect,.14f,.5f,44,44);var emblem=Pic(emblemRect,db.Find(3,1).Emblem,db.Find(3,1).FactionColor);
                var title=UiFactory.Text("FactionTitleText",bottom,content.Font,db.Find(3,1).FactionTitle,17,db.Find(3,1).FactionColor);UiFactory.Anchor(title.rectTransform,new Vector2(.29f,.06f),new Vector2(.97f,.94f));title.fontStyle=FontStyles.Bold;title.richText=false;
                var back=UiFactory.Rect("CardBack",motion);UiFactory.Anchor(back,Vector2.zero,Vector2.one);back.localRotation=Quaternion.Euler(0,180,0);var backImage=Pic(back,content.CardBack,Color.white);
                var glowRect=UiFactory.Rect("SpecularGlowOverlay",motion);UiFactory.Anchor(glowRect,Vector2.zero,Vector2.one);var overlay=Pic(glowRect,glow,new Color(.85f,.93f,1,0));overlay.material=material;
                var cv=new SerializedObject(root.GetComponent<CardView>());Set(cv,"characterPortrait",portrait);Set(cv,"suitEmblem",emblem);Set(cv,"backImage",backImage);Set(cv,"rankBadge",rank);Set(cv,"characterName",name);Set(cv,"factionTitle",title);Set(cv,"frontGroup",group);Set(cv,"genericPortrait",db.Find(3,1).FallbackArt);
                var array=cv.FindProperty("factionGems");array.arraySize=4;for(int i=0;i<4;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=gems[i];cv.ApplyModifiedPropertiesWithoutUndo();
                var fx=new SerializedObject(root.GetComponent<CardFX>());Set(fx,"motionRoot",motion);Set(fx,"shadow",shadowRect);Set(fx,"frontView",front.gameObject);Set(fx,"backView",back.gameObject);Set(fx,"glowOverlay",overlay);fx.ApplyModifiedPropertiesWithoutUndo();root.GetComponent<CardFX>().SnapFace(true);
                PrefabUtility.SaveAsPrefabAsset(root,CardArtPipeline.PrefabPath);AssetDatabase.SaveAssetIfDirty(material);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        public static void WireFeedback(CardCharacterDatabase db)
        {
            var root=PrefabUtility.LoadPrefabContents(CardArtPipeline.PrefabPath);
            try
            {
                var front=root.transform.Find("CardMotionRoot/CardFront");
                var frame=front.Find("OrnateFrame").GetComponent<UnityEngine.UI.Image>();
                var cv=new SerializedObject(root.GetComponent<CardView>());Set(cv,"frameBorder",frame);cv.ApplyModifiedPropertiesWithoutUndo();
                var fx=new SerializedObject(root.GetComponent<CardFX>());fx.FindProperty("flipDuration").floatValue=.3f;fx.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,CardArtPipeline.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void Set(SerializedObject owner,string field,UnityEngine.Object value){owner.FindProperty(field).objectReferenceValue=value;}
        private static UnityEngine.UI.Image Pic(RectTransform rect,Sprite sprite,Color color){var image=UiFactory.Image(rect,color,sprite);image.type=UnityEngine.UI.Image.Type.Simple;return image;}
    }
}
