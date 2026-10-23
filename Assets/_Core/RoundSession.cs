using System;

namespace CursedDeck.Core
{
    /// <summary>Command-authoritative round. Hooks execute before outcome resolution;
    /// notifications cannot reenter actions. Applications publish committed state afterwards.</summary>
    public sealed class RoundSession
    {
        private readonly RuleEngine game;
        private readonly ScoringPipeline scoring;
        private readonly IScoreHook scoreHook;
        private readonly IRoundEffects effects;
        private bool executing;
        public bool IsSealed { get; private set; }
        public bool CanSeal => !executing && EffectsActive && State.Phase == RoundPhase.End && !IsSealed;
        private bool EffectsActive => effects == null || effects.IsActive;
        public RoundState State { get; }
        public CommandHistory History { get; } = new CommandHistory();
        public event Action<RoundPhase> PhaseChanged;
        public BoardLayout Layout => game.Layout;
        public CardModel Waste => game.Waste;
        public int Chain => game.Chain;
        public int StockRemaining => game.StockRemaining;
        public int RemainingCards => game.RemainingCards;
        public bool HasNoMoves => game.HasNoMoves;
        private int ReservedStock => effects == null ? 0 : Math.Max(0, effects.ReservedStockCards);
        public bool CanDraw => EffectsActive && State.Phase == RoundPhase.Play && State.DrawsRemaining > 0 && game.StockRemaining > ReservedStock;
        public bool CanReroll => EffectsActive && State.Phase == RoundPhase.Play && RerollAvailable();
        public CardModel GetCard(int slot) => game.GetCard(slot);
        public bool IsRemoved(int slot) => game.IsRemoved(slot);
        public bool IsExposed(int slot) => game.IsExposed(slot);
        public bool IsBlind(int slot)=>game.IsBlind(slot);
        public DealerKind Dealer=>game.Dealer.Kind;
        public bool CanPlay(int slot) => EffectsActive && State.Phase == RoundPhase.Play && game.CanPlay(slot);

        public RoundSession(RuleEngine rules, decimal targetScore, int drawLimit,
            ScoringPipeline scoring = null, IScoreHook scoreHook = null, IRoundEffects effects = null)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (scoreHook != null && effects != null) throw new ArgumentException("Use one scoring hook boundary.");
            game = rules.Fork();
            State = new RoundState(targetScore, game.Dealer.StartingDraws(drawLimit));
            this.scoring = scoring ?? new ScoringPipeline();
            this.effects = effects;
            this.scoreHook = effects ?? scoreHook;
            game.MovePolicy = effects;
        }

        public bool Start()
        {
            if (executing || State.Phase != RoundPhase.Start) return false;
            executing = true;
            RoundMemento before = Capture();
            try
            {
                effects?.OnRoundStart(this);
                SetPhase(RoundPhase.Play);
                Resolve();
                return true;
            }
            catch { Restore(before); throw; }
            finally { executing = false; }
        }

        public bool TryPlay(int slot)
        {
            if (executing || !CanPlay(slot)) return false;
            RoundMemento before = Capture();
            executing = true;
            try
            {
                CardModel previousWaste = game.Waste;
                game.TryPlay(slot);
                State.CardsPlayed++;
                State.DrawsRemaining=checked(State.DrawsRemaining+game.Dealer.DrawRefund(game.Chain));
                State.LastCommand = CommandKind.Play;
                effects?.OnCardPlayed(this, game.Waste, previousWaste);
                SetPhase(RoundPhase.Score);
                State.Score += scoring.Evaluate(new ScoreContext(game.Waste, game.Chain), scoreHook);
                Resolve();
                History.Record(new RoundCommand(CommandKind.Play, slot), before);
                return true;
            }
            catch { Restore(before); throw; }
            finally { executing = false; }
        }

        public bool TryDraw() => Draw(false);
        public bool TryReroll() => Draw(true);

