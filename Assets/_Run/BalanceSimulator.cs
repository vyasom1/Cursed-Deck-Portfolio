using System;
using System.Collections.Generic;
using System.Globalization;
namespace CursedDeck.Run
{
    public sealed class SimulationResult
    {
        public int Seed,Floor,Steps,Draws,Chain,BlindMisses;public decimal Score;public bool Won;public string Pick,Dealer;public string[] Offers;
        public string Csv(string profile)=>Seed+","+Floor+","+Dealer+","+profile+","+(Won?1:0)+","+Score.ToString("0.##",CultureInfo.InvariantCulture)+","+Steps+","+Draws+","+Chain+","+(Pick??"");
    }
    public sealed class FullRunSample
    {
        public int Seed,Steps,BlindMisses;public bool Won;public decimal Score;
        public readonly bool[] Reached=new bool[9],RoundWins=new bool[9];
        public readonly decimal[] RoundScores=new decimal[9];public readonly int[] RoundDraws=new int[9],RoundChains=new int[9];
        public readonly List<string> Offered=new List<string>(),Picked=new List<string>();public string[] Owned;
    }
    /// <summary>Seeded player-visible greedy bot. Blind guesses use accepted/rejected feedback;
    /// they never read hidden ranks or the generator witness. Misses cost nothing under current rules.</summary>
    public static class BalanceSimulator
    {
        public static int ChoosePlay(RunController run,Core.SeededRNG rng)=>ChoosePlay(run,rng,null);
        private static int ChoosePlay(RunController run,Core.SeededRNG rng,bool[] rejected)
        {
            var round=run.CurrentRound;int blind=-1,blindCount=0;bool hasBlind=false;
            for(int i=0;i<round.Layout.Count;i++)if(round.IsBlind(i)){hasBlind=true;if(rejected==null||!rejected[i])if(rng.NextInt(++blindCount)==0)blind=i;}
            if(hasBlind)return blind;
            int chosen=-1,best=int.MinValue,ties=0;
            for(int slot=0;slot<round.Layout.Count;slot++)
            {
                if(!round.CanPlay(slot))continue;int value=0;
                for(int other=0;other<round.Layout.Count;other++)
                {
                    if(other==slot||round.IsRemoved(other))continue;
                    if(round.IsExposed(other)&&!round.IsBlind(other))
                    {int diff=Math.Abs((int)round.GetCard(slot).Rank-(int)round.GetCard(other).Rank);if(diff==1||diff==12)value+=5;}
                    else
                    {bool frees=true,coveredBySlot=false;foreach(int cover in round.Layout[other].CoveringSlots){if(cover==slot)coveredBySlot=true;else if(!round.IsRemoved(cover))frees=false;}if(frees&&coveredBySlot)value+=10;}
                }
                if(value>best){best=value;chosen=slot;ties=1;}else if(value==best&&rng.NextInt(++ties)==0)chosen=slot;
            }
            return chosen;
        }
        private static void Act(RunController run,Core.SeededRNG rng,bool[] rejected,ref int misses)
        {
            int slot=ChoosePlay(run,rng,rejected);bool accepted;
            if(slot>=0)
            {
                bool blind=run.CurrentRound.IsBlind(slot);accepted=run.Execute(new RunAction(RunActionKind.Play,slot));
                if(!accepted&&blind){rejected[slot]=true;misses++;return;}
            }
            else accepted=run.CurrentRound.CanDraw?run.Execute(new RunAction(RunActionKind.Draw)):run.CurrentRound.CanReroll&&run.Execute(new RunAction(RunActionKind.Reroll));
            if(!accepted)throw new InvalidOperationException("Pending round has no accepted action.");Array.Clear(rejected,0,rejected.Length);
        }
        private static int Preference(RelicKind kind)
        {
            switch(kind){case RelicKind.LoadedDie:return 10;case RelicKind.RedVelvet:return 9;case RelicKind.SuitBridge:case RelicKind.GraveCompass:return 8;case RelicKind.ColorEcho:case RelicKind.VoidStep:return 7;case RelicKind.LastBreath:return 6;case RelicKind.Metronome:return 5;case RelicKind.CrackedMirror:case RelicKind.StillHour:case RelicKind.FrostPocket:return 4;case RelicKind.BoneAbacus:case RelicKind.BoneCrown:return 3;case RelicKind.SoulLedger:return 2;default:return 1;}
        }
        private static RelicSpec Pick(IReadOnlyList<RelicSpec> offers,Core.SeededRNG rng)
        {RelicSpec chosen=null;int best=int.MinValue,ties=0;foreach(var relic in offers){int rank=Preference(relic.Kind);if(rank>best){chosen=relic;best=rank;ties=1;}else if(rank==best&&rng.NextInt(++ties)==0)chosen=relic;}return chosen;}
        public static SimulationResult Evaluate(int seed,int floor,IReadOnlyList<RelicSpec> catalog,IReadOnlyList<RelicSpec> starting=null)
        {
            if(floor<1||floor>3)throw new ArgumentOutOfRangeException(nameof(floor));
            var run=new RunController(seed,catalog,new RunConfig(startingGold:100),startingRelics:starting);run.DebugRestart(floor,3);
            var rng=new Core.SeededRNG(unchecked(seed^floor*104729));var rejected=new bool[run.Deal.Layout.Count];var result=new SimulationResult{Seed=seed,Floor=floor,Dealer=run.CurrentRound.Dealer.ToString()};
            while(run.State.Stage==RunStage.Playing){if(++result.Steps>256)throw new InvalidOperationException("Simulator did not terminate.");Act(run,rng,rejected,ref result.BlindMisses);result.Chain=Math.Max(result.Chain,run.CurrentRound.Chain);}
            result.Score=run.CurrentRound.State.Score;result.Won=run.State.Stage==RunStage.Reward;result.Draws=run.CurrentRound.State.InitialDrawLimit-run.CurrentRound.State.DrawsRemaining;
            var offered=new List<string>();if(result.Won)foreach(var relic in run.Reward.Options)offered.Add(relic.Id);result.Offers=offered.ToArray();result.Pick=result.Won?Pick(run.Reward.Options,rng)?.Id:null;
            if(result.Won&&!run.ChooseReward(result.Pick))throw new InvalidOperationException("Reward rejected.");return result;
        }
        public static FullRunSample RunFull(int seed,IReadOnlyList<RelicSpec> catalog)
        {
            var run=new RunController(seed,catalog);var rng=new Core.SeededRNG(unchecked(seed^73428767));var rejected=new bool[run.Deal.Layout.Count];var result=new FullRunSample{Seed=seed};
            for(int safety=0;safety<4096;safety++)
            {
                if(run.State.Stage==RunStage.Playing)
                {result.Steps++;Act(run,rng,rejected,ref result.BlindMisses);int index=(run.State.Floor-1)*3+run.State.Round-1;result.RoundChains[index]=Math.Max(result.RoundChains[index],run.CurrentRound.Chain);continue;}
                if(run.State.Stage==RunStage.Reward)
                {
                    Record(result,run,true);foreach(var relic in run.Reward.Options)result.Offered.Add(relic.Id);var pick=Pick(run.Reward.Options,rng);if(pick!=null)result.Picked.Add(pick.Id);
                    if(!run.ChooseReward(pick?.Id))throw new InvalidOperationException("Full-run reward rejected.");Array.Clear(rejected,0,rejected.Length);continue;
                }
                if(run.State.Stage==RunStage.Shop)
                {
                    // Natural earned gold only; buy affordable relics by the same deterministic preference.
                    while(true)
                    {
                        ShopOffer best=null;int weight=int.MinValue;foreach(var offer in run.Shop.Offers)if(!offer.Purchased&&offer.Relic!=null&&offer.Price<=run.State.Gold&&Preference(offer.Relic.Kind)>weight){best=offer;weight=Preference(offer.Relic.Kind);}
                        if(best==null)break;if(!run.Buy(best.Id))throw new InvalidOperationException("Full-run shop rejected.");
                    }
                    if(!run.LeaveShop())throw new InvalidOperationException("Full-run shop exit rejected.");continue;
                }
                result.Won=run.State.Stage==RunStage.Won;if(!result.Won)Record(result,run,false);
                result.Owned=new string[run.State.Relics.Count];for(int i=0;i<result.Owned.Length;i++)result.Owned[i]=run.State.Relics[i].Id;Array.Sort(result.Owned,StringComparer.Ordinal);return result;
            }
            throw new InvalidOperationException("Full-run simulator exceeded its bounded decision budget.");
        }
        private static void Record(FullRunSample result,RunController run,bool won)
        {
            int index=(run.State.Floor-1)*3+run.State.Round-1;if(result.Reached[index])return;result.Reached[index]=true;result.RoundWins[index]=won;result.RoundScores[index]=run.CurrentRound.State.Score;result.Score+=run.CurrentRound.State.Score;result.RoundDraws[index]=run.CurrentRound.State.InitialDrawLimit-run.CurrentRound.State.DrawsRemaining;
        }
    }
}
