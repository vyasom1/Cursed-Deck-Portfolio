using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using CursedDeck.Core;

namespace CursedDeck.Run.Tests
{
    public sealed class RunProgressionTests
    {
        private static RelicSpec[] Catalog()
        {
            var result=new RelicSpec[8]; for(int i=0;i<8;i++) result[i]=RelicTests.Spec((RelicKind)i); return result;
        }
        internal static void Win(RunController run)
        {
            foreach(int slot in run.Deal.SolutionSlots)
            {
                if(run.State.Stage!=RunStage.Playing) break;
                Assert.That(run.Execute(new RunAction(RunActionKind.Play,slot)),Is.True);
            }
            Assert.That(run.State.Stage,Is.EqualTo(RunStage.Reward));
        }
        private static void Choose(RunController run)
        {
            Assert.That(run.ChooseReward(run.Reward.Options.Count==0 ? null : run.Reward.Options[0].Id),Is.True);
        }
        internal static RunController AtShop(int seed=42)
        {
            var run=new RunController(seed,Catalog());
            for(int i=0;i<3;i++) { Win(run); Choose(run); }
            Assert.That(run.State.Stage,Is.EqualTo(RunStage.Shop)); return run;
        }
        private static void Same(RunController run)
        {
            string json=run.ToJson(); var loaded=RunController.FromJson(json);
            Assert.That(loaded.ToJson(),Is.EqualTo(json));
            Assert.That(loaded.CurrentRound.Waste,Is.EqualTo(run.CurrentRound.Waste));
            Assert.That(loaded.CurrentRound.History.Count,Is.EqualTo(run.CurrentRound.History.Count));
            Assert.That(loaded.CurrentRound.State.Score,Is.EqualTo(run.CurrentRound.State.Score));
            Assert.That(loaded.CurrentRound.Chain,Is.EqualTo(run.CurrentRound.Chain));
            for(int i=0;i<28;i++)
            {
                Assert.That(loaded.CurrentRound.IsRemoved(i),Is.EqualTo(run.CurrentRound.IsRemoved(i)));
                Assert.That(loaded.IsPeeked(i),Is.EqualTo(run.IsPeeked(i)));
            }
        }

        [Test]
        public void FullHeadlessRunHasNineRoundsThreeRealBossesAndOnlyTwoShops()
        {
            var run=new RunController(42,Catalog()); int rounds=0,bosses=0,shops=0;
            for(int floor=1;floor<=3;floor++)
            {
                for(int round=1;round<=3;round++)
                {
                    Assert.That(run.State.Floor,Is.EqualTo(floor)); Assert.That(run.State.Round,Is.EqualTo(round));
                    Assert.That(run.IsBossRound,Is.EqualTo(round==3));
                    if(run.IsBossRound) bosses++;
                    Win(run); rounds++; Same(run); Choose(run);
                }
                if(floor<3) { shops++; Assert.That(run.State.Stage,Is.EqualTo(RunStage.Shop)); Same(run); Assert.That(run.LeaveShop(),Is.True); }
            }
            Assert.That(rounds,Is.EqualTo(9)); Assert.That(bosses,Is.EqualTo(3)); Assert.That(shops,Is.EqualTo(2));
            Assert.That(run.State.Stage,Is.EqualTo(RunStage.Won)); Same(run);
            Assert.That(run.ChooseReward(null),Is.False); Assert.That(run.LeaveShop(),Is.False);
        }

        [Test]
        public void GoldMathCapsRelicGrantedDrawsAndIsPaidOnce()
        {
            var config=new RunConfig(); Assert.That(config.GoldAward(23),Is.EqualTo(66));
            Assert.That(config.GoldAward(4),Is.EqualTo(28)); Assert.That(config.GoldAward(100),Is.EqualTo(66));
            Assert.That(config.GoldAward(-1),Is.EqualTo(20));
            var run=new RunController(42,Catalog()); Win(run);
            Assert.That(run.LastGoldAward,Is.EqualTo(66)); Assert.That(run.State.Gold,Is.EqualTo(66));
            Assert.That(run.Execute(new RunAction(RunActionKind.Draw)),Is.False); Assert.That(run.State.Gold,Is.EqualTo(66));
        }

        [Test]
        public void SaveLoadPreservesActiveActionsStockOrderUndoAndConsumables()
        {
            var run=AtShop();
            Assert.That(run.Buy("item-0"),Is.True); Assert.That(run.Buy("item-1"),Is.True); Assert.That(run.Buy("item-2"),Is.True);
            Assert.That(run.LeaveShop(),Is.True);
            int first=run.Deal.SolutionSlots[0]; Assert.That(run.Execute(new RunAction(RunActionKind.Play,first)),Is.True);
            Assert.That(run.Execute(new RunAction(RunActionKind.Peek)),Is.True);
            Assert.That(run.Execute(new RunAction(RunActionKind.Shuffle)),Is.True);
            var loaded=RunController.FromJson(run.ToJson()); Same(loaded);
            Assert.That(run.Execute(new RunAction(RunActionKind.Undo)),Is.True);
            Assert.That(loaded.Execute(new RunAction(RunActionKind.Undo)),Is.True);
            Assert.That(loaded.ToJson(),Is.EqualTo(run.ToJson()));
            Assert.That(run.State.Consumables[ConsumableKind.Undo],Is.Zero);
            Assert.That(run.CurrentRound.Chain,Is.EqualTo(1));
            Assert.That(run.IsPeeked(0),Is.True);
            Assert.That(run.Execute(new RunAction(RunActionKind.Draw)),Is.True);
            Assert.That(loaded.Execute(new RunAction(RunActionKind.Draw)),Is.True);
            Assert.That(loaded.CurrentRound.Waste,Is.EqualTo(run.CurrentRound.Waste)); Same(run);
        }

