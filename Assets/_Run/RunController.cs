using System;
using System.Collections.Generic;
using CursedDeck.Core;

namespace CursedDeck.Run
{
    public enum RunActionKind { Play, Draw, Reroll, Undo, Peek, Shuffle, RewardDraw, Revive }
    public readonly struct RunAction
    {
        public RunActionKind Kind { get; }
        public int Value { get; }
        public RunAction(RunActionKind kind,int value=0) { Kind=kind; Value=value; }
    }

    public sealed partial class RunController
    {
        private readonly RelicSpec[] catalog;
        private readonly BoardLayout layout;
        private readonly List<RunAction> actions=new List<RunAction>(128);
        private readonly List<string> purchases=new List<string>(6);
        private readonly bool[] peeked;
        private RunSnapshot roundStart;
        private int roundSeed;
        private int shuffleCount;
        private bool adDrawUsed,adReviveUsed;
        private bool rewardResolved;
        private string rewardChoice;
        private RewardGenerator rewards;
        public RunState State { get; private set; }
        public RunConfig Config { get; }
        public RoundSession CurrentRound { get; private set; }
        public GeneratedBoard Deal { get; private set; }
        public RewardOffer Reward { get; private set; }
        public ShopManager Shop { get; private set; }
        public int LastGoldAward { get; private set; }
        public bool CanRewardDraw=>!adDrawUsed && State.Stage==RunStage.Playing && CurrentRound.StockRemaining>0;
        public bool CanRevive=>!adReviveUsed && State.Stage==RunStage.Lost && CurrentRound.StockRemaining>0;
        public bool IsBossRound => State.Round==3;
        public bool IsPeeked(int slot) => slot>=0 && slot<peeked.Length && peeked[slot];
        public IReadOnlyList<RelicSpec> Catalog { get; }

