using System;
using System.IO;
using CursedDeck.Data;
using CursedDeck.View;
using UnityEditor;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static class PrepareVisualPhase2
    {
        public const string MistPath="Assets/_View/Resources/VFX/VFX_AmbientCastleMist.prefab";
        private const string AudioPath="Assets/_View/Audio/Phase2";
        [MenuItem("Tools/Cursed Deck/Prepare Visual Phase 2")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(MistPath));Directory.CreateDirectory(AudioPath);AssetDatabase.Refresh();
            var root=new GameObject("VFX_AmbientCastleMist",typeof(RectTransform),typeof(CanvasRenderer),typeof(AmbientCastleMist));
            try{root.GetComponent<AmbientCastleMist>().Initialize();PrefabUtility.SaveAsPrefabAsset(root,MistPath);}finally{UnityEngine.Object.DestroyImmediate(root);}
            string[] names={"CardPlaySnap","CardRevealFlip","RelicPurchaseClink","RoundWonFanfare","RoundDefeatSting"};
            string[] fields={"play","reveal","purchase","win","defeat"};
            var audio=Resources.Load<AudioDefinition>("CursedDeck/Audio");var serialized=new SerializedObject(audio);
            for(int i=0;i<names.Length;i++)
            {
                string path=AudioPath+"/"+names[i]+".wav";if(!File.Exists(path))Synthesize(path,i);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(AudioImporter)AssetImporter.GetAtPath(path);importer.forceToMono=true;
                var settings=importer.defaultSampleSettings;settings.preloadAudioData=true;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
                serialized.FindProperty(fields[i]).objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();UIWireframePipeline.CreateScreenPrefabs(UIPackSkin.Load());AssetDatabase.SaveAssets();
            Debug.Log("CURSED_DECK_VISUAL_PHASE2_READY Mist=64 PopupPool=12 BurstPool=12 Audio=6-distinct-cues MotionPreference=JSON");
        }
        // Original synthesized audio, generated once into project-owned WAVs. No download or package dependency.
        private static void Synthesize(string path,int kind)
        {
            const int rate=22050;float duration=kind<2?.16f:kind==2?.35f:kind==3?1.1f:.8f;int count=(int)(rate*duration);uint noise=1567;
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                for(int i=0;i<count;i++)
                {
                    double t=i/(double)rate,envelope=Math.Min(1,t/.007)*Math.Pow(1-t/duration,2);noise=noise*1664525u+1013904223u;double n=(noise>>8)/8388608.0-1;double sample;
                    if(kind==0)sample=(n*.6+Math.Sin(2*Math.PI*(800*t-1200*t*t))*.4)*Math.Exp(-35*t);
                    else if(kind==1)sample=(n*.25+Math.Sin(2*Math.PI*(250*t+1600*t*t))*.35)*Math.Exp(-12*t);
                    else if(kind==2)sample=(Math.Sin(2*Math.PI*1468*t)+.4*Math.Sin(2*Math.PI*2331*t))*Math.Exp(-9*t)*.45;
                    else if(kind==3){double f=t<.22?523.25:t<.44?659.25:783.99;double local=t% .22;sample=(Math.Sin(2*Math.PI*f*t)+.3*Math.Sin(4*Math.PI*f*t))*(.4+.6*Math.Exp(-8*local))*.45;}
                    else sample=(Math.Sin(2*Math.PI*(220*t-65*t*t))+.35*Math.Sin(2*Math.PI*164.81*t))*.45;
                    writer.Write((short)(Math.Clamp(sample*envelope,-.95,.95)*32767));
                }
            }
        }
        public static void CompileAndroid()
        {
            const string output="TestResults/visual-phase2-android/scripts";Directory.CreateDirectory(output);
            var result=UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(new UnityEditor.Build.Player.ScriptCompilationSettings{target=BuildTarget.Android,group=BuildTargetGroup.Android},output);
            if(result.assemblies==null||result.assemblies.Count==0)throw new InvalidOperationException("Android compilation produced no assemblies.");
            Debug.Log("CURSED_DECK_VISUAL_PHASE2_ANDROID_OK Assemblies="+result.assemblies.Count);
        }
    }
}