        private bool Draw(bool reroll)
        {
            if (executing || (reroll ? !CanReroll : !CanDraw)) return false;
            RoundMemento before = Capture();
            executing = true;
            try
            {
                if (reroll && !effects.PayForReroll(this)) { Restore(before); return false; }
                SetPhase(RoundPhase.Draw);
                if (!game.TryDraw()) { Restore(before); return false; }
                if (!reroll) State.DrawsRemaining--;
                State.LastCommand = reroll ? CommandKind.Reroll : CommandKind.Draw;
                effects?.OnDraw(this, reroll);
                SetPhase(RoundPhase.Score);
                Resolve();
                History.Record(new RoundCommand(reroll ? CommandKind.Reroll : CommandKind.Draw, -1), before);
                return true;
            }
            catch { Restore(before); throw; }
            finally { executing = false; }
        }

        public bool TryShuffleStock(SeededRNG rng)
        {
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (executing || !EffectsActive || State.Phase != RoundPhase.Play || game.StockRemaining < 2) return false;
            RoundMemento before = Capture(); executing = true;
            try
            {
                game.ShuffleStock(rng);
                History.Record(new RoundCommand(CommandKind.Shuffle, -1), before);
                return true;
            }
            catch { Restore(before); throw; }
            finally { executing = false; }
        }

        /// <summary>Completed mock-ad allowance grant. Never creates physical cards.</summary>
        public bool TryRewardDraw(bool revive)
        {
            if(executing || IsSealed || !EffectsActive || game.StockRemaining==0 ||
                (revive ? State.Outcome!=RoundOutcome.Lost : State.Phase!=RoundPhase.Play))return false;
            var before=Capture();executing=true;
            try{State.DrawsRemaining=checked(State.DrawsRemaining+1);State.Outcome=RoundOutcome.Pending;SetPhase(RoundPhase.Play);Resolve();History.Record(new RoundCommand(CommandKind.Reward,-1),before);return true;}
            catch{Restore(before);throw;}finally{executing=false;}
        }

        public bool TryUndo()
        {
            if (executing || IsSealed || !EffectsActive || !History.TryPop(out RoundMemento before)) return false;
            executing = true;
            try { Restore(before); return true; }
            finally { executing = false; }
        }

        public bool Seal()
        {
            if (!CanSeal) return false;
            IsSealed = true;
            return true;
        }

        public void GrantDraws(int count)
        {
            if (!executing || count < 0) throw new InvalidOperationException("Draw grants belong to an active round hook.");
            State.DrawsRemaining = checked(State.DrawsRemaining + count);
        }

        public void SetRescueDrawAllowance(int count)
        {
            if (!executing || count < 1) throw new InvalidOperationException("Rescue belongs to an active round hook.");
            State.DrawsRemaining = count;
        }

        private bool RerollAvailable() => game.StockRemaining > ReservedStock && effects != null && effects.CanReroll(this);

        private void Resolve()
        {
            if (State.Score >= State.TargetScore || game.IsCleared) State.Outcome = RoundOutcome.Won;
            else if (game.HasNoMoves && (State.DrawsRemaining == 0 || game.StockRemaining <= ReservedStock) && !RerollAvailable())
            {
                bool rescued = effects != null && effects.TryPreventDefeat(this);
                State.Outcome = rescued ? RoundOutcome.Pending : RoundOutcome.Lost;
            }
            else State.Outcome = RoundOutcome.Pending;
            bool terminal = State.Outcome != RoundOutcome.Pending;
            SetPhase(terminal ? RoundPhase.End : RoundPhase.Play);
            if (terminal) effects?.OnRoundEnd(this, State.Outcome);
        }

        private void SetPhase(RoundPhase phase)
        {
            if (State.Phase == phase) return;
            State.Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        private RoundMemento Capture() => new RoundMemento {
            Board = game.Capture(), Score = State.Score, Draws = State.DrawsRemaining,
            CardsPlayed = State.CardsPlayed, Phase = State.Phase, Outcome = State.Outcome,
            LastCommand = State.LastCommand, Effects = effects?.CaptureState()
        };

        private void Restore(RoundMemento before)
        {
            game.Restore(before.Board); State.Score = before.Score; State.DrawsRemaining = before.Draws;
            State.CardsPlayed = before.CardsPlayed; State.Outcome = before.Outcome;
            State.LastCommand = before.LastCommand;
            if (effects != null) effects.RestoreState(before.Effects);
            SetPhase(before.Phase);
        }
    }
}
