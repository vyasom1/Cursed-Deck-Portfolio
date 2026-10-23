using System;

namespace CursedDeck.Core
{
    public enum RoundPhase { Start, Play, Draw, Score, End }
    public enum RoundOutcome { Pending, Won, Lost }

    public sealed class RoundState
    {
        public decimal TargetScore { get; }
        public int InitialDrawLimit { get; }
        public decimal Score { get; internal set; }
        public int DrawsRemaining { get; internal set; }
        public CommandKind? LastCommand { get; internal set; }
        public int CardsPlayed { get; internal set; }
        public RoundPhase Phase { get; internal set; } = RoundPhase.Start;
        public RoundOutcome Outcome { get; internal set; } = RoundOutcome.Pending;

        public RoundState(decimal targetScore, int drawLimit)
        {
            if (targetScore <= 0) throw new ArgumentOutOfRangeException(nameof(targetScore));
            if (drawLimit < 0) throw new ArgumentOutOfRangeException(nameof(drawLimit));
            TargetScore = targetScore; InitialDrawLimit = drawLimit; DrawsRemaining = drawLimit;
        }
    }
}
