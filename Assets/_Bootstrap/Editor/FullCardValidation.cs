using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static class FullCardValidation
    {
        public static void CompileAndroidScripts()
        {
            string folder=Path.GetFullPath("TestResults/fullcards-android/scripts");Directory.CreateDirectory(folder);
            var settings=new ScriptCompilationSettings{target=BuildTarget.Android,group=BuildTargetGroup.Android};
            var result=PlayerBuildInterface.CompilePlayerScripts(settings,folder);
            if(result.assemblies==null||result.assemblies.Count==0)throw new InvalidOperationException("Android player script compilation produced no assemblies.");
            Debug.Log("CURSED_DECK_ANDROID_SCRIPTS_OK Assemblies="+result.assemblies.Count);
        }
    }
}
