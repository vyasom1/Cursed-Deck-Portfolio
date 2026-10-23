using System;
using NUnit.Framework;

namespace CursedDeck.Core.Tests
{
    public sealed class SeededRNGTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(-1)] [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
        public void SameSeedProducesSameStream(int seed)
        {
            var first = new SeededRNG(seed); var second = new SeededRNG(seed);
            for (int i = 0; i < 1000; i++) Assert.That(first.NextUInt(), Is.EqualTo(second.NextUInt()));
        }

        [Test]
        public void VersionOneStreamMatchesPublishedSplitMix64Vectors()
        {
            var rng = new SeededRNG(0);
            // Upper 32 bits of SplitMix64's first outputs for initial state zero.
            Assert.That(rng.NextUInt(), Is.EqualTo(0xE220A839u));
            Assert.That(rng.NextUInt(), Is.EqualTo(0x6E789E6Au));
            Assert.That(rng.NextUInt(), Is.EqualTo(0x06C45D18u));
        }

        [TestCase(1)] [TestCase(2)] [TestCase(13)] [TestCase(52)] [TestCase(int.MaxValue)]
        public void BoundedIntegersStayWithinExclusiveRange(int bound)
        {
            var rng = new SeededRNG(42);
            for (int i = 0; i < 1000; i++) Assert.That(rng.NextInt(bound), Is.InRange(0, bound - 1));
        }

        [TestCase(0)] [TestCase(-1)]
        public void RejectsInvalidBounds(int bound) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new SeededRNG(1).NextInt(bound));

        [Test]
        public void ShuffleIsDeterministicPermutationAndHandlesSmallCollections()
        {
            var first = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var second = (int[])first.Clone();
            new SeededRNG(123).Shuffle(first); new SeededRNG(123).Shuffle(second);
            CollectionAssert.AreEqual(first, second);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, first);
            new SeededRNG(1).Shuffle(new int[0]);
            var singleton = new[] { 9 }; new SeededRNG(1).Shuffle(singleton);
            Assert.That(singleton[0], Is.EqualTo(9));
            Assert.Throws<ArgumentNullException>(() => new SeededRNG(1).Shuffle<int>(null));
        }
    }
}
