using System;
using NUnit.Framework;

namespace CursedDeck.Core.Tests
{
    public sealed class BoardLayoutTests
    {
        [Test]
        public void TriPeaksHasCanonicalTwentyEightSlotTopology()
        {
            var layout = BoardLayout.TriPeaks;
            Assert.That(layout.Count, Is.EqualTo(28));
            int[][] expected = {
                new[] {3,4}, new[] {5,6}, new[] {7,8},
                new[] {9,10}, new[] {10,11}, new[] {12,13}, new[] {13,14}, new[] {15,16}, new[] {16,17},
                new[] {18,19}, new[] {19,20}, new[] {20,21}, new[] {21,22}, new[] {22,23},
                new[] {23,24}, new[] {24,25}, new[] {25,26}, new[] {26,27}
            };
            for (int i = 0; i < 18; i++) CollectionAssert.AreEqual(expected[i], layout[i].CoveringSlots);
            for (int i = 18; i < 28; i++) Assert.That(layout[i].CoveringSlots.Count, Is.Zero);
        }

        [Test]
        public void LayoutDefensivelyCopiesData()
        {
            var covers = new[] { 1 };
            var source = new[] { new BoardSlot(2, 0, covers), new BoardSlot(1, 1) };
            var layout = new BoardLayout(source);
            covers[0] = 0; source[0] = new BoardSlot(9, 9);
            Assert.That(layout[0].X, Is.EqualTo(2));
            Assert.That(layout[0].Y, Is.Zero);
            CollectionAssert.AreEqual(new[] {1}, layout[0].CoveringSlots);
        }

        [Test]
        public void RejectsNullEmptyOversizedAndNullSlotLayouts()
        {
            Assert.Throws<ArgumentNullException>(() => new BoardLayout(null));
            Assert.Throws<ArgumentException>(() => new BoardLayout(new BoardSlot[0]));
            Assert.Throws<ArgumentException>(() => new BoardLayout(new BoardSlot[52]));
            Assert.Throws<ArgumentException>(() => new BoardLayout(new BoardSlot[1]));
            Assert.Throws<ArgumentNullException>(() => new BoardSlot(0, 0, null));
        }

        [TestCase(-1)] [TestCase(2)] [TestCase(0)]
        public void RejectsInvalidCoveringIndexOrSelfReference(int covering) =>
            Assert.Throws<ArgumentException>(() => new BoardLayout(new[] {
                new BoardSlot(0, 0, covering), new BoardSlot(0, 1) }));

        [Test]
        public void RejectsDuplicateCoveringEdgesAndCycles()
        {
            Assert.Throws<ArgumentException>(() => new BoardLayout(new[] {
                new BoardSlot(0, 0, 1, 1), new BoardSlot(0, 1) }));
            Assert.Throws<ArgumentException>(() => new BoardLayout(new[] {
                new BoardSlot(0, 0, 1), new BoardSlot(0, 1, 0) }));
            Assert.Throws<ArgumentException>(() => new BoardLayout(new[] {
                new BoardSlot(0, 0, 1), new BoardSlot(0, 1, 2), new BoardSlot(0, 2, 0) }));
        }
    }
}
