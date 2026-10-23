using System;
using CursedDeck.Core;

namespace CursedDeck.Run
{
    /// <summary>Presentation DTO deliberately exposes no Core types.</summary>
    public readonly struct RoundViewState
    {
        public decimal Score { get; }
        public int Draws { get; }
        public int Chain { get; }
        public int Gold { get; }
        public int CardsRemaining { get; }
        public bool Ended { get; }
        public bool Won { get; }
        internal RoundViewState(RoundSession round, RunState run)
        {
            Score = round.State.Score; Draws = round.State.DrawsRemaining; Chain = round.Chain;
            Gold = run.Gold; CardsRemaining = round.RemainingCards;
            Ended = round.State.Phase == RoundPhase.End; Won = round.State.Outcome == RoundOutcome.Won;
        }
    }

    public sealed partial class EventBus
    {
        public event Action<RoundViewState> RoundChanged;
        internal void Publish(RoundViewState state) => RoundChanged?.Invoke(state);
    }

    /// <summary>View-facing command facade. Only successful committed commands publish.
    /// The composition root supplies Core; presentation never calls it directly.</summary>
    public sealed class RoundApplication
    {
        private readonly RoundSession round;
        private readonly RunState run;
        private readonly EventBus events;
        public RoundViewState State => new RoundViewState(round, run);
        public RoundApplication(RoundSession round, RunState run, EventBus events)
        {
            this.round = round ?? throw new ArgumentNullException(nameof(round));
            this.run = run ?? throw new ArgumentNullException(nameof(run));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
        }
        private bool Publish(bool success) { if (success) events.Publish(State); return success; }
        public bool Start() => Publish(round.Start());
        public bool Play(int slot) => Publish(round.TryPlay(slot));
        public bool Draw() => Publish(round.TryDraw());
        public bool Reroll() => Publish(round.TryReroll());
        public bool Undo() => Publish(round.TryUndo());
    }
}
