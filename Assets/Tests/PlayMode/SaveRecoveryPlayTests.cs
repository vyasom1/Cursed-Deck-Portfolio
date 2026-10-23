#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using CursedDeck.Bootstrap;
using CursedDeck.Meta;
using CursedDeck.Run;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace CursedDeck.PlayTests
{
    public sealed class SaveRecoveryPlayTests
    {
        [UnityTest] public IEnumerator CorruptEmbeddedCheckpointStillBuildsTitleAndResetIsExplicit()
        {
            string directory=Path.Combine(Path.GetTempPath(),"CursedDeckRecoveryUI-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var store=new MetaStore(Path.Combine(directory,"meta-v1.json"));var profile=new MetaProfile();profile.Begin(42,"classic",null,DateTime.UtcNow);profile.Active.RunCheckpoint="broken checkpoint";store.Save(profile);
            GameBootstrap bootstrap=null;UnityEngine.Events.UnityAction<Scene,LoadSceneMode> loaded=(scene,mode)=>{if(scene.name=="CursedDeck"){bootstrap=UnityEngine.Object.FindFirstObjectByType<GameBootstrap>();bootstrap.StorageDirectory=directory;}};
            SceneManager.sceneLoaded+=loaded;
            try
            {
                yield return SceneManager.LoadSceneAsync("CursedDeck");yield return null;yield return null;
                Assert.That(bootstrap.View,Is.Not.Null);Assert.That(bootstrap.View.StartMenuVisible);Assert.That(bootstrap.View.RecoveryVisible);Assert.That(File.Exists(store.Path+".corrupt.bak"));
                bootstrap.View.Events.Send(new GameCommand(GameCommandKind.ResetProgress));yield return null;
                Assert.That(bootstrap.View.RecoveryVisible,Is.False);Assert.That(bootstrap.Host.CanContinueRun,Is.False);Assert.That(bootstrap.Profile.SoulShards,Is.Zero);Assert.That(File.Exists(store.Path+".reset.bak"));Assert.That(File.Exists(store.Path+".corrupt.bak"));
            }
            finally{SceneManager.sceneLoaded-=loaded;if(bootstrap!=null)UnityEngine.Object.DestroyImmediate(bootstrap.gameObject);if(Directory.Exists(directory))Directory.Delete(directory,true);}
        }
    }
}
#endif
