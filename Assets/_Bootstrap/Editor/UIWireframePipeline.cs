using System;
using System.IO;
using System.Linq;
using System.Text;
using CursedDeck.Data;
using CursedDeck.View;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
namespace CursedDeck.Bootstrap.Editor
{
    public static class UIWireframePipeline
    {
        public const string Source="Assets/UI/CursedDeckUIPack";
        public const string BoardPath="Assets/_View/Resources/UI/BoardView.prefab";
        public const string HUDPath="Assets/_View/Resources/UI/HUDView.prefab";
        public const string SkinPath="Assets/_View/Resources/CursedDeck/UIPackSkin.asset";
        public static void Prepare()
        {
            if(!Directory.Exists(Source))throw new DirectoryNotFoundException("UI pack not found: "+Source);
            var paths=Directory.GetFiles(Source,"*",SearchOption.AllDirectories).Where(f=>Path.GetExtension(f).Equals(".png",StringComparison.OrdinalIgnoreCase)).Select(f=>f.Replace('\\','/')).OrderBy(f=>f,StringComparer.Ordinal).ToArray();
            if(paths.Length==0)throw new InvalidOperationException("No UI PNG files found.");
            // Resolve mandatory roles before mutating assets; the earlier partial Resources pack is untouched.
            string[] required={"GothicBackground","TitlePlaque","InfoPanel","ScorePanel","ResourcePanel","InstructionPanel","StatusBar","PrimaryButton","PrimaryButton_Hover","PrimaryButton_Disabled","SecondaryButton","CardBack","CardFrame","Settings","GoldCoinStack","Relic","Suit_Club","Suit_Diamond","Suit_Heart","Suit_Spade"};
            foreach(string role in required)Resolve(paths,role);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var report=new StringBuilder("# UI pack import manifest\n\nSource PNG bytes are unchanged. Borders below are source pixels (left,bottom,right,top). Image pixelsPerUnitMultiplier = 4 for scalable panels.\n\n| Sprite | Size | Border | Max size |\n|---|---|---|---|\n");
            foreach(string path in paths)
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);string before=EditorJsonUtility.ToJson(importer);
                importer.GetSourceTextureWidthAndHeight(out int width,out int height);
                string stem=Path.GetFileNameWithoutExtension(path);
                Rect region=UIWireframeSprites.Trimmed(path)?UIWireframeSprites.AlphaBounds(path):new Rect(0,0,width,height);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=UIWireframeSprites.Trimmed(path)?SpriteImportMode.Multiple:SpriteImportMode.Single;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=true;
                importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.isReadable=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.npotScale=TextureImporterNPOTScale.None;
                bool panel=path.Contains("/Panels/")||path.Contains("/Buttons/")||stem=="TitlePlaque";
                float x=panel?Mathf.Min(160,region.width*.22f):0,y=panel?Mathf.Min(96,region.height*.28f):0;
                importer.spriteBorder=new Vector4(x,y,x,y);importer.maxTextureSize=path.Contains("/Icons/")?512:stem=="CardBack"||stem=="CardFrame"?512:2048;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.compressionQuality=100;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
                var android=importer.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;android.maxTextureSize=importer.maxTextureSize;android.format=TextureImporterFormat.ASTC_6x6;android.compressionQuality=100;importer.SetPlatformTextureSettings(android);
                if(before!=EditorJsonUtility.ToJson(importer))importer.SaveAndReimport();
                if(UIWireframeSprites.Trimmed(path))UIWireframeSprites.Trim(importer,panel,region);
                var imported=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                report.AppendLine("| `"+path.Substring(Source.Length+1)+"` | "+width+"×"+height+" region "+region+" | "+importer.spriteBorder+" | "+importer.maxTextureSize+" |");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(SkinPath));Directory.CreateDirectory(Path.GetDirectoryName(BoardPath));AssetDatabase.Refresh();
            var skin=AssetDatabase.LoadAssetAtPath<UIPackSkin>(SkinPath);if(skin==null){skin=ScriptableObject.CreateInstance<UIPackSkin>();AssetDatabase.CreateAsset(skin,SkinPath);}
            Sprite S(string role)=>AssetDatabase.LoadAssetAtPath<Sprite>(Resolve(paths,role));
            skin.Background=S("GothicBackground");skin.TitlePlaque=S("TitlePlaque");skin.FloorInfo=S("InfoPanel");skin.ScorePanel=S("ScorePanel");skin.ResourcePanel=S("ResourcePanel");skin.InstructionPanel=S("InstructionPanel");skin.StatusBar=S("StatusBar");skin.DrawDefault=S("PrimaryButton");skin.DrawHover=S("PrimaryButton_Hover");skin.DrawDisabled=S("PrimaryButton_Disabled");skin.SecondaryButton=S("SecondaryButton");skin.CardBack=S("CardBack");skin.CardFrame=S("CardFrame");skin.Settings=S("Settings");skin.Gold=S("GoldCoinStack");skin.Relic=S("Relic");skin.Suits=new[]{S("Suit_Club"),S("Suit_Diamond"),S("Suit_Heart"),S("Suit_Spade")};skin.SuitMaterials=new Material[4];skin.SuitMaterials[3]=EmblemMaterial("SpadeBlue",1,new Color(.18f,.57f,1));skin.SuitMaterials[1]=EmblemMaterial("DiamondGold",2,new Color(1,.77f,.16f));skin.Validate();EditorUtility.SetDirty(skin);AssetDatabase.SaveAssetIfDirty(skin);
            var db=CardArtPipeline.EnsureDatabase();db.SetDefaultCardBack(skin.CardBack);EditorUtility.SetDirty(db);AssetDatabase.SaveAssetIfDirty(db);
            WireCard(skin);CreateScreenPrefabs(skin);Atlas(paths);
            Directory.CreateDirectory("Docs");File.WriteAllText("Docs/UI-Pack-Import-Manifest.md",report.ToString(),new UTF8Encoding(false));
            AssetDatabase.SaveAssets();Debug.Log("CURSED_DECK_UI_PACK_READY Sprites="+paths.Length+" Prefabs=BoardView,HUDView,CardView SpriteSwap=Default,Hover,Pressed,Selected,Disabled");
        }
        private static Material EmblemMaterial(string name,float mode,Color tint)
        {
            string path="Assets/_View/Resources/CursedDeck/"+name+".mat";var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/_View/Shaders/FactionEmblem.shader");if(shader==null)throw new InvalidOperationException("Faction emblem shader missing.");
            if(ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Faction emblem shader has compile errors.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}material.shader=shader;material.SetFloat("_Mode",mode);material.SetColor("_FactionTint",tint);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);return material;
        }
        private static string Resolve(string[] paths,string stem)
        {
            var matches=paths.Where(f=>string.Equals(Path.GetFileNameWithoutExtension(f),stem,StringComparison.OrdinalIgnoreCase)).ToArray();
            if(matches.Length!=1)throw new InvalidOperationException("Expected exactly one UI sprite for "+stem+", found "+matches.Length);return matches[0];
        }
        public static void WireCard(UIPackSkin skin)
        {
            if(skin==null)return;
            var root=PrefabUtility.LoadPrefabContents(CardArtPipeline.PrefabPath);
            try
            {
                var front=root.transform.Find("CardMotionRoot/CardFront");
                var legacy=front.Find("OrnateFrame").GetComponent<UnityEngine.UI.Image>();legacy.enabled=false;
                foreach(var image in legacy.GetComponentsInChildren<UnityEngine.UI.Image>(true))image.enabled=false; // baked ruby sockets replace the older faction gem overlay
                var overlay=UIPackLayout.Panel("GothicFrameOverlay",front,skin.CardFrame,Vector2.zero,Vector2.one);overlay.type=UnityEngine.UI.Image.Type.Simple;int headerIndex=front.Find("HeaderBanner").GetSiblingIndex();overlay.transform.SetSiblingIndex(headerIndex-(overlay.transform.GetSiblingIndex()<headerIndex?1:0));
                root.transform.Find("CardMotionRoot/CardBack").GetComponent<UnityEngine.UI.Image>().sprite=skin.CardBack;
                var name=front.Find("HeaderBanner/CharacterNameText") as RectTransform;name.anchorMax=new Vector2(.83f,.96f);
                var top=UIPackLayout.Icon("SuitBadgeTopRight",front,skin.Suits[3],new Vector2(.86f,.885f),new Vector2(.95f,.955f));top.material=skin.SuitMaterials[3];top.transform.SetAsLastSibling();
                var bottom=UIPackLayout.Icon("SuitBadgeBottomLeft",front,skin.Suits[3],new Vector2(.06f,.17f),new Vector2(.16f,.25f));bottom.material=skin.SuitMaterials[3];bottom.transform.SetAsLastSibling();
                PrefabUtility.SaveAsPrefabAsset(root,CardArtPipeline.PrefabPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            PortfolioCardPresentation.ConfigurePrefab();
        }
        public static void CreateScreenPrefabs(UIPackSkin skin)
        {
            var content=Resources.Load<GameContent>("CursedDeck/Content");var holder=new GameObject("HUD Authoring",typeof(RectTransform));
            try{var hud=HUDView.Ensure(holder.transform,content.Font,skin);if(PrefabUtility.IsPartOfPrefabInstance(hud.gameObject))PrefabUtility.UnpackPrefabInstance(hud.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);PrefabUtility.SaveAsPrefabAsset(hud.gameObject,HUDPath);}finally{UnityEngine.Object.DestroyImmediate(holder);}
            bool loaded=File.Exists(BoardPath);GameObject root=loaded?PrefabUtility.LoadPrefabContents(BoardPath):new GameObject("BoardView",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster),typeof(GameView));

            try{root.name="BoardView";root.GetComponent<GameView>().BuildMainScreen(content.Font,content.CardFront,skin.CardBack);PrefabUtility.SaveAsPrefabAsset(root,BoardPath);}
            finally{if(loaded)PrefabUtility.UnloadPrefabContents(root);else UnityEngine.Object.DestroyImmediate(root);}
        }
        private static void Atlas(string[] paths)
        {
            const string path="Assets/_View/Art/CursedDeckUIPack.spriteatlasv2";
            var asset=File.Exists(path)?SpriteAtlasAsset.Load(path):new SpriteAtlasAsset();
            if(File.Exists(path)){var runtime=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);asset.Remove(runtime.GetPackables());}
            asset.Add(paths.Where(p=>!p.Contains("/Backgrounds/")).Select(p=>(UnityEngine.Object)AssetDatabase.LoadAssetAtPath<Sprite>(p)).ToArray());SpriteAtlasAsset.Save(asset,path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(SpriteAtlasImporter)AssetImporter.GetAtPath(path);importer.includeInBuild=true;var pack=importer.packingSettings;pack.padding=4;pack.enableRotation=false;pack.enableTightPacking=false;importer.packingSettings=pack;
            var texture=importer.textureSettings;texture.generateMipMaps=false;texture.readable=false;texture.filterMode=FilterMode.Bilinear;importer.textureSettings=texture;
            importer.SetPlatformSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_6x6,compressionQuality=100});importer.SaveAndReimport();
        }
        public static void CompileAndroidScripts()
        {
            const string folder="TestResults/uipack-android/scripts";Directory.CreateDirectory(folder);
            var result=PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings{target=BuildTarget.Android,group=BuildTargetGroup.Android},folder);
            if(result.assemblies==null||result.assemblies.Count==0)throw new InvalidOperationException("Android compilation produced no assemblies.");
            Debug.Log("CURSED_DECK_UI_ANDROID_SCRIPTS_OK Assemblies="+result.assemblies.Count);
        }
    }
}
