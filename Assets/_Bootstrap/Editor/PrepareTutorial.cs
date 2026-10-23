using System.IO;
using CursedDeck.Data;
using CursedDeck.View;
using UnityEditor;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static class PrepareTutorial
    {
        public const string PrefabPath="Assets/_View/Resources/TutorialOverlay.prefab";
        [MenuItem("Tools/Cursed Deck/Prepare Guided Tutorial")]
        public static void Prepare()
        {
            var content=Resources.Load<GameContent>("CursedDeck/Content");var skin=UIPackSkin.Load();
            if(!File.Exists(PrefabPath))
            {
                var root=new GameObject("TutorialOverlay",typeof(RectTransform),typeof(TutorialManager));
                try{root.GetComponent<TutorialManager>().BuildVisuals(content.Font,skin!=null?skin.ResourcePanel:content.CardFront);root.SetActive(false);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);}
                finally{Object.DestroyImmediate(root);}
            }
            var overlay=PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var hand=overlay.transform.Find("HandPointer");if(hand.GetComponent<CanvasRenderer>()==null)hand.gameObject.AddComponent<CanvasRenderer>();
                PrefabUtility.SaveAsPrefabAsset(overlay,PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(overlay);}
            // Existing screen authoring reuses named objects and preserves prefab GUIDs.
            UIWireframePipeline.CreateScreenPrefabs(skin);
            AssetDatabase.SaveAssets();Debug.Log("CURSED_DECK_FIVE_STEP_TUTORIAL_READY");
        }
    }
}
