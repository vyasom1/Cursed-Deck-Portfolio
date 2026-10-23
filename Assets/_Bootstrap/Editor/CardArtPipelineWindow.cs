using System;
using System.IO;
using System.Globalization;
using System.Text;
using CursedDeck.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.U2D;
namespace CursedDeck.Bootstrap.Editor
{
    public sealed class CardArtPipelineWindow:EditorWindow
    {
        private Label status;
        [MenuItem("Tools/Cursed Deck/Card Art Pipeline")]
        public static void Open(){var window=GetWindow<CardArtPipelineWindow>("Card Art Pipeline");window.minSize=new Vector2(460,260);}
        public void CreateGUI()
        {
            var root=rootVisualElement;root.style.paddingLeft=root.style.paddingRight=16;root.style.paddingTop=16;
            root.Add(new Label("CURSED DECK / 52 CHARACTER CARDS"){style={unityFontStyleAndWeight=FontStyle.Bold,fontSize=18}});
            root.Add(new HelpBox("Save generated PNGs in Assets/Resources/CardArt, named Spades_1.png through Clubs_13.png. Rank 1 = Ace; 11/12/13 = Jack/Queen/King. Import replaces art bindings only; names and source PNGs are preserved.",HelpBoxMessageType.Info));
            var export=new UnityEngine.UIElements.Button(()=>Run(()=>{CardArtPipeline.ExportPrompts();return "Exported 52 prompts to Docs/CARD_GEN_PROMPTS.txt";})){text="1. Export 52 image prompts",name="ExportPrompts"};root.Add(export);
            var import=new UnityEngine.UIElements.Button(()=>Run(()=>CardArtPipeline.ImportAll(true))){text="2. Batch import + generate missing fallbacks",name="BatchImport"};root.Add(import);
            root.Add(new UnityEngine.UIElements.Button(()=>EditorUtility.RevealInFinder(Path.GetFullPath(CardArtPipeline.SourceFolder))){text="Open source art folder"});
            status=new Label("Fallbacks are procedural placeholders. No online API or package is required.");status.style.whiteSpace=WhiteSpace.Normal;status.style.marginTop=12;root.Add(status);
        }
        private void Run(Func<string> action){try{status.text=action();}catch(OperationCanceledException){status.text="Import cancelled; source images were preserved.";}catch(Exception ex){status.text=ex.Message;Debug.LogException(ex);}}
    }
    public static class CardArtPipeline
    {
        public const string SourceFolder="Assets/Resources/CardArt";
        public const string FallbackFolder="Assets/_View/Art/TCG/Fallbacks";
        public const string StyleFolder="Assets/_View/Art/TCG/Style";
        public const string DatabasePath="Assets/_Data/CardCharacterDatabase.asset";
        private const string LegacyDatabasePath="Assets/_Data/Resources/CardCharacterDatabase.asset";
        public const string PrefabPath="Assets/_View/Resources/TCG/CardView.prefab";
        public static CardCharacterDatabase EnsureDatabase()
        {
            var db=AssetDatabase.LoadAssetAtPath<CardCharacterDatabase>(DatabasePath);
            if(db==null&&AssetDatabase.LoadAssetAtPath<CardCharacterDatabase>(LegacyDatabasePath)!=null)
            {
                string error=AssetDatabase.MoveAsset(LegacyDatabasePath,DatabasePath);
                if(!string.IsNullOrEmpty(error))throw new InvalidOperationException(error);
                db=AssetDatabase.LoadAssetAtPath<CardCharacterDatabase>(DatabasePath);
            }
            if(db==null){Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath));AssetDatabase.Refresh();db=ScriptableObject.CreateInstance<CardCharacterDatabase>();db.SetCharacters(CardCharacterMatrix.Create());AssetDatabase.CreateAsset(db,DatabasePath);}
            if(db.UpgradeCatalog(CardCharacterMatrix.Create(),3)){EditorUtility.SetDirty(db);AssetDatabase.SaveAssetIfDirty(db);}
            db.Validate(false);return db;
        }
        public static string FileName(int suit,int rank)=>CardCharacterMatrix.SuitNames[suit]+"_"+rank.ToString(CultureInfo.InvariantCulture)+".png";
        public static bool TryParseFileName(string file,out int suit,out int rank)
        {
            suit=-1;rank=0;if(Path.GetExtension(file)!=".png")return false;
            string name=Path.GetFileNameWithoutExtension(file);int split=name.LastIndexOf('_');if(split<1)return false;
            string suitName=name.Substring(0,split),number=name.Substring(split+1);
            for(int i=0;i<4;i++)if(CardCharacterMatrix.SuitNames[i]==suitName)suit=i;
            return suit>=0&&int.TryParse(number,NumberStyles.None,CultureInfo.InvariantCulture,out rank)&&rank>=1&&rank<=13&&rank.ToString(CultureInfo.InvariantCulture)==number;
        }
        public static string Prompts(CardCharacterDatabase db)
        {
            db.Validate(false);var text=new StringBuilder("CURSED DECK — 52 CHARACTER ART PROMPTS\nSave each generated image using the filename above its prompt.\n\n");
            foreach(int suit in new[]{3,2,1,0})for(int rank=1;rank<=13;rank++)
            {
                var c=db.Find(suit,rank);text.AppendLine("["+FileName(suit,rank)+"]");
                text.Append("Dark fantasy card art portrait, ").Append(c.CharacterName).Append(", ").Append(c.FactionDescription).Append(", ").Append(c.Pose).AppendLine(", dark atmospheric background, painterly TCG card style, vertical 3:4 aspect ratio, no text.").AppendLine();
            }
            return text.ToString();
        }
        public static void ExportPrompts()=>ExportPrompts("Docs/CARD_GEN_PROMPTS.txt");
        public static void ExportPrompts(string path)
        {
            string folder=Path.GetDirectoryName(Path.GetFullPath(path));Directory.CreateDirectory(folder);
            File.WriteAllText(path,Prompts(EnsureDatabase()),new UTF8Encoding(false));
        }
        public static void FullSetup()
        {
            string result=ImportAll(true);ExportPrompts();AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("CURSED_DECK_FULL_SETUP "+result+" Database="+DatabasePath);
        }
        public static void Prepare(){Debug.Log("CURSED_DECK_TCG_READY "+ImportAll());ExportPrompts();}
        public static string ImportAll(bool progress=false)
        {
            Directory.CreateDirectory(SourceFolder);Directory.CreateDirectory(FallbackFolder);Directory.CreateDirectory(StyleFolder);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var db=EnsureDatabase();string result=BindArt(db,SourceFolder,FallbackFolder,progress);
            PortfolioCardPresentation.Prepare(db);CardTCGPrefab.Ensure(db);CardTCGPrefab.WireFeedback(db);EnsureAtlases(db);
            var content=Resources.Load<GameContent>("CursedDeck/Content");
            if(content==null)throw new InvalidOperationException("Game content missing.");
            content.SetCharacters(db);EditorUtility.SetDirty(content);AssetDatabase.SaveAssetIfDirty(content);
            UIWireframePipeline.WireCard(CursedDeck.View.UIPackSkin.Load());
            PortfolioCardPresentation.ConfigurePrefab();
            return result;
        }
        // Public to permit isolated importer tests without writing into the user's source-art directory.
        public static string BindArt(CardCharacterDatabase db,string sourceFolder,string fallbackFolder,bool progress=false)
        {
            CheckAssetFolder(sourceFolder);CheckAssetFolder(fallbackFolder);db.Validate(false);Directory.CreateDirectory(sourceFolder);Directory.CreateDirectory(fallbackFolder);Directory.CreateDirectory(StyleFolder);AssetDatabase.Refresh();
            var files=new System.Collections.Generic.Dictionary<string,string>(StringComparer.Ordinal);
            foreach(string file in Directory.GetFiles(sourceFolder,"*.png"))if(TryParseFileName(file,out _,out _))files[Path.GetFileName(file)]=file.Replace('\\','/');
            var sources=new Sprite[52];var fallbacks=new Sprite[52];var emblems=new Sprite[4];int imported=0;
            try
            {
                for(int suit=0;suit<4;suit++)emblems[suit]=CardArtTextures.EnsureEmblem(suit);
                for(int i=0;i<52;i++)
                {
                    var c=db.Characters[i];if(progress&&EditorUtility.DisplayCancelableProgressBar("Cursed Deck / Card Art",c.CharacterName,i/52f))throw new OperationCanceledException();
                    string fallback=fallbackFolder+"/"+FileName(c.Suit,c.Rank);CardArtTextures.EnsurePortrait(fallback,c.Suit,c.Rank,c.FactionColor);fallbacks[i]=ConfigureSprite(fallback);
                    if(files.TryGetValue(FileName(c.Suit,c.Rank),out string source)){sources[i]=ConfigureSprite(source);if(sources[i]!=null)imported++;}
                }
                Undo.RecordObject(db,"Bind Cursed Deck character art");
                for(int i=0;i<52;i++)db.SetImportedArt(i,sources[i],fallbacks[i],emblems[db.Characters[i].Suit]);
                db.Validate();EditorUtility.SetDirty(db);if(AssetDatabase.Contains(db))AssetDatabase.SaveAssetIfDirty(db);
                foreach(string file in Directory.GetFiles(sourceFolder,"*.png"))if(!TryParseFileName(file,out _,out _))Debug.LogWarning("Ignored non-canonical card art filename: "+file);
                return "Characters=52 Imported="+imported+" Fallbacks="+(52-imported);
            }
            finally{if(progress)EditorUtility.ClearProgressBar();}
        }
        private static void CheckAssetFolder(string folder)
        {
            string full=Path.GetFullPath(folder),assets=Path.GetFullPath(Application.dataPath)+Path.DirectorySeparatorChar;
            if(!full.StartsWith(assets,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Art folders must be inside this project's Assets directory.");
        }
        public static Sprite ConfigureSprite(string path)
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new InvalidOperationException("Cannot import PNG: "+path);
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            bool changed=importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Single||importer.maxTextureSize!=512||importer.compressionQuality!=100||importer.wrapMode!=TextureWrapMode.Clamp||importer.filterMode!=FilterMode.Bilinear||!importer.alphaIsTransparency||importer.textureCompression!=TextureImporterCompression.CompressedHQ||importer.mipmapEnabled||importer.isReadable||settings.spriteMeshType!=SpriteMeshType.FullRect||importer.npotScale!=TextureImporterNPOTScale.None;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.compressionQuality=100;
            importer.spritePixelsPerUnit=100;importer.mipmapEnabled=false;importer.isReadable=false;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.npotScale=TextureImporterNPOTScale.None;
            importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            var android=importer.GetPlatformTextureSettings("Android");changed|=!android.overridden||android.maxTextureSize!=512||android.format!=TextureImporterFormat.ASTC_6x6||android.compressionQuality!=100;
            android.name="Android";android.overridden=true;android.maxTextureSize=512;android.format=TextureImporterFormat.ASTC_6x6;android.compressionQuality=100;importer.SetPlatformTextureSettings(android);
            if(changed)importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        public static void EnsureAtlases(CardCharacterDatabase db)
        {
            Directory.CreateDirectory("Assets/_View/Art/TCG/Atlases");EditorSettings.spritePackerMode=SpritePackerMode.SpriteAtlasV2;
            if(EditorSettings.spritePackerMode!=SpritePackerMode.SpriteAtlasV2)throw new InvalidOperationException("SpriteAtlas V2 must be enabled.");
            for(int suit=0;suit<4;suit++)
            {
                var sprites=new System.Collections.Generic.List<UnityEngine.Object>();
                foreach(var c in db.Characters)if(c.Suit==suit){sprites.Add(c.FallbackArt);if(c.SourceArt!=null)sprites.Add(c.SourceArt);if(c.DisplayPortrait!=c.Portrait)sprites.Add(c.DisplayPortrait);}
                Atlas("Characters-"+CardCharacterMatrix.SuitNames[suit],sprites.ToArray(),2048);
            }
            var style=new System.Collections.Generic.List<UnityEngine.Object>();foreach(string file in Directory.GetFiles(StyleFolder,"*.png"))style.Add(AssetDatabase.LoadAssetAtPath<Sprite>(file));
            if(style.Count>0)Atlas("CardTCG-Style",style.ToArray(),1024);
        }
        private static void Atlas(string name,UnityEngine.Object[] sprites,int size)
        {
            string path="Assets/_View/Art/TCG/Atlases/"+name+".spriteatlasv2";var asset=File.Exists(path)?SpriteAtlasAsset.Load(path):new SpriteAtlasAsset();
            if(File.Exists(path)){var runtime=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);asset.Remove(runtime.GetPackables());}
            asset.Add(sprites);SpriteAtlasAsset.Save(asset,path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(SpriteAtlasImporter)AssetImporter.GetAtPath(path);importer.includeInBuild=true;
            var packing=importer.packingSettings;packing.padding=4;packing.enableRotation=false;packing.enableTightPacking=false;importer.packingSettings=packing;
            var texture=importer.textureSettings;texture.generateMipMaps=false;texture.filterMode=FilterMode.Bilinear;texture.readable=false;importer.textureSettings=texture;
            importer.SetPlatformSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=size,format=TextureImporterFormat.ASTC_6x6,compressionQuality=100});importer.SaveAndReimport();
        }
    }
    // [UNITY-SKILL:SPRITEATLAS] New built-in character atlases; original UI atlas remains unchanged.
    public sealed class CardArtBuildPreparation:IPreprocessBuildWithReport
    {
        public int callbackOrder=>-100;
        public void OnPreprocessBuild(BuildReport report){PortfolioRelics.Prepare();CardArtPipeline.ImportAll();}
    }
}
