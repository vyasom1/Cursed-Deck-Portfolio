using System;
using System.IO;
using System.Collections.Generic;
using CursedDeck.Data;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D;

namespace CursedDeck.Bootstrap.Editor
{
    public static class PreparePlayableGame
    {
        public const string ScenePath="Assets/Scenes/CursedDeck.unity";
        public const string AtlasPath="Assets/_View/Art/CursedDeck.spriteatlasv2";
        public const string ContentPath="Assets/_Data/Resources/CursedDeck/Content.asset";
        [MenuItem("Cursed Deck/Prepare Playable Scene")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/_View/Art"); Directory.CreateDirectory("Assets/_Data/Resources/CursedDeck");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            // Essentials in Assets/TextMesh Pro came from the already-installed UI
            // package. Keep those checked-in GUIDs; never download or add packages here.
            if(Resources.Load<TMP_Settings>("TMP Settings")==null) throw new InvalidOperationException("Restore the checked-in bundled TMP resources before authoring.");
            Sprite front=MakeSprite("CardFront",-2),back=MakeSprite("CardBack",-1);
            var icons=new Sprite[4]; for(int i=0;i<4;i++) icons[i]=MakeSprite("Suit-"+i,i);
            EnsureAtlas();
            var rules=AssetDatabase.LoadAssetAtPath<RunConfigDefinition>("Assets/_Data/Resources/CursedDeck/RunRules.asset");
            if(rules==null) { rules=ScriptableObject.CreateInstance<RunConfigDefinition>(); AssetDatabase.CreateAsset(rules,"Assets/_Data/Resources/CursedDeck/RunRules.asset"); }
            var content=AssetDatabase.LoadAssetAtPath<GameContent>(ContentPath);
            if(content==null) { content=ScriptableObject.CreateInstance<GameContent>(); AssetDatabase.CreateAsset(content,ContentPath); }
            var font=Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if(font==null) throw new InvalidOperationException("Installed TMP essentials did not provide a font.");
            string[] relicPaths=AssetDatabase.FindAssets("t:RelicDefinition",new[] {"Assets/_Data/Relics"});
            Array.Sort(relicPaths,(a,b)=>StringComparer.Ordinal.Compare(AssetDatabase.GUIDToAssetPath(a),AssetDatabase.GUIDToAssetPath(b)));
            var serialized=new SerializedObject(content);
            var relics=serialized.FindProperty("relics"); relics.arraySize=relicPaths.Length;
            for(int i=0;i<relicPaths.Length;i++) relics.GetArrayElementAtIndex(i).objectReferenceValue=AssetDatabase.LoadAssetAtPath<RelicDefinition>(AssetDatabase.GUIDToAssetPath(relicPaths[i]));
            serialized.FindProperty("layout").objectReferenceValue=AssetDatabase.LoadAssetAtPath<BoardLayoutDefinition>("Assets/_Data/Layouts/TriPeaks.asset");
            serialized.FindProperty("rules").objectReferenceValue=rules; serialized.FindProperty("font").objectReferenceValue=font;
            serialized.FindProperty("cardFront").objectReferenceValue=front; serialized.FindProperty("cardBack").objectReferenceValue=back;
            var iconProperty=serialized.FindProperty("suitIcons"); iconProperty.arraySize=4;
            for(int i=0;i<4;i++) iconProperty.GetArrayElementAtIndex(i).objectReferenceValue=icons[i];
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(content); AssetDatabase.SaveAssets(); content.Validate();
            if(!File.Exists(ScenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var camera=new GameObject("Main Camera",typeof(Camera)); camera.tag="MainCamera";
                var component=camera.GetComponent<Camera>(); component.clearFlags=CameraClearFlags.SolidColor; component.backgroundColor=new Color(.025f,.075f,.065f); component.orthographic=true;
                camera.transform.position=new Vector3(0,0,-10);
                var bootstrap=new GameObject("GameBootstrap",typeof(GameBootstrap)).GetComponent<GameBootstrap>();
                var boot=new SerializedObject(bootstrap); boot.FindProperty("content").objectReferenceValue=content;
                boot.FindProperty("inputActions").objectReferenceValue=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
                boot.ApplyModifiedPropertiesWithoutUndo(); EditorSceneManager.SaveScene(scene,ScenePath);
            }
            var scenes=new List<EditorBuildSettingsScene> {new EditorBuildSettingsScene(ScenePath,true)};
            foreach(var scene in EditorBuildSettings.scenes) if(scene.path!=ScenePath) scenes.Add(scene);
            EditorBuildSettings.scenes=scenes.ToArray();
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft=false; PlayerSettings.allowedAutorotateToLandscapeRight=false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false; PlayerSettings.defaultScreenWidth=1080; PlayerSettings.defaultScreenHeight=1920;
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            Debug.Log("CURSED_DECK_SCENE_READY: "+ScenePath);
        }
        // [UNITY-SKILL:SPRITEATLAS] Local always-loaded UI atlas, authored with V2 API.
        public static void EnsureAtlas()
        {
            EditorSettings.spritePackerMode=SpritePackerMode.SpriteAtlasV2;
            if(EditorSettings.spritePackerMode!=SpritePackerMode.SpriteAtlasV2) throw new InvalidOperationException("Sprite packing was not enabled.");
            var asset=File.Exists(AtlasPath) ? SpriteAtlasAsset.Load(AtlasPath) : new SpriteAtlasAsset();
            if(!File.Exists(AtlasPath))
            {
                var sprites=new UnityEngine.Object[6]; sprites[0]=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_View/Art/CardFront.png"); sprites[1]=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_View/Art/CardBack.png");
                for(int i=0;i<4;i++) sprites[i+2]=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_View/Art/Suit-"+i+".png");
                asset.Add(sprites); SpriteAtlasAsset.Save(asset,AtlasPath); AssetDatabase.ImportAsset(AtlasPath,ImportAssetOptions.ForceSynchronousImport);
            }
            var importer=(SpriteAtlasImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.includeInBuild=true;
            var packing=importer.packingSettings; packing.padding=4; packing.enableRotation=false; packing.enableTightPacking=false; importer.packingSettings=packing;
            var texture=importer.textureSettings; texture.generateMipMaps=false; texture.filterMode=FilterMode.Bilinear; importer.textureSettings=texture;
            importer.SetPlatformSettings(new TextureImporterPlatformSettings {name="Android",overridden=true,maxTextureSize=1024,format=TextureImporterFormat.ASTC_4x4});
            importer.SaveAndReimport();
            var runtime=AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
            SpriteAtlasUtility.PackAtlases(new[] {runtime},EditorUserBuildSettings.activeBuildTarget,false);
        }
        private static Sprite MakeSprite(string name,int kind)
        {
            string path="Assets/_View/Art/"+name+".png";
            if(!File.Exists(path))
            {
                int w=kind<0 ? 64 : 64,h=kind<0 ? 96 : 64;
                var texture=new Texture2D(w,h,TextureFormat.RGBA32,false); var pixels=new Color32[w*h];
                for(int y=0;y<h;y++) for(int x=0;x<w;x++)
                {
                    Color color=Color.clear;
                    if(kind<0)
                    {
                        float dx=x-Mathf.Clamp(x,6,w-7),dy=y-Mathf.Clamp(y,6,h-7);
                        if(dx*dx+dy*dy<=36)
                        {
                            bool border=x<3 || x>w-4 || y<3 || y>h-4;
                            color=border ? new Color(.82f,.68f,.38f) : kind==-2 ? new Color(.96f,.94f,.86f) : new Color(.08f,.23f,.20f);
                            if(kind==-1 && !border && ((x+y)%20==0 || (x-y+200)%20==0)) color=new Color(.38f,.44f,.29f);
                            if(kind==-1 && Mathf.Abs(x-32)+Mathf.Abs(y-48)<11) color=new Color(.82f,.68f,.38f);
                        }
                    }
                    else
                    {
                        float px=(x-31.5f)/28f,py=(y-31.5f)/28f; bool inside=false;
                        if(kind==1) inside=Mathf.Abs(px)+Mathf.Abs(py)<.93f;
                        if(kind==2) { float v=px*px+(py+.12f)*(py+.12f)-.65f; inside=v*v*v-px*px*(py+.12f)*(py+.12f)*(py+.12f)<0; }
                        if(kind==3)
                        {
                            float v=px*px+(py-.08f)*(py-.08f)-.62f;
                            inside=v*v*v+px*px*(py-.08f)*(py-.08f)*(py-.08f)<0 || (py<-.42f && py>-.94f && Mathf.Abs(px)<.12f+(-py-.42f)*.25f);
                        }
                        if(kind==0) inside=(px*px+(py-.43f)*(py-.43f)<.20f) || ((px-.4f)*(px-.4f)+(py+.08f)*(py+.08f)<.20f) ||
                            ((px+.4f)*(px+.4f)+(py+.08f)*(py+.08f)<.20f) || (py<-.2f && py>-.92f && Mathf.Abs(px)<.13f+(-py-.2f)*.2f);
                        if(inside) color=Color.white;
                    }
                    pixels[y*w+x]=color;
                }
                texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path); importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100; importer.mipmapEnabled=false; importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.spriteBorder=kind<0 ? new Vector4(7,7,7,7) : Vector4.zero;
            importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
    public sealed class CardAtlasPrebuild : IPreprocessBuildWithReport
    {
        public int callbackOrder=>0;
        public void OnPreprocessBuild(BuildReport report) { PreparePlayableGame.EnsureAtlas(); }
    }
}
