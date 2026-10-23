using System;
using CursedDeck.Core;
using CursedDeck.Data;
using CursedDeck.View;
using TMPro;
using UnityEditor;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static class PrepareVisualPhase1
    {
        [MenuItem("Tools/Cursed Deck/Prepare Visual Phase 1")]
        public static void Prepare()
        {
            var content=Resources.Load<GameContent>("CursedDeck/Content");
            const string layoutPath="Assets/_Data/Layout_TwoPeaks.asset";
            var layout=AssetDatabase.LoadAssetAtPath<BoardLayoutDefinition>(layoutPath);
            if(layout==null){layout=ScriptableObject.CreateInstance<BoardLayoutDefinition>();AssetDatabase.CreateAsset(layout,layoutPath);}
            var serialized=new SerializedObject(layout);var slots=serialized.FindProperty("slots");slots.arraySize=BoardLayout.TwoPeaks.Count;
            for(int i=0;i<slots.arraySize;i++)
            {
                var slot=slots.GetArrayElementAtIndex(i);var logical=BoardLayout.TwoPeaks[i];slot.FindPropertyRelative("x").intValue=logical.X;slot.FindPropertyRelative("y").intValue=logical.Y;
                var covering=slot.FindPropertyRelative("covering");covering.arraySize=logical.CoveringSlots.Count;for(int j=0;j<covering.arraySize;j++)covering.GetArrayElementAtIndex(j).intValue=logical.CoveringSlots[j];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();var game=new SerializedObject(content);game.FindProperty("layout").objectReferenceValue=layout;game.ApplyModifiedPropertiesWithoutUndo();
            PrepareRankMaterial(content.Font);PortfolioCardPresentation.ConfigurePrefab();
            var overlay=PrefabUtility.LoadPrefabContents(PrepareTutorial.PrefabPath);
            try{overlay.GetComponent<TutorialManager>().ApplyBannerContrast();PrefabUtility.SaveAsPrefabAsset(overlay,PrepareTutorial.PrefabPath);}finally{UnityEngine.Object.DestroyImmediate(overlay);}
            UIWireframePipeline.CreateScreenPrefabs(UIPackSkin.Load());AssetDatabase.SaveAssets();
            Debug.Log("CURSED_DECK_VISUAL_PHASE1_READY Layout=TwoPeaks Cards=19 Base=7 ReferenceCardWidth=145 Rank=36 Suit=44 Outline=3-reference-pixels");
        }
        private static void PrepareRankMaterial(TMP_FontAsset font)
        {
            const string path="Assets/_View/Resources/TCG/RankOutline.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(font.material);AssetDatabase.CreateAsset(material,path);}
            material.CopyPropertiesFromMaterial(font.material);material.shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader");material.name="RankOutline";
            material.SetColor("_FaceColor",Color.white);material.SetColor("_OutlineColor",Color.black);material.SetFloat("_OutlineSoftness",0);
            // Outward-only outline: matching dilation prevents the outline from eroding the bright glyph face.
            // TMP's automatic ratios otherwise shrink both distances when their combined padding exceeds one.
            float width=3*font.faceInfo.pointSize/(36*material.GetFloat("_GradientScale"));
            material.SetFloat("_OutlineWidth",width);material.SetFloat("_FaceDilate",width);material.EnableKeyword("RATIOS_OFF");material.SetFloat("_ScaleRatioA",1);
            material.EnableKeyword("OUTLINE_ON");ShaderUtilities.UpdateShaderRatios(material);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
        }
        public static void ConfigureCard(GameObject root)
        {
            var front=root.transform.Find("CardMotionRoot/CardFront");
            var viewport=(RectTransform)front.Find("PortraitViewport");UiFactory.Anchor(viewport,new Vector2(.04f,.04f),new Vector2(.96f,.96f));
            var shade=UiFactory.Rect("BoardArtShade",viewport);UiFactory.Anchor(shade,Vector2.zero,Vector2.one);if(shade.GetComponent<CanvasRenderer>()==null)shade.gameObject.AddComponent<CanvasRenderer>();var art=shade.GetComponent<CardArtShade>();if(art==null)art=shade.gameObject.AddComponent<CardArtShade>();art.raycastTarget=false;
            var blocked=UiFactory.Rect("BlockedShade",front);UiFactory.Anchor(blocked,Vector2.zero,Vector2.one);UiFactory.Image(blocked,new Color(0,0,0,.5f));blocked.SetAsLastSibling();
            var glow=UiFactory.Rect("PlayableGlow",front);UiFactory.Anchor(glow,Vector2.zero,Vector2.one);if(glow.GetComponent<CanvasRenderer>()==null)glow.gameObject.AddComponent<CanvasRenderer>();var light=glow.GetComponent<CardPlayableGlow>();if(light==null)light=glow.gameObject.AddComponent<CardPlayableGlow>();light.raycastTarget=false;light.enabled=false;
            if(root.GetComponent<CardLongPress>()==null)root.AddComponent<CardLongPress>();
            CardReadability.Rank(front.Find("HeaderBanner/RankBadge/RankNumber").GetComponent<TextMeshProUGUI>());
            var border=front.Find("GothicFrameOverlay").GetComponent<UnityEngine.UI.Image>();
            var cv=new SerializedObject(root.GetComponent<CardView>());cv.FindProperty("frameBorder").objectReferenceValue=border;cv.ApplyModifiedPropertiesWithoutUndo();
            var badge=front.Find("HeaderBanner/RankBadge").GetComponent<UnityEngine.UI.Image>();badge.sprite=null;badge.color=new Color(0,0,0,.55f);
        }
        public static void CompileAndroid()
        {
            const string output="TestResults/visual-phase1-android/scripts";System.IO.Directory.CreateDirectory(output);
            var result=UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(new UnityEditor.Build.Player.ScriptCompilationSettings{target=BuildTarget.Android,group=BuildTargetGroup.Android},output);
            if(result.assemblies==null||result.assemblies.Count==0)throw new InvalidOperationException("Android compilation produced no assemblies.");
            Debug.Log("CURSED_DECK_VISUAL_PHASE1_ANDROID_OK Assemblies="+result.assemblies.Count);
        }
    }
}
