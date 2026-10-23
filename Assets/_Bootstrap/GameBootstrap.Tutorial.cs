using CursedDeck.Meta;
using CursedDeck.Run;
namespace CursedDeck.Bootstrap
{
    public sealed partial class GameBootstrap
    {
        private void InitializeTutorialSettings()
        {
            // Existing profiles migrate their old completion flag; do not force returning players through FTUE.
            if(profile.Settings!=null)settings=profile.Settings;
            settings.hasCompletedTutorial|=profile.TutorialDone;
            profile.TutorialDone=settings.hasCompletedTutorial;profile.Settings=settings;GameSettings.Bind(settings);
        }
        private void StartTutorial()
        {
            if(metaStore.SaveBlocked||!recoveryWritable){host.Notice="Practice is unavailable until the save recovery is resolved.";host.Publish();return;}
            settings.hasCompletedTutorial=false;profile.TutorialDone=false;profile.Settings=settings;metaStore.Save(profile);
            View.QueueTutorial(false);
            var layout=content.Layout.CreateLayout();
            host.BeginTutorial(new RunController(FirstMatchGuide.For(layout).Seed,content.CreateCatalog(),content.Rules.CreateConfig(),layout));
        }
    }
}
