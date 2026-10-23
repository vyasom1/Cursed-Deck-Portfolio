using System;
using System.IO;
using CursedDeck.Data;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
namespace CursedDeck.Bootstrap.Editor
{
    public static partial class PrepareShip
    {
        public static void BuildContent()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var clips=new AudioClip[3];string[] names={"CasinoAfterHours","CardFlick","ComboBell"};
            for(int i=0;i<3;i++)
            {
                string path="Assets/_View/Audio/"+names[i]+".wav";var importer=(AudioImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new InvalidOperationException("Audio missing: "+path);
                importer.forceToMono=true;var settings=importer.defaultSampleSettings;settings.preloadAudioData=true;settings.compressionFormat=i==0?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;settings.loadType=i==0?AudioClipLoadType.CompressedInMemory:AudioClipLoadType.DecompressOnLoad;settings.quality=.65f;importer.defaultSampleSettings=settings;importer.SaveAndReimport();clips[i]=AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
            string target="Assets/_Data/Resources/CursedDeck/Audio.asset";var data=AssetDatabase.LoadAssetAtPath<AudioDefinition>(target);if(data==null){data=ScriptableObject.CreateInstance<AudioDefinition>();AssetDatabase.CreateAsset(data,target);}var s=new SerializedObject(data);s.FindProperty("ambient").objectReferenceValue=clips[0];s.FindProperty("card").objectReferenceValue=clips[1];s.FindProperty("combo").objectReferenceValue=clips[2];s.ApplyModifiedPropertiesWithoutUndo();
            var scene=EditorSceneManager.OpenScene(PreparePlayableGame.ScenePath);var camera=Camera.main;if(camera==null)throw new InvalidOperationException("Camera missing.");if(camera.GetComponent<AudioListener>()==null)camera.gameObject.AddComponent<AudioListener>();EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();Debug.Log("CURSED_DECK_POLISH_READY");
        }
        public static void Android()
        {
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)throw new InvalidOperationException("Launch Unity with -buildTarget Android.");
            PlayerSettings.companyName="Cursed Deck";PlayerSettings.productName="Cursed Deck";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.yato.curseddeck");PlayerSettings.bundleVersion="0.3.0";PlayerSettings.Android.bundleVersionCode=3;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel25;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            EditorUserBuildSettings.buildAppBundle=false;EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
            // These are test-runner generated fixtures, not game content.
            foreach(string fixture in new[]{"PerformanceTestRunInfo.json","PerformanceTestRunSettings.json"})
                AssetDatabase.DeleteAsset("Assets/Resources/"+fixture);
            Directory.CreateDirectory("Builds/Android");Directory.CreateDirectory("TestResults/phase9-build");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{PreparePlayableGame.ScenePath},locationPathName="Builds/Android/CursedDeck-preview-arm64.apk",target=BuildTarget.Android,options=BuildOptions.None});
            var summary=report.summary;File.WriteAllText("TestResults/phase9-build/summary.json",JsonUtility.ToJson(new BuildEvidence{result=summary.result.ToString(),errors=summary.totalErrors,warnings=summary.totalWarnings,bytes=summary.totalSize,seconds=summary.totalTime.TotalSeconds,architecture="ARM64",backend="IL2CPP",package="com.yato.curseddeck",artifact="Builds/Android/CursedDeck-preview-arm64.apk"},true));
            if(summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Android build failed: "+summary.result+" errors="+summary.totalErrors);Debug.Log("CURSED_DECK_APK_READY");
        }
        [Serializable] private sealed class BuildEvidence{public string result,architecture,backend,package,artifact;public int errors,warnings;public ulong bytes;public double seconds;}
    }
}
