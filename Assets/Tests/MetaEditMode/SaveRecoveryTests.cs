using System;
using System.IO;
using CursedDeck.Meta;
using CursedDeck.Run;
using NUnit.Framework;
namespace CursedDeck.MetaTests
{
    public sealed class SaveRecoveryTests
    {
        private string directory;
        [SetUp] public void Setup(){directory=Path.Combine(Path.GetTempPath(),"CursedDeckRecovery-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);}
        [TearDown] public void Cleanup(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
        private MetaStore Store()=>new MetaStore(Path.Combine(directory,"meta.json"));
        [Test] public void BothCorruptProfilesAreQuarantinedAndDefaultProfileLoads()
        {
            var store=Store();File.WriteAllText(store.Path,"broken primary");File.WriteAllText(store.Path+".bak","broken backup");
            var profile=store.Load(out string warning);Assert.That(profile.SoulShards,Is.Zero);Assert.That(store.RecoveredDefault);Assert.That(warning,Does.Contain("Default profile restored"));
            Assert.That(File.ReadAllText(store.Path+".corrupt.bak"),Is.EqualTo("broken primary"));Assert.That(File.ReadAllText(store.Path+".bak.corrupt.bak"),Is.EqualTo("broken backup"));Assert.That(File.ReadAllText(Path.Combine(directory,"save-recovery.log")),Does.Contain("raw preserved=True"));
            store.Save(profile);Assert.That(MetaStore.FromJson(File.ReadAllText(store.Path)).SoulShards,Is.Zero);
        }
        [Test] public void ValidBackupSurvivesTheFirstRecoverySave()
        {
            var store=Store();var profile=new MetaProfile();store.Save(profile);profile.TutorialDone=true;store.Save(profile);string backup=File.ReadAllText(store.Path+".bak");File.WriteAllText(store.Path,"bad");
            var restored=store.Load(out string warning);Assert.That(warning,Does.Contain("backup"));Assert.That(store.RecoveredDefault,Is.False);Assert.That(restored.TutorialDone,Is.False);store.Save(restored);
            Assert.That(File.ReadAllText(store.Path+".bak"),Is.EqualTo(backup));Assert.That(File.ReadAllText(store.Path+".corrupt.bak"),Is.EqualTo("bad"));
        }
        [Test] public void EmbeddedBadCheckpointCannotEscapeTheProfileRecoveryBoundary()
        {
            var store=Store();var profile=new MetaProfile();profile.Begin(42,"classic",null,DateTime.UtcNow);profile.Active.RunCheckpoint="not valid run JSON";store.Save(profile);string original=File.ReadAllText(store.Path);
            var restored=store.Load(out _,json=>{RunController.FromJson(json);return true;});Assert.That(store.RecoveredDefault);Assert.That(restored.Active,Is.Null);Assert.That(File.ReadAllText(store.Path+".corrupt.bak"),Is.EqualTo(original));
        }
        [Test] public void RepeatedRecoveryNeverOverwritesAnEarlierCorruptBackup()
        {
            var store=Store();File.WriteAllText(store.Path,"first damaged save");store.Load(out _);File.WriteAllText(store.Path,"second damaged save");store.Load(out _);
            Assert.That(File.ReadAllText(store.Path+".corrupt.bak"),Is.EqualTo("first damaged save"));Assert.That(Directory.GetFiles(directory,"*.corrupt.bak").Length,Is.EqualTo(2));
        }
        [Test] public void FailureToPreserveLockedDataBlocksSubsequentWrites()
        {
            var store=Store();File.WriteAllText(store.Path,"locked original");MetaProfile restored;
            using(var locked=new FileStream(store.Path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))restored=store.Load(out _);
            Assert.That(store.SaveBlocked);Assert.Throws<IOException>(()=>store.Save(restored));Assert.That(File.ReadAllText(store.Path),Is.EqualTo("locked original"));
        }
        [Test] public void RecoveredRunSavePreservesItsKnownGoodBackup()
        {
            var store=new RunSaveStore(Path.Combine(directory,"run.json")){CorruptionRecovery=RecoveryJournal.Preserve};var run=new RunController(42,new RelicSpec[0]);store.Save(run);run.Execute(new RunAction(RunActionKind.Play,run.Deal.SolutionSlots[0]));store.Save(run);string backup=File.ReadAllText(store.Path+".bak");File.WriteAllText(store.Path,"broken run");
            Assert.That(store.TryLoad(out var restored,out _));store.Save(restored);Assert.That(File.ReadAllText(store.Path+".bak"),Is.EqualTo(backup));Assert.That(File.ReadAllText(store.Path+".corrupt.bak"),Is.EqualTo("broken run"));
        }
        [Test] public void ExplicitResetArchivesRatherThanDeletingProgress()
        {
            var store=Store();store.Save(new MetaProfile());string old=File.ReadAllText(store.Path);RecoveryJournal.ArchiveForReset(store.Path);Assert.That(File.Exists(store.Path),Is.False);Assert.That(File.ReadAllText(store.Path+".reset.bak"),Is.EqualTo(old));
        }
    }
}