        [Test]
        public void ShopPricesAndOffersAreSeededAndPurchasesAreAtomic()
        {
            var a=AtShop(); var b=AtShop();
            Assert.That(a.Shop.Offers.Count,Is.InRange(3,6));
            for(int i=0;i<a.Shop.Offers.Count;i++)
            {
                Assert.That(a.Shop.Offers[i].Id,Is.EqualTo(b.Shop.Offers[i].Id));
                Assert.That(a.Shop.Offers[i].Price,Is.EqualTo(b.Shop.Offers[i].Price));
            }
            int gold=a.State.Gold; int price=a.Shop.Offers[0].Price;
            Assert.That(a.Buy("missing"),Is.False); Assert.That(a.State.Gold,Is.EqualTo(gold));
            Assert.That(a.Buy(a.Shop.Offers[0].Id),Is.True);
            Assert.That(a.State.Gold,Is.EqualTo(gold-price)); Assert.That(a.State.Consumables[ConsumableKind.Undo],Is.EqualTo(1));
            Assert.That(a.Buy(a.Shop.Offers[0].Id),Is.False); Assert.That(a.State.Gold,Is.EqualTo(gold-price));
            Same(a);
            var offer=a.Shop.Offers[a.Shop.Offers.Count-1];
            if(offer.Relic!=null)
            {
                Assert.That(a.Buy(offer.Id),Is.True); Assert.That(a.State.Owns(offer.Relic.Id),Is.True); Same(a);
            }
            a.State.TrySpendGold(a.State.Gold);
            Assert.That(a.Buy("item-1"),Is.False); Assert.That(a.State.Consumables[ConsumableKind.Peek],Is.Zero);
        }

        [Test]
        public void ShopIsUnavailableDuringRoundAndCannotRepeatAfterLeaving()
        {
            var run=new RunController(42,Catalog()); Assert.That(run.Buy("item-0"),Is.False); Assert.That(run.LeaveShop(),Is.False);
            run=AtShop(); var old=run.Shop; run.LeaveShop();
            Assert.That(old.TryBuy("item-0"),Is.False); Assert.That(run.State.Floor,Is.EqualTo(2)); Assert.That(run.State.Round,Is.EqualTo(1));
        }

        [Test]
        public void RejectedCommandsDoNotChangeSerializedStateOrConsumeItems()
        {
            var run=new RunController(42,Catalog()); string before=run.ToJson();
            Assert.That(run.Execute(new RunAction(RunActionKind.Play,-1)),Is.False);
            Assert.That(run.Execute(new RunAction(RunActionKind.Undo)),Is.False);
            Assert.That(run.Execute(new RunAction(RunActionKind.Peek)),Is.False);
            Assert.That(run.Execute(new RunAction(RunActionKind.Shuffle)),Is.False);
            Assert.That(run.ToJson(),Is.EqualTo(before));
        }

        [Test]
        public void LifecycleSavesOnCloseAndResumesWithBackupRecovery()
        {
            string folder=Path.Combine(Path.GetTempPath(),"CursedDeck-"+Guid.NewGuid().ToString("N"));
            string path=Path.Combine(folder,"run.json");
            try
            {
                var store=new RunSaveStore(path); var life=new RunLifecycle(store,42,Catalog());
                life.Controller.Execute(new RunAction(RunActionKind.Play,life.Controller.Deal.SolutionSlots[0])); life.Close();
                string first=life.Controller.ToJson();
                var resumed=new RunLifecycle(store,99,Catalog()); Assert.That(resumed.Controller.ToJson(),Is.EqualTo(first));
                resumed.Controller.Execute(new RunAction(RunActionKind.Play,resumed.Controller.Deal.SolutionSlots[1])); resumed.Close();
                File.WriteAllText(path,"broken");
                Assert.That(store.TryLoad(out RunController backup,out string warning),Is.True);
                Assert.That(backup.ToJson(),Is.EqualTo(first)); Assert.That(warning,Does.Contain("Recovered"));
                File.WriteAllText(path+".bak","broken too");
                Assert.That(store.TryLoad(out _,out warning),Is.False); Assert.That(File.ReadAllText(path),Is.EqualTo("broken"));
            }
            finally { if(Directory.Exists(folder)) Directory.Delete(folder,true); }
        }

        [Test]
        public void DamagedOrUnsupportedSaveIsRejected()
        {
            var run=new RunController(42,Catalog());
            Assert.Throws<SerializationException>(()=>RunController.FromJson(run.ToJson().Replace("\"Version\":1","\"Version\":9")));
            Assert.Throws<SerializationException>(()=>RunController.FromJson("{}"));
            Assert.Throws<SerializationException>(()=>RunController.FromJson(""));
        }
    }
}
