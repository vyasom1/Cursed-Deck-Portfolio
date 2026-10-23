using System.IO;
using CursedDeck.Run;
using CursedDeck.Data;
using CursedDeck.View;
using UnityEngine;
namespace CursedDeck.Bootstrap
{
    public sealed partial class GameBootstrap
    {
        private RewardedActions rewardsService;
        private IPurchases purchases;
        private IAnalytics analytics;
        private void InitializeServices(){rewardsService=new RewardedActions(new MockRewardedAds());purchases=new MockPurchases();analytics=new LocalAnalytics(Path.Combine(Path.GetDirectoryName(SavePath),"analytics-local.csv"));}
        private void ServiceCommand(GameCommand command)
        {
            if(command.Kind==GameCommandKind.TutorialComplete)
            {
                CursedDeck.Meta.GameSettings.Instance.hasCompletedTutorial=true;profile.TutorialDone=true;profile.Settings=settings;metaStore.Save(profile);
                if(command.Value==0)StartMetaRun(System.Environment.TickCount,"classic",null,true);
                return;
            }
#if UNITY_EDITOR || CURSED_DECK_DEBUG
            bool completed;
            if(command.Kind==GameCommandKind.MockPurchase){completed=purchases.Purchase("support-demo",out _);host.Notice=completed?"Mock purchase complete. No payment or gameplay reward.":"Mock purchase unavailable.";analytics.Track("mock-iap",host.Controller.State.Seed,host.Controller.State.Floor,host.Controller.State.Round,host.Controller.CurrentRound.State.Score);}
            else{completed=rewardsService.TryComplete(host.Controller,command.Kind==GameCommandKind.TestAdRevive);host.Notice=completed?"TEST AD complete: +1 draw allowance.":"Test reward unavailable: already used or no stock remains.";if(completed){SaveNow();analytics.Track("test-ad",host.Controller.State.Seed,host.Controller.State.Floor,host.Controller.State.Round,host.Controller.CurrentRound.State.Score);}}
            host.Publish(GameFeedback.Draw);
#endif
        }
        private void InitializeAudio(){var data=Resources.Load<AudioDefinition>("CursedDeck/Audio");if(data!=null)View.gameObject.AddComponent<AudioFeedback>().Initialize(bus,data.Ambient,data.Card,data.Combo,settings.Sound,new FeedbackAudioClips(data.Play,data.Reveal,data.Purchase,data.Win,data.Defeat));}
    }
}
