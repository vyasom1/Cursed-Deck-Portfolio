using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using CursedDeck.Meta;
using CursedDeck.Run;
using CursedDeck.Data;
using UnityEngine;
namespace CursedDeck.Bootstrap
{
    public sealed partial class GameBootstrap
    {
        private MetaProfile profile;
        private MetaStore metaStore;
        private MetaDefinition metaContent;
        private MetaRules metaRules;
        private RunController validatedProfileCheckpoint;
        public MetaProfile Profile=>profile;
        private void InitializeMeta()
        {
            metaStore=new MetaStore(Path.Combine(Path.GetDirectoryName(SavePath),"meta-v1.json"));RunController candidate=null;profile=metaStore.Load(out string warning,json=>{candidate=RunController.FromJson(json);return true;});
            validatedProfileCheckpoint=profile.Active?.RunCheckpoint!=null?candidate:null;
            if(warning!=null){recoveryPending=true;recoveryDefault=metaStore.RecoveredDefault;}
            metaContent=Resources.Load<MetaDefinition>("CursedDeck/Meta");if(metaContent==null)throw new InvalidOperationException("Meta content missing.");metaRules=metaContent.Rules();metaRules.Validate();if(warning!=null)Debug.LogWarning(warning);
        }
        private void EnsureMetaSession(){if(profile.Active==null||profile.Active.Seed!=host.Controller.State.Seed)profile.Begin(host.Controller.State.Seed,"classic",null,DateTime.UtcNow);}
        private void ObserveMeta()
        {
            if(profile==null)return;EnsureMetaSession();var run=host.Controller;var round=run.CurrentRound;
            profile.Active.Observe(run.State.Floor,run.State.Round,round.State.Score,round.Chain,run.State.Stage==RunStage.Reward||run.State.Stage==RunStage.Shop||run.State.Stage==RunStage.Won);
            profile.Active.RunCheckpoint=run.ToJson();
            if(run.State.Stage==RunStage.Won)profile.Finish(true,DateTime.UtcNow,metaRules);
            metaStore.Save(profile);PublishMeta();
        }
        private void StartMetaRun(int seed,string deck,string date,bool replaceActive=false)
        {
            if(!replaceActive && host.Controller.State.Stage!=RunStage.Lost && host.Controller.State.Stage!=RunStage.Won && (host.Controller.State.Stage!=RunStage.Playing || host.Controller.State.Floor!=1 || host.Controller.State.Round!=1 || host.Controller.CurrentRound.History.Count>0)){host.Notice="Finish this run before starting another.";host.Publish();return;}
            if(!profile.IsUnlocked(deck)){host.Notice="Unlock this deck first.";host.Publish();return;}
            if(profile.Active!=null&&(host.Controller.State.Stage==RunStage.Lost||host.Controller.State.Stage==RunStage.Won)){ObserveMeta();profile.Finish(host.Controller.State.Stage==RunStage.Won,DateTime.UtcNow,metaRules);}
            var catalog=new List<RelicSpec>(content.CreateCatalog());var starting=new List<RelicSpec>();
            if(date==null)foreach(var asset in metaContent.UnlockRelics)if(profile.IsUnlocked(asset.CreateSpec().Id))catalog.Add(asset.CreateSpec());
            if(deck=="red-deck") {var bonus=new RelicSpec("red-deck-bonus",RelicKind.RedVelvet,0,multiplier:1.1m);catalog.Add(bonus);starting.Add(bonus);}
            if(!profile.IsUnlocked(deck)){host.Notice="Unlock this deck first.";host.Publish();return;}
            profile.Begin(seed,deck,date,DateTime.UtcNow);host.Notice=null;
            host.Replace(new RunController(seed,catalog,content.Rules.CreateConfig(),content.Layout.CreateLayout(),starting));
        }
        private void MetaCommand(GameCommand command)
        {
            if(command.Kind==GameCommandKind.ResetProgress){ResetRecoveredProgress();return;}
            if(command.Kind==GameCommandKind.MetaClassic)StartMetaRun(Environment.TickCount,"classic",null);
            else if(command.Kind==GameCommandKind.MetaRed)StartMetaRun(Environment.TickCount,"red-deck",null);
            else if(command.Kind==GameCommandKind.MetaDaily){var now=DateTime.UtcNow;StartMetaRun(DailyRun.Seed(now),"classic",DailyRun.DateKey(now));}
            else if(command.Kind==GameCommandKind.MetaUnlock){bool ok=profile.TryUnlock(command.Id,metaRules);metaStore.Save(profile);PublishMeta(ok?"Unlocked for future regular runs.":"Locked, owned or insufficient shards.");return;}
            else if(command.Kind==GameCommandKind.MetaShare){string result=profile.Active?.DailyDate!=null ? DailyRun.Share(profile.Active.DailyDate,profile.Active.Seed,profile.Active.Score,profile.Active.WonRounds,host.Controller.State.Stage==RunStage.Won) : profile.LastShare;if(!string.IsNullOrEmpty(result)){GUIUtility.systemCopyBuffer=result;PublishMeta("Daily result copied to clipboard.");}else PublishMeta("Play a daily run to create a result.");return;}
            PublishMeta();
        }
        private RelicPresentation[] MetaPresentations()
        {
            var rows=new List<RelicPresentation>(content.CreatePresentation());foreach(var asset in metaContent.UnlockRelics)rows.Add(new RelicPresentation(asset.CreateSpec().Id,asset.DisplayName,asset.Description));return rows.ToArray();
        }
        private void PublishMeta(string notice=null)
        {
            if(profile==null||bus==null)return;var labels=new string[MetaUnlocks.Ids.Length];for(int i=0;i<labels.Length;i++)labels[i]=MetaUnlocks.Ids[i].Replace('-',' ').ToUpperInvariant()+"  "+(profile.IsUnlocked(MetaUnlocks.Ids[i])?"OWNED":(i==0?metaRules.DeckCost:metaRules.RelicCost)+" SHARDS");
            bus.PublishMeta(new MetaPresentation{Summary="SOUL SHARDS  "+profile.SoulShards+"\nBEST  "+profile.BestScore.ToString("0.##",CultureInfo.InvariantCulture)+"   WINS  "+profile.Wins+"\nFASTEST  "+profile.FastestRunSeconds+"s   CHAIN  "+profile.LongestChain,
                Daily="DAILY  "+DailyRun.DateKey(DateTime.UtcNow)+"   STREAK  "+profile.DailyStreak,Share=profile.LastShare,RedUnlocked=profile.IsUnlocked("red-deck"),UnlockIds=MetaUnlocks.Ids,UnlockLabels=labels,Notice=notice});
        }
    }
}
