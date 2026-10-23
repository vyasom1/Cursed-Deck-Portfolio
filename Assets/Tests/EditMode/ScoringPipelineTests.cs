using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CursedDeck.Core.Tests
{
    public sealed class ScoringPipelineTests
    {
        private sealed class Hook : IScoreHook
        {
            public readonly List<ScoringStage> Stages = new List<ScoringStage>();
            public readonly List<decimal> Inputs = new List<decimal>();
            public decimal Apply(ScoringStage stage, ScoreContext context, decimal value)
            {
                Stages.Add(stage); Inputs.Add(value);
                if (stage == ScoringStage.FlatBonuses) return value + 20;
                if (stage == ScoringStage.RelicMultipliers) return value * 2;
                return value;
            }
        }
        private sealed class NegativeHook : IScoreHook
        {
            public decimal Apply(ScoringStage stage, ScoreContext context, decimal value) => -1;
        }
        private static ScoreContext Context(int chain) => new ScoreContext(new CardModel(Rank.Six, Suit.Hearts), chain);

        [Test]
        public void ExactlyFourHooksRunInOrderAndFlatBonusIsMultiplied()
        {
            var hook = new Hook();
            Assert.That(new ScoringPipeline().Evaluate(Context(2), hook), Is.EqualTo(300m));
            CollectionAssert.AreEqual(new[] {ScoringStage.BasePoints, ScoringStage.FlatBonuses,
                ScoringStage.ChainMultiplier, ScoringStage.RelicMultipliers}, hook.Stages);
            CollectionAssert.AreEqual(new[] {100m, 100m, 150m, 150m}, hook.Inputs);
        }

        [TestCase(1, 100)] [TestCase(2, 125)] [TestCase(5, 200)] [TestCase(28, 775)]
        public void ChainMathStartsAtOneTimes(int chain, int expected) =>
            Assert.That(new ScoringPipeline().Evaluate(Context(chain)), Is.EqualTo((decimal)expected));

        [Test]
        public void FractionalScoresArePreservedAndConfigurationIsRespected()
        {
            Assert.That(new ScoringPipeline(3m, 0.1m).Evaluate(Context(2)), Is.EqualTo(3.3m));
            Assert.That(new ScoringPipeline(0m, 0m).Evaluate(Context(5)), Is.Zero);
        }

        [Test]
        public void InvalidInputsAndNegativeHookResultsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ScoringPipeline(-1m));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ScoringPipeline(1m, -1m));
            Assert.Throws<ArgumentOutOfRangeException>(() => Context(0));
            Assert.Throws<ArgumentException>(() => new ScoreContext(default, 1));
            Assert.Throws<ArgumentException>(() => new ScoringPipeline().Evaluate(default));
            Assert.Throws<InvalidOperationException>(() => new ScoringPipeline().Evaluate(Context(1), new NegativeHook()));
        }
    }
}
