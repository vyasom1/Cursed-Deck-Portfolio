using System;
using System.IO;
using NUnit.Framework;
using CursedDeck.Meta;
using CursedDeck.Run;
using CursedDeck.Data;
using UnityEngine;
namespace CursedDeck.MetaTests
{
    public sealed class AtomicSessionTests
    {
        [Test] public void ProfileTransactionRestoresMatchingRunSessionAndExactUndoHistory()
        {
            var utc=new DateTime(2026,10,22,0,0,0,DateTimeKind.Utc);var profile=new MetaProfile();var run=new RunController(42,Resources.Load<GameContent>("CursedDeck/Content").CreateCatalog());profile.Begin(42,"classic",null,utc);run.Execute(new RunAction(RunActionKind.Play,run.Deal.SolutionSlots[0]));profile.Active.Observe(1,1,run.CurrentRound.State.Score,run.CurrentRound.Chain,false);profile.Active.RunCheckpoint=run.ToJson();string session=profile.Active.Id;
            string path=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"),"meta.json");try{var store=new MetaStore(path);store.Save(profile);var restored=store.Load(out _);var replay=RunController.FromJson(restored.Active.RunCheckpoint);Assert.That(restored.Active.Id,Is.EqualTo(session));Assert.That(restored.Active.Score,Is.EqualTo(replay.CurrentRound.State.Score));Assert.That(replay.ToJson(),Is.EqualTo(run.ToJson()));Assert.That(replay.CurrentRound.History.Count,Is.EqualTo(1));}finally{if(Directory.Exists(Path.GetDirectoryName(path)))Directory.Delete(Path.GetDirectoryName(path),true);}
        }
        [Test] public void IncompleteRunCannotIncrementWinsOrGrantWinBonus(){var p=new MetaProfile();var utc=DateTime.UtcNow;p.Begin(1,"classic",null,utc);Assert.Throws<InvalidOperationException>(()=>p.Finish(true,utc,new MetaRules()));Assert.That(p.Wins,Is.EqualTo(0));Assert.That(p.SoulShards,Is.EqualTo(0));}
    }
}