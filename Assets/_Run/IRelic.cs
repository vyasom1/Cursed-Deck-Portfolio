using CursedDeck.Core;

namespace CursedDeck.Run
{
    public sealed class RelicContext
    {
        public RunState Run { get; }
        public RoundSession Round { get; internal set; }
        public bool AfterDraw { get; internal set; }
        internal RelicContext(RunState run) { Run = run; }
    }

    public interface IRelic
    {
        RelicSpec Definition { get; }
        void OnRoundStart(RelicContext context);
        void OnCardPlayed(RelicContext context, CardModel card, CardModel previousWaste);
        void OnDraw(RelicContext context, bool isReroll);
        decimal OnScore(RelicContext context, ScoringStage stage, ScoreContext score, decimal value);
        void OnRoundEnd(RelicContext context, RoundOutcome outcome);
        object CaptureState();
        void RestoreState(object state);
    }

    public interface IRelicRules
    {
        bool AllowsMove(RelicContext context, CardModel card, CardModel waste);
        int ReservedStockCards(RelicContext context);
        bool CanReroll(RelicContext context);
        bool PayForReroll(RelicContext context);
        bool TryPreventDefeat(RelicContext context);
    }

    public abstract class RelicBase : IRelic
    {
        public RelicSpec Definition { get; }
        protected RelicBase(RelicSpec definition) { Definition = definition ?? throw new System.ArgumentNullException(nameof(definition)); }
        public virtual void OnRoundStart(RelicContext context) { }
        public virtual void OnCardPlayed(RelicContext context, CardModel card, CardModel previousWaste) { }
        public virtual void OnDraw(RelicContext context, bool isReroll) { }
        public virtual decimal OnScore(RelicContext context, ScoringStage stage, ScoreContext score, decimal value) => value;
        public virtual void OnRoundEnd(RelicContext context, RoundOutcome outcome) { }
        public virtual object CaptureState() => null;
        public virtual void RestoreState(object state) { }
    }
}
