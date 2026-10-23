using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static partial class PrepareShip
    {
        public const string PortfolioApk="Builds/Release/CursedDeck_Portfolio_Signed.apk";
        private const string Pending="CursedDeck.PortfolioBuild.Pending";
        [MenuItem("Tools/Cursed Deck/Build Portfolio APK")]
        public static void BuildPortfolioAPK()
        {
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
            {
                SessionState.SetBool(Pending,true);
                if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android))
                {SessionState.SetBool(Pending,false);throw new InvalidOperationException("Android target switch failed.");}
                EditorApplication.delayCall+=ResumePortfolioBuild;return;
            }
            PortfolioAndroid();
        }
        [InitializeOnLoadMethod] private static void ResumeAfterReload(){if(SessionState.GetBool(Pending,false))EditorApplication.delayCall+=ResumePortfolioBuild;}
        private static void ResumePortfolioBuild()
        {
            if(!SessionState.GetBool(Pending,false))return;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=ResumePortfolioBuild;return;}
            SessionState.SetBool(Pending,false);PortfolioAndroid();
        }
        [Serializable] private sealed class SigningData{public string alias,password;}
        public static void PortfolioAndroid()
        {
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)throw new InvalidOperationException("Use -buildTarget Android for batch builds.");
            const string privateFolder="Builds/Signing";Directory.CreateDirectory(privateFolder);
            string key=Path.GetFullPath(privateFolder+"/cursed-deck-portfolio.keystore"),credentials=privateFolder+"/portfolio-signing.local.json";
            SigningData signing;
            if(!File.Exists(credentials))
            {
                if(File.Exists(key))throw new InvalidOperationException("Signing key exists but credentials are missing. Restore credentials; never replace the key.");
                signing=new SigningData{alias="cursed-deck-portfolio",password=Guid.NewGuid().ToString("N")+Guid.NewGuid().ToString("N")};
                string jdk=Path.Combine(EditorApplication.applicationContentsPath,"PlaybackEngines/AndroidPlayer/OpenJDK/bin/keytool.exe");
                var start=new System.Diagnostics.ProcessStartInfo(jdk){UseShellExecute=false,CreateNoWindow=true,RedirectStandardError=true,RedirectStandardOutput=true};
                start.EnvironmentVariables["CURSED_DECK_LOCAL_KEY_PASS"]=signing.password;
                start.Arguments="-genkeypair -keystore \""+key+"\" -storetype JKS -alias "+signing.alias+" -keyalg RSA -keysize 3072 -validity 10000 -dname \"CN=Cursed Deck Portfolio\" -storepass:env CURSED_DECK_LOCAL_KEY_PASS -keypass:env CURSED_DECK_LOCAL_KEY_PASS";
                using(var process=System.Diagnostics.Process.Start(start)){string output=process.StandardOutput.ReadToEnd();string error=process.StandardError.ReadToEnd();process.WaitForExit();if(process.ExitCode!=0)throw new InvalidOperationException("Portfolio signing key generation failed: "+error+output);}
                File.WriteAllText(credentials,JsonUtility.ToJson(signing,true));
            }
            else signing=JsonUtility.FromJson<SigningData>(File.ReadAllText(credentials));
            if(signing==null||string.IsNullOrEmpty(signing.password)||string.IsNullOrEmpty(signing.alias)||!File.Exists(key))throw new InvalidOperationException("Invalid local signing configuration. Restore it from your private backup.");
            PlayerSettings.companyName="Cursed Deck";PlayerSettings.productName="Cursed Deck";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.yato.curseddeck");PlayerSettings.bundleVersion="0.4.2";PlayerSettings.Android.bundleVersionCode=6;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=(AndroidSdkVersions)25;PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            string defines=PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android,string.Join(";",defines.Split(';').Where(x=>x!="CURSED_DECK_DEBUG"&&x!="ENABLE_RUNTIME_PIPELINE"&&x!="ENABLE_RUNTIME_PIPELINE_DEV")));
            EditorUserBuildSettings.development=false;EditorUserBuildSettings.allowDebugging=false;EditorUserBuildSettings.connectProfiler=false;
            EditorUserBuildSettings.buildAppBundle=false;EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
            foreach(string fixture in new[]{"PerformanceTestRunInfo.json","PerformanceTestRunSettings.json"})AssetDatabase.DeleteAsset("Assets/Resources/"+fixture);
            string oldName=PlayerSettings.Android.keystoreName,oldAlias=PlayerSettings.Android.keyaliasName,oldStorePass=PlayerSettings.Android.keystorePass,oldAliasPass=PlayerSettings.Android.keyaliasPass;bool oldCustom=PlayerSettings.Android.useCustomKeystore;
            try
            {
                PlayerSettings.Android.useCustomKeystore=true;PlayerSettings.Android.keystoreName=key;PlayerSettings.Android.keyaliasName=signing.alias;PlayerSettings.Android.keystorePass=PlayerSettings.Android.keyaliasPass=signing.password;
                Directory.CreateDirectory(Path.GetDirectoryName(PortfolioApk));Directory.CreateDirectory("TestResults/portfolio-build");
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{PreparePlayableGame.ScenePath},locationPathName=PortfolioApk,target=BuildTarget.Android,options=BuildOptions.None});
                var summary=report.summary;
                File.WriteAllText("TestResults/portfolio-build/summary.json",JsonUtility.ToJson(new BuildEvidence{result=summary.result.ToString(),errors=summary.totalErrors,warnings=summary.totalWarnings,bytes=summary.totalSize,seconds=summary.totalTime.TotalSeconds,architecture="ARM64",backend="IL2CPP",package="com.yato.curseddeck",artifact=PortfolioApk},true));
                if(summary.result!=BuildResult.Succeeded||summary.totalErrors!=0||!File.Exists(PortfolioApk)||new FileInfo(PortfolioApk).Length==0)throw new InvalidOperationException("Portfolio build did not complete cleanly: "+summary.result+" errors="+summary.totalErrors);
                Debug.Log("CURSED_DECK_PORTFOLIO_APK_READY "+Path.GetFullPath(PortfolioApk));
            }
            finally
            {
                PlayerSettings.Android.useCustomKeystore=oldCustom;PlayerSettings.Android.keystoreName=oldName;PlayerSettings.Android.keyaliasName=oldAlias;PlayerSettings.Android.keystorePass=oldStorePass;PlayerSettings.Android.keyaliasPass=oldAliasPass;
            }
        }
    }
}