        public RunController(int seed,IReadOnlyList<RelicSpec> catalog,RunConfig config=null,BoardLayout layout=null,IReadOnlyList<RelicSpec> startingRelics=null)
        {
            if(catalog==null) throw new ArgumentNullException(nameof(catalog));
            this.catalog=new RelicSpec[catalog.Count]; var ids=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<catalog.Count;i++)
            {
                if(catalog[i]==null || !ids.Add(catalog[i].Id)) throw new ArgumentException("Invalid catalog.");
                this.catalog[i]=catalog[i];
            }
            Catalog=Array.AsReadOnly(this.catalog);
            Config=config ?? new RunConfig(); Config.Validate();
            this.layout=layout ?? BoardLayout.TriPeaks;peeked=new bool[this.layout.Count];
            State=new RunState(seed,Config.StartingGold);
            if(startingRelics!=null) for(int i=0;i<startingRelics.Count;i++) { if(!ids.Contains(startingRelics[i].Id)) throw new ArgumentException("Starting relic must be in catalog.");State.Acquire(startingRelics[i]); }
            BeginRound();
        }
        public static int BoardSeedFor(int seed,int floor,int round){if(floor<1||floor>3||round<1||round>3)throw new ArgumentOutOfRangeException();return Derive(seed,floor,round,0);}
        private static int Derive(int seed,int floor,int round,int stream) => unchecked(seed^(floor*73856093)^(round*19349663)^stream);
        private void BeginRound()
        {
            roundStart=RunSnapshot.From(State);
            roundSeed=Derive(State.Seed,State.Floor,State.Round,0);
            BuildRound();
        }
        private void BuildRound()
        {
            actions.Clear(); purchases.Clear(); Array.Clear(peeked,0,peeked.Length); shuffleCount=0;
            adDrawUsed=false; adReviveUsed=false; rewardResolved=false; rewardChoice=null; Reward=null; Shop=null; LastGoldAward=0;
            State.Stage=RunStage.Playing;
            Deal=WinnableBoardGenerator.Generate(roundSeed,layout,Config.DealerRulesVersion==1 && IsBossRound ? (DealerKind)State.Floor : DealerKind.None);
            CurrentRound=new RoundSession(Deal.CreateGame(),Config.Target(State.Floor,State.Round),Config.DrawLimit,
                effects:new RelicManager(State));
            rewards=new RewardGenerator(catalog,Derive(State.Seed,State.Floor,State.Round,1073741824));
            CurrentRound.Start(); Settle();
        }
        public bool Execute(RunAction action)
        {
            bool undo=action.Kind==RunActionKind.Undo;
            bool revive=action.Kind==RunActionKind.Revive;
            if(State.Stage!=RunStage.Playing && !(revive && State.Stage==RunStage.Lost) && !(undo && (State.Stage==RunStage.Reward || State.Stage==RunStage.Lost))) return false;
            bool success;
            switch(action.Kind)
            {
                case RunActionKind.RewardDraw:
                    if(!CanRewardDraw)return false;success=CurrentRound.TryRewardDraw(false);if(success)adDrawUsed=true;break;
                case RunActionKind.Revive:
                    if(!CanRevive)return false;success=CurrentRound.TryRewardDraw(true);if(success){adReviveUsed=true;State.Stage=RunStage.Playing;}break;
                case RunActionKind.Play: success=CurrentRound.TryPlay(action.Value); break;
                case RunActionKind.Draw: success=CurrentRound.TryDraw(); break;
                case RunActionKind.Reroll: success=CurrentRound.TryReroll(); break;
                case RunActionKind.Undo:
                    if(State.Consumables[ConsumableKind.Undo]==0 || CurrentRound.History.Count==0 || CurrentRound.IsSealed) return false;
                    success=CurrentRound.TryUndo();
                    if(success) { State.Consumables.TryConsume(ConsumableKind.Undo); State.Stage=RunStage.Playing; LastGoldAward=0; }
                    break;
                case RunActionKind.Peek:
                    if(State.Consumables[ConsumableKind.Peek]==0) return false;
                    int found=0;
                    for(int i=0;i<layout.Count && found<3;i++)
                        if(!peeked[i] && !CurrentRound.IsRemoved(i) && !CurrentRound.IsExposed(i)) { peeked[i]=true; found++; }
                    success=found>0;
                    if(success) State.Consumables.TryConsume(ConsumableKind.Peek);
                    break;
                case RunActionKind.Shuffle:
                    if(State.Consumables[ConsumableKind.ShuffleDrawPile]==0) return false;
                    success=CurrentRound.TryShuffleStock(new SeededRNG(Derive(roundSeed,1,++shuffleCount,536870912)));
                    if(success) State.Consumables.TryConsume(ConsumableKind.ShuffleDrawPile);
                    else shuffleCount--;
                    break;
                default: return false;
            }
            if(!success) return false;
            actions.Add(action); Settle(); return true;
        }
        private void Settle()
        {
            if(CurrentRound.State.Outcome==RoundOutcome.Pending) return;
            if(CurrentRound.State.Outcome==RoundOutcome.Lost) { State.Stage=RunStage.Lost; return; }
            if(State.Stage==RunStage.Reward || State.Stage==RunStage.Shop || State.Stage==RunStage.Won) return;
            LastGoldAward=Config.GoldAward(CurrentRound.State.DrawsRemaining);
            State.AddGold(LastGoldAward); State.Stage=RunStage.Reward;
            Reward=rewards.OfferAfterRound(State,CurrentRound,Config.FallbackRewardGold);
        }
        public bool ChooseReward(string id)
        {
            if(State.Stage!=RunStage.Reward || rewardResolved) return false;
            if(!Reward.TryChoose(id))return false;
            rewardResolved=true; rewardChoice=id;
            if(State.Round<3) { State.Round++; BeginRound(); }
            else if(State.Floor==3) State.Stage=RunStage.Won;
            else
            {
                State.Stage=RunStage.Shop;
                Shop=new ShopManager(State,catalog,Config,Derive(State.Seed,State.Floor,3,268435456));
            }
            return true;
        }
        public bool Buy(string id)
        {
            if(State.Stage!=RunStage.Shop || Shop==null || !Shop.TryBuy(id)) return false;
            purchases.Add(id); return true;
        }
        public bool LeaveShop()
        {
            if(State.Stage!=RunStage.Shop || State.Floor>=3) return false;
            State.Floor++; State.Round=1; BeginRound(); return true;
        }
    }
}
