using System;
using System.IO;
using CursedDeck.Meta;
using CursedDeck.Run;
namespace CursedDeck.Bootstrap
{
    public sealed partial class GameBootstrap
    {
        private bool recoveryPending,recoveryDefault;
        private bool recoveryWritable=true;
        private bool ReportCorruption(string path,Exception error)
        { recoveryPending=true;bool preserved=RecoveryJournal.Preserve(path,error);recoveryWritable&=preserved;return preserved; }
        private string RecoveryMessage()
        {
            if(!recoveryWritable||metaStore.SaveBlocked)return "Damaged data could not be copied.\nOriginal files remain intact.\nSaving is paused; check storage or reset progress.";
            if(metaStore.RecoveredDefault)return "Default profile restored.\nDamaged data was preserved.\nContinue safely, or reset progress.";
            if(recoveryDefault)return "Your profile was kept. A new run is ready.\nDamaged data was preserved.\nContinue safely, or reset progress.";
            return "A previous valid save was restored.\nDamaged data was preserved.\nContinue safely, or reset progress.";
        }
        private void ResetRecoveredProgress()
        {
            if(!recoveryPending)return;
            try
            {
                foreach(string path in new[]{SavePath,SavePath+".bak",metaStore.Path,metaStore.Path+".bak"})RecoveryJournal.ArchiveForReset(path);
                profile=new MetaProfile();settings.hasCompletedTutorial=false;profile.Settings=settings;GameSettings.Bind(settings);validatedProfileCheckpoint=null;metaStore=new MetaStore(metaStore.Path);metaStore.Save(profile);
                store=new RunSaveStore(SavePath){CorruptionRecovery=ReportCorruption};recoveryWritable=true;recoveryPending=false;host.ShowStartMenu(false);
                host.Replace(new RunController(42,content.CreateCatalog(),content.Rules.CreateConfig(),content.Layout.CreateLayout()));
                View.DismissRecovery();View.QueueTutorial(true);PublishMeta();
            }
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException)
            { host.Notice="Progress could not be reset. Check available storage and try again.";host.Publish(); }
        }
    }
}
