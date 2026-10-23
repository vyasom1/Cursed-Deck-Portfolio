using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CursedDeck.Data;
namespace CursedDeck.Run.Tests
{
    public sealed class MockServiceTests
    {
        private RunController New(int draws=23)=>new RunController(12,Resources.Load<GameContent>("CursedDeck/Content").CreateCatalog(),new RunConfig(drawLimit:draws,baseTarget:1000000));
        [Test] public void FailedOrRepeatedAdNeverGrantsAndSaveReplayKeepsQuota(){var run=New();var ads=new MockRewardedAds{Succeed=false};var service=new RewardedActions(ads);string before=run.ToJson();Assert.That(service.TryComplete(run,false),Is.False);Assert.That(run.ToJson(),Is.EqualTo(before));ads.Succeed=true;Assert.That(service.TryComplete(run,false));Assert.That(run.CurrentRound.State.DrawsRemaining,Is.EqualTo(24));Assert.That(service.TryComplete(run,false),Is.False);var restored=RunController.FromJson(run.ToJson());Assert.That(restored.ToJson(),Is.EqualTo(run.ToJson()));Assert.That(restored.CanRewardDraw,Is.False);}
        [Test] public void UndoRestoresDrawStateButDoesNotFarmAnotherAd(){var run=New();run.State.Consumables.Add(ConsumableKind.Undo,1);run.DebugRestart(1,1);int before=run.CurrentRound.State.DrawsRemaining;Assert.That(new RewardedActions(new MockRewardedAds()).TryComplete(run,false));Assert.That(run.Execute(new RunAction(RunActionKind.Undo)));Assert.That(run.CurrentRound.State.DrawsRemaining,Is.EqualTo(before));Assert.That(run.CanRewardDraw,Is.False);Assert.That(RunController.FromJson(run.ToJson()).ToJson(),Is.EqualTo(run.ToJson()));}
        [Test] public void ReviveRequiresLossWithPhysicalStockAndFiresOnlyOnce(){RunController run=null;for(int seed=1;seed<=30;seed++){var r=new RunController(seed,Resources.Load<GameContent>("CursedDeck/Content").CreateCatalog(),new RunConfig(drawLimit:0,baseTarget:1000000));var rng=new Core.SeededRNG(seed);while(r.State.Stage==RunStage.Playing){int slot=BalanceSimulator.ChoosePlay(r,rng);Assert.That(slot,Is.GreaterThanOrEqualTo(0));r.Execute(new RunAction(RunActionKind.Play,slot));}if(r.State.Stage==RunStage.Lost){run=r;break;}}Assert.That(run,Is.Not.Null);var service=new RewardedActions(new MockRewardedAds());Assert.That(service.TryComplete(run,true));Assert.That(run.State.Stage,Is.EqualTo(RunStage.Playing));Assert.That(run.CurrentRound.State.DrawsRemaining,Is.EqualTo(1));Assert.That(service.TryComplete(run,true),Is.False);Assert.That(RunController.FromJson(run.ToJson()).ToJson(),Is.EqualTo(run.ToJson()));}
        [Test] public void MockPurchaseIsExplicitAndNeverAcceptsUnknownProduct(){IPurchases p=new MockPurchases();Assert.That(p.Purchase("unknown",out string receipt),Is.False);Assert.That(receipt,Is.Null);Assert.That(p.Purchase("support-demo",out receipt));Assert.That(receipt,Does.StartWith("MOCK-NO-CHARGE-"));}
        [Test] public void AnalyticsWritesOnlyLocalGameplayRows(){string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")+".csv");try{IAnalytics log=new LocalAnalytics(path);log.Track("checkpoint",42,1,2,123.5m);Assert.That(File.ReadAllText(path),Does.EndWith(",checkpoint,42,1,2,123.5\n"));Assert.Throws<ArgumentException>(()=>log.Track("email",1,1,1,0));}finally{File.Delete(path);}}
    }
}
