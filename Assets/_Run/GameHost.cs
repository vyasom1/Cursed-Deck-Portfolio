using System;
using System.Collections.Generic;

namespace CursedDeck.Run
{
    /// <summary>Application/event boundary. View receives DTOs, never Core objects.</summary>
    public sealed class GameHost : IDisposable
    {
        private readonly EventBus bus;
        private readonly Dictionary<string,RelicPresentation> text=new Dictionary<string,RelicPresentation>(StringComparer.Ordinal);
        private bool busy;
        private UiSettings settings;
        public RunController Controller { get; private set; }
        public event Action<GameCommand> ServiceRequested;
        public event Action<GameCommand> MetaRequested;
        public event Action SaveRequested;
        public event Action<int> NewRunRequested;
        public event Action MenuNewRunRequested;
        public event Action MenuDailyRequested;
        public event Action TutorialRequested;
        private GuidedTutorial tutorial;
        private RunController suspendedRun;private bool suspendedEntered;
        public bool IsTutorial=>tutorial!=null;
        public void BeginTutorial(RunController practice)
        {
            if(practice==null)throw new ArgumentNullException(nameof(practice));
            if(!IsTutorial)
            {
                if(HasEnteredRun)SaveRequested?.Invoke();
                suspendedRun=Controller;suspendedEntered=CanContinueRun;
            }
            Controller=practice;
            tutorial=new GuidedTutorial(FirstMatchGuide.For(practice.Deal.Layout).Slot);HasEnteredRun=true;Menu=MenuMode.Playing;Publish(GameFeedback.Deal);
        }
        private void RestorePracticeRun()
        { Controller=suspendedRun;suspendedRun=null;HasEnteredRun=suspendedEntered;tutorial=null; }
        public event Action<GameCommandKind> SettingsRequested;
        public string Notice { get; set; }
        public MenuMode Menu { get; private set; }
        public bool HasEnteredRun { get; private set; }=true;
        public bool CanContinueRun => HasEnteredRun && Controller.State.Stage!=RunStage.Won && Controller.State.Stage!=RunStage.Lost;
        // Only composition calls this at launch, after validating/recovering the checkpoint.
        public void ShowStartMenu(bool hasCheckpoint)
        { HasEnteredRun=hasCheckpoint; Menu=MenuMode.Title; Publish(); }
        public GameHost(RunController controller,EventBus bus,IReadOnlyList<RelicPresentation> presentations,UiSettings settings)
        {
            Controller=controller ?? throw new ArgumentNullException(nameof(controller));
            this.bus=bus ?? throw new ArgumentNullException(nameof(bus)); this.settings=settings;
            if(presentations!=null) for(int i=0;i<presentations.Count;i++) text[presentations[i].Id]=presentations[i];
            bus.CommandRequested+=Handle;
        }
        public void Dispose() { bus.CommandRequested-=Handle; }
        public void Replace(RunController controller)
        {
            Controller=controller ?? throw new ArgumentNullException(nameof(controller));
            SaveRequested?.Invoke(); Publish(GameFeedback.Deal);
        }
        public void UpdateSettings(UiSettings value) { settings=value; Publish(GameFeedback.Settings); }
        private void Handle(GameCommand command)
        {
            if(busy) return;
            if(tutorial!=null&&!tutorial.Allows(command))return;
            busy=true;
            try
            {
                switch(command.Kind)
                {
                    case GameCommandKind.StartTutorial: TutorialRequested?.Invoke();return;
                    case GameCommandKind.TutorialNext:
                        if(Menu==MenuMode.Playing&&tutorial!=null){tutorial.Advance();Publish();}return;
                    case GameCommandKind.TutorialComplete:
                        if(Menu!=MenuMode.Playing||tutorial==null||tutorial.Stage!=TutorialStage.Complete)return;
                        bool restore=suspendedEntered;RestorePracticeRun();Menu=MenuMode.Playing;HasEnteredRun=true;
                        ServiceRequested?.Invoke(new GameCommand(GameCommandKind.TutorialComplete,restore?1:0));
                        SaveRequested?.Invoke();Publish(GameFeedback.Deal);return;
                    case GameCommandKind.Pause:
                        if(Menu==MenuMode.Playing){Menu=MenuMode.Paused;SaveRequested?.Invoke();Publish();}return;
                    case GameCommandKind.ReturnToTitle:
                        if(IsTutorial)RestorePracticeRun();
                        if(HasEnteredRun)SaveRequested?.Invoke();Menu=MenuMode.Title;Publish();return;
                    case GameCommandKind.Resume:
                        if(Menu==MenuMode.Paused || (Menu==MenuMode.Title && CanContinueRun)){Menu=MenuMode.Playing;Publish();}return;
                    case GameCommandKind.MenuDaily:
                        if(Menu!=MenuMode.Title || MenuDailyRequested==null)return;
                        MenuDailyRequested.Invoke();HasEnteredRun=true;Menu=MenuMode.Playing;SaveRequested?.Invoke();Publish();return;
                    case GameCommandKind.MenuNewRun:
                        if(Menu!=MenuMode.Title || MenuNewRunRequested==null)return;
                        MenuNewRunRequested.Invoke();HasEnteredRun=true;Menu=MenuMode.Playing;SaveRequested?.Invoke();Publish();return;
                }
                // A raycast shield is also present, but queued/keyboard actions must not bypass pause.
                if(Menu!=MenuMode.Playing && !AllowedInMenu(command.Kind))return;
                bool success=false; GameFeedback feedback=GameFeedback.None;
                switch(command.Kind)
                {
                    case GameCommandKind.ResetProgress: case GameCommandKind.MetaRefresh: case GameCommandKind.MetaClassic: case GameCommandKind.MetaRed: case GameCommandKind.MetaDaily: case GameCommandKind.MetaUnlock: case GameCommandKind.MetaShare:
                        MetaRequested?.Invoke(command); return;
                    case GameCommandKind.TestAdDraw: case GameCommandKind.TestAdRevive: case GameCommandKind.MockPurchase:
                        ServiceRequested?.Invoke(command);return;
                    case GameCommandKind.Refresh: Publish(GameFeedback.None); return;
                    case GameCommandKind.NewRun: NewRunRequested?.Invoke(command.Value); return;
                    case GameCommandKind.ToggleSound: case GameCommandKind.ToggleHaptics:
                    case GameCommandKind.ToggleLeftHanded: case GameCommandKind.ToggleColorblind: case GameCommandKind.ToggleReduceMotion:
                        SettingsRequested?.Invoke(command.Kind); return;
                    case GameCommandKind.Play: success=Controller.Execute(new RunAction(RunActionKind.Play,command.Value)); feedback=GameFeedback.Play; break;
                    case GameCommandKind.Draw: success=Controller.Execute(new RunAction(RunActionKind.Draw)); feedback=GameFeedback.Draw; break;
                    case GameCommandKind.Reroll: success=Controller.Execute(new RunAction(RunActionKind.Reroll)); feedback=GameFeedback.Draw; break;
                    case GameCommandKind.Undo: success=Controller.Execute(new RunAction(RunActionKind.Undo)); feedback=GameFeedback.Undo; break;
                    case GameCommandKind.Peek: success=Controller.Execute(new RunAction(RunActionKind.Peek)); feedback=GameFeedback.Peek; break;
                    case GameCommandKind.Shuffle: success=Controller.Execute(new RunAction(RunActionKind.Shuffle)); feedback=GameFeedback.Shuffle; break;
                    case GameCommandKind.ChooseReward: success=Controller.ChooseReward(command.Id); feedback=GameFeedback.Reward; break;
                    case GameCommandKind.Buy: success=Controller.Buy(command.Id); feedback=GameFeedback.Shop; break;
                    case GameCommandKind.Continue:
                        success=Controller.State.Stage==RunStage.Shop ? Controller.LeaveShop() : Controller.ChooseReward(null);
                        feedback=GameFeedback.Deal; break;
#if UNITY_EDITOR || CURSED_DECK_DEBUG
                    case GameCommandKind.DebugRegenerate:
                        Controller.DebugRestart(Controller.State.Floor,Controller.State.Round,command.Value==0 ? unchecked(Controller.Deal.Seed+1) : command.Value); success=true; feedback=GameFeedback.Deal; break;
                    case GameCommandKind.DebugGold:
                        Controller.DebugRestart(Controller.State.Floor,Controller.State.Round,Controller.Deal.Seed,100); success=true; feedback=GameFeedback.Deal; break;
                    case GameCommandKind.DebugRelic:
                        Controller.DebugRestart(Controller.State.Floor,Controller.State.Round,Controller.Deal.Seed,0,true); success=true; feedback=GameFeedback.Deal; break;
                    case GameCommandKind.DebugJump:
                        if(command.Value>=0 && command.Value<9) { Controller.DebugRestart(command.Value/3+1,command.Value%3+1); success=true; feedback=GameFeedback.Deal; } break;
#endif
                }
                if(success) { tutorial?.Accepted(command);SaveRequested?.Invoke(); Publish(feedback,command.Kind==GameCommandKind.Play ? command.Value : -1); }
            }
            finally { busy=false; }
        }
        private static bool AllowedInMenu(GameCommandKind kind)
        {
            return kind==GameCommandKind.Refresh || kind==GameCommandKind.ResetProgress || kind==GameCommandKind.MetaRefresh || kind==GameCommandKind.MetaShare || kind==GameCommandKind.MetaUnlock ||
                kind==GameCommandKind.MockPurchase || kind==GameCommandKind.ToggleSound || kind==GameCommandKind.ToggleHaptics ||
                kind==GameCommandKind.ToggleLeftHanded || kind==GameCommandKind.ToggleColorblind || kind==GameCommandKind.ToggleReduceMotion;
        }
        private RelicPresentation Presentation(RelicSpec spec) => text.TryGetValue(spec.Id,out var value) ? value : new RelicPresentation(spec.Id,spec.Kind.ToString(),"A relic from the haunted casino.");
        public GameSnapshot Snapshot(GameFeedback feedback=GameFeedback.None,int playedSlot=-1)
        {
            var c=Controller; var round=c.CurrentRound; var cards=new CardUiState[round.Layout.Count];
            for(int i=0;i<cards.Length;i++)
            {
                bool removed=round.IsRemoved(i), exposed=round.IsExposed(i), visible=!removed && (exposed || c.IsPeeked(i)) && !round.IsBlind(i);
                var card=round.GetCard(i);
                cards[i]=new CardUiState(i,round.Layout[i].X,round.Layout[i].Y,visible ? (int)card.Rank : 0,
                    visible ? (int)card.Suit : 0,visible,exposed,c.State.Stage==RunStage.Playing && (round.IsBlind(i) || round.CanPlay(i)) &&
                    (tutorial==null || (tutorial.Stage==TutorialStage.Match && i==tutorial.Slot)),removed);
            }
            var owned=new List<RelicPresentation>();foreach(var relic in c.State.Relics)if(relic.Id!="red-deck-bonus")owned.Add(Presentation(relic));
            var offers=new List<OfferUiState>(6);
            if(c.State.Stage==RunStage.Reward && c.Reward!=null)
                foreach(var relic in c.Reward.Options)
                {
                    var p=Presentation(relic); offers.Add(new OfferUiState(relic.Id,p.Name,p.Description,0,true));
                }
            if(c.State.Stage==RunStage.Shop && c.Shop!=null)
                foreach(var offer in c.Shop.Offers)
                {
                    string title,description;
                    if(offer.Relic!=null) { var p=Presentation(offer.Relic); title=p.Name; description=p.Description; }
                    else
                    {
                        title=offer.Consumable==ConsumableKind.Undo ? "Undo" : offer.Consumable==ConsumableKind.Peek ? "Peek" : "Shuffle Draw Pile";
                        description=offer.Consumable==ConsumableKind.Undo ? "Reverse one move." : offer.Consumable==ConsumableKind.Peek ? "Reveal three covered cards." : "Reorder the remaining stock.";
                    }
                    offers.Add(new OfferUiState(offer.Id,title,description,offer.Price,!offer.Purchased && c.State.Gold>=offer.Price));
                }
            return new GameSnapshot {TutorialActive=IsTutorial,TutorialStage=tutorial?.Stage??TutorialStage.Welcome,TutorialSlot=tutorial?.Slot??-1,HasNoMoves=round.HasNoMoves,GuideSlot=c.State.Seed==FirstMatchGuide.For(round.Layout).Seed&&c.State.Floor==1&&c.State.Round==1?FirstMatchGuide.For(round.Layout).Slot:-1,Menu=Menu,CanContinueRun=CanContinueRun,Seed=c.State.Seed,BoardSeed=c.Deal.Seed,Floor=c.State.Floor,Round=c.State.Round,Stage=c.State.Stage,
                DealerName=round.Dealer==Core.DealerKind.Apprentice?"THE APPRENTICE":round.Dealer==Core.DealerKind.Clockmaker?"THE CLOCKMAKER":round.Dealer==Core.DealerKind.Mirror?"THE MIRROR":"",
                DealerRule=round.Dealer==Core.DealerKind.Apprentice?"J/Q/K LOCKED UNTIL 3 NUMBERED PLAYS":round.Dealer==Core.DealerKind.Clockmaker?"-3 START DRAWS / CHAIN 5+ REFUNDS 1":round.Dealer==Core.DealerKind.Mirror?"EVERY FOURTH PLAY: A BLIND CARD":"",Boss=c.IsBossRound,Score=round.State.Score,Target=round.State.TargetScore,Draws=round.State.DrawsRemaining,
                Chain=round.Chain,Gold=c.State.Gold,Award=c.LastGoldAward,Stock=round.StockRemaining,WasteRank=(int)round.Waste.Rank,WasteSuit=(int)round.Waste.Suit,
                UndoCount=c.State.Consumables[ConsumableKind.Undo],PeekCount=c.State.Consumables[ConsumableKind.Peek],ShuffleCount=c.State.Consumables[ConsumableKind.ShuffleDrawPile],
                CanUndo=c.State.Consumables[ConsumableKind.Undo]>0 && round.History.Count>0 && !round.IsSealed,
                CanRewardDraw=c.CanRewardDraw,CanRevive=c.CanRevive,CanDraw=c.State.Stage==RunStage.Playing && round.CanDraw && (tutorial==null || tutorial.Stage==TutorialStage.Draw),CanReroll=c.State.Stage==RunStage.Playing && round.CanReroll,
                OwnedRelics=owned.AsReadOnly(),FallbackRewardGold=c.State.Stage==RunStage.Reward&&c.Reward!=null?c.Reward.FallbackGold:0,RelicCount=c.State.Relics.Count-(c.State.Owns("red-deck-bonus")?1:0),Cards=Array.AsReadOnly(cards),Offers=offers.AsReadOnly(),Settings=settings,Feedback=feedback,PlayedSlot=playedSlot,Notice=Notice};
        }
        public void Publish(GameFeedback feedback=GameFeedback.None,int playedSlot=-1) => bus.Publish(Snapshot(feedback,playedSlot));
    }
}
