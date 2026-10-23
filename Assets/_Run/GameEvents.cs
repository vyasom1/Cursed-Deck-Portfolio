using System;
using System.Collections.Generic;

namespace CursedDeck.Run
{
    public enum GameCommandKind { Play, Draw, Reroll, Undo, Peek, Shuffle, ChooseReward, Buy, Continue,
        NewRun, Refresh, ToggleSound, ToggleHaptics, ToggleLeftHanded, ToggleColorblind,
        DebugRegenerate, DebugGold, DebugRelic, DebugJump, MetaRefresh, MetaClassic, MetaRed, MetaDaily, MetaUnlock, MetaShare, TestAdDraw, TestAdRevive, MockPurchase, TutorialComplete,
        Pause, Resume, ReturnToTitle, MenuNewRun, ResetProgress, MenuDaily, StartTutorial, TutorialNext, ToggleReduceMotion }
    public enum MenuMode { Playing, Title, Paused }
    public readonly struct GameCommand
    {
        public GameCommandKind Kind { get; }
        public int Value { get; }
        public string Id { get; }
        public GameCommand(GameCommandKind kind,int value=0,string id=null) { Kind=kind; Value=value; Id=id; }
    }
    public sealed class MetaPresentation
    {
        public string Summary, Daily, Share, Notice;
        public bool RedUnlocked;
        public string[] UnlockIds, UnlockLabels;
    }
    public enum GameFeedback { None, Deal, Play, Draw, Undo, Peek, Shuffle, Reward, Shop, Settings }
    public readonly struct UiSettings
    {
        public bool Sound { get; }
        public bool Haptics { get; }
        public bool LeftHanded { get; }
        public bool Colorblind { get; }
        public bool ReduceMotion { get; }
        public UiSettings(bool sound=true,bool haptics=true,bool leftHanded=false,bool colorblind=false,bool reduceMotion=false)
        { Sound=sound; Haptics=haptics; LeftHanded=leftHanded; Colorblind=colorblind;ReduceMotion=reduceMotion; }
    }
    public readonly struct CardUiState
    {
        public int Slot { get; }
        public int X { get; }
        public int Y { get; }
        public int Rank { get; }
        public int Suit { get; }
        public bool Visible { get; }
        public bool Exposed { get; }
        public bool Playable { get; }
        public bool Removed { get; }
        public CardUiState(int slot,int x,int y,int rank,int suit,bool visible,bool exposed,bool playable,bool removed)
        { Slot=slot; X=x; Y=y; Rank=rank; Suit=suit; Visible=visible; Exposed=exposed; Playable=playable; Removed=removed; }
    }
    public sealed class RelicPresentation
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public RelicPresentation(string id,string name,string description) { Id=id; Name=name; Description=description; }
    }
    public sealed class OfferUiState
    {
        public string Id { get; }
        public string Title { get; }
        public string Detail { get; }
        public int Price { get; }
        public bool Available { get; }
        public OfferUiState(string id,string title,string detail,int price,bool available)
        { Id=id; Title=title; Detail=detail; Price=price; Available=available; }
    }
    public sealed class GameSnapshot
    {
        public MenuMode Menu { get; internal set; }
        public bool CanContinueRun { get; internal set; }
        public int Seed { get; internal set; }
        public int BoardSeed { get; internal set; }
        public int Floor { get; internal set; }
        public int Round { get; internal set; }
        public RunStage Stage { get; internal set; }
        public bool Boss { get; internal set; }
        public string DealerName { get; internal set; }
        public string DealerRule { get; internal set; }
        public decimal Score { get; internal set; }
        public decimal Target { get; internal set; }
        public int Draws { get; internal set; }
        public int Chain { get; internal set; }
        public int Gold { get; internal set; }
        public int Award { get; internal set; }
        public int Stock { get; internal set; }
        public int WasteRank { get; internal set; }
        public int WasteSuit { get; internal set; }
        public int UndoCount { get; internal set; }
        public int PeekCount { get; internal set; }
        public int ShuffleCount { get; internal set; }
        public bool CanUndo { get; internal set; }
        public bool CanDraw { get; internal set; }
        public bool CanReroll { get; internal set; }
        public bool CanRewardDraw {get;internal set;}
        public bool CanRevive {get;internal set;}
        public int RelicCount { get; internal set; }
        public IReadOnlyList<RelicPresentation> OwnedRelics {get;internal set;}
        public int FallbackRewardGold {get;internal set;}
        public IReadOnlyList<CardUiState> Cards { get; internal set; }
        public IReadOnlyList<OfferUiState> Offers { get; internal set; }
        public UiSettings Settings { get; internal set; }
        public GameFeedback Feedback { get; internal set; }
        public int PlayedSlot { get; internal set; }=-1;
        public bool TutorialActive {get;internal set;}
        public TutorialStage TutorialStage {get;internal set;}
        public int TutorialSlot {get;internal set;}=-1;
        public bool HasNoMoves {get;internal set;}
        public int GuideSlot {get;internal set;}=-1;
        public string Notice { get; internal set; }
    }
    public sealed partial class EventBus
    {
        public event Action<MetaPresentation> MetaChanged;
        public void PublishMeta(MetaPresentation value)=>MetaChanged?.Invoke(value);
        public event Action<GameCommand> CommandRequested;
        public event Action<GameSnapshot> GameChanged;
        public void Send(GameCommand command) => CommandRequested?.Invoke(command);
        internal void Publish(GameSnapshot snapshot) => GameChanged?.Invoke(snapshot);
    }
}
