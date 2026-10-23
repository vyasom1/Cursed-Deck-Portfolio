using System;

namespace CursedDeck.Core
{
    public enum ScoringStage { BasePoints, FlatBonuses, ChainMultiplier, RelicMultipliers }

    public readonly struct ScoreContext
    {
        public CardModel Card { get; }
        public int Chain { get; }
        public ScoreContext(CardModel card, int chain)
        {
            if (!card.IsValid) throw new ArgumentException("Invalid scoring card.", nameof(card));
            if (chain < 1) throw new ArgumentOutOfRangeException(nameof(chain));
            Card = card; Chain = chain;
        }
    }

    public interface IScoreHook
    {
        decimal Apply(ScoringStage stage, ScoreContext context, decimal value);
    }

    /// <summary>Each stage calls the single hook exactly once, after its built-in operation.
    /// Decimal arithmetic preserves fractional chain/relic scores without rounding drift.</summary>
    public sealed class ScoringPipeline
    {
        public decimal BasePoints { get; }
        public decimal ChainStep { get; }
        public ScoringPipeline(decimal basePoints = 100m, decimal chainStep = 0.25m)
        {
            if (basePoints < 0) throw new ArgumentOutOfRangeException(nameof(basePoints));
            if (chainStep < 0) throw new ArgumentOutOfRangeException(nameof(chainStep));
            BasePoints = basePoints; ChainStep = chainStep;
        }

        public decimal Evaluate(ScoreContext context, IScoreHook hook = null)
        {
            if (!context.Card.IsValid || context.Chain < 1) throw new ArgumentException("Invalid context.");
            decimal value = Stage(ScoringStage.BasePoints, context, BasePoints, hook);
            value = Stage(ScoringStage.FlatBonuses, context, value, hook);
            value = Stage(ScoringStage.ChainMultiplier, context, value * (1m + (context.Chain - 1m) * ChainStep), hook);
            return Stage(ScoringStage.RelicMultipliers, context, value, hook);
        }

        private static decimal Stage(ScoringStage stage, ScoreContext context, decimal value, IScoreHook hook)
        {
            if (hook != null) value = hook.Apply(stage, context, value);
            if (value < 0) throw new InvalidOperationException("A scoring hook produced negative points.");
            return value;
        }
    }
}
