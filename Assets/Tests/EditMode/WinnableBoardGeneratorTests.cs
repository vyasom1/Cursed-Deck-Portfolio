using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace CursedDeck.Core.Tests
{
    public sealed class WinnableBoardGeneratorTests
    {
        [TestCase(0)] [TestCase(42)] [TestCase(-42)] [TestCase(int.MinValue)] [TestCase(int.MaxValue)]
        public void SameSeedReproducesEveryZoneAndSolution(int seed)
        {
            var first = WinnableBoardGenerator.Generate(seed);
            var second = WinnableBoardGenerator.Generate(seed);
            Assert.That(first.Seed, Is.EqualTo(seed));
            Assert.That(first.InitialWaste, Is.EqualTo(second.InitialWaste));
            CollectionAssert.AreEqual(first.Cards, second.Cards);
            CollectionAssert.AreEqual(first.Stock, second.Stock);
            CollectionAssert.AreEqual(first.SolutionSlots, second.SolutionSlots);
        }

        [Test]
        public void DifferentSeedsChangeTheDeal()
        {
            var signatures = new HashSet<string>();
            for (int seed = 0; seed < 100; seed++)
            {
                var board = WinnableBoardGenerator.Generate(seed);
                signatures.Add(string.Join(",", board.Cards));
            }
            Assert.That(signatures.Count, Is.EqualTo(100));
        }

        [Test]
        public void OneThousandSeedsUseExactlyOneStandardDeckAndHaveLegalClearingWitnesses()
        {
            for (int seed = -500; seed < 500; seed++) VerifyDeal(WinnableBoardGenerator.Generate(seed));
            VerifyDeal(WinnableBoardGenerator.Generate(int.MinValue));
            VerifyDeal(WinnableBoardGenerator.Generate(int.MaxValue));
        }

        private static void VerifyDeal(GeneratedBoard deal)
        {
            Assert.That(deal.Cards.Count, Is.EqualTo(28));
            Assert.That(deal.Stock.Count, Is.EqualTo(23));
            Assert.That(deal.SolutionSlots.Count, Is.EqualTo(28));
            var all = new HashSet<CardModel>(deal.Cards);
            Assert.That(all.Add(deal.InitialWaste), Is.True);
            foreach (CardModel card in deal.Stock) Assert.That(all.Add(card), Is.True);
            CollectionAssert.AreEquivalent(DeckModel.CreateStandardCards(), all);

            // Independently audit rank transitions and covering edges before asking the engine.
            var removed = new HashSet<int>();
            CardModel waste = deal.InitialWaste;
            var game = deal.CreateGame();
            foreach (int slot in deal.SolutionSlots)
            {
                Assert.That(slot, Is.InRange(0, 27));
                Assert.That(removed.Contains(slot), Is.False);
                foreach (int covering in deal.Layout[slot].CoveringSlots)
                    Assert.That(removed.Contains(covering), Is.True, "seed " + deal.Seed + ", slot " + slot);
                int rank = (int)deal.Cards[slot].Rank;
                int previous = (int)waste.Rank;
                Assert.That(rank == previous % 13 + 1 || previous == rank % 13 + 1, Is.True);
                Assert.That(game.CanPlay(slot), Is.True);
                Assert.That(game.TryPlay(slot), Is.True);
                removed.Add(slot); waste = deal.Cards[slot];
            }
            Assert.That(game.IsCleared, Is.True);
            Assert.That(game.Chain, Is.EqualTo(28));
            Assert.That(game.StockRemaining, Is.EqualTo(23));
            Assert.That(game.RemainingCards, Is.Zero);
        }

        [Test]
        public void EachCreatedGameHasIndependentState()
        {
            var deal = WinnableBoardGenerator.Generate(42);
            var first = deal.CreateGame(); var second = deal.CreateGame();
            first.TryPlay(deal.SolutionSlots[0]);
            Assert.That(second.RemainingCards, Is.EqualTo(28));
            Assert.That(second.Waste, Is.EqualTo(deal.InitialWaste));
            Assert.That(second.Chain, Is.Zero);
            Assert.That(deal.Cards is CardModel[], Is.False);
            Assert.That(deal.Stock is CardModel[], Is.False);
            Assert.That(deal.SolutionSlots is int[], Is.False);
        }

        [Test]
        public void Seed42ProducesVerifiedWrittenSolutionTrace()
        {
            var deal = WinnableBoardGenerator.Generate(42);
            VerifyDeal(deal);
            var game = deal.CreateGame();
            var text = new StringBuilder();
            text.AppendLine("# Seed 42: verified solvable sequence");
            text.AppendLine();
            text.AppendLine("Generator version: " + GeneratedBoard.GeneratorVersion + ". Slots are zero-based.");
            text.AppendLine("Initial waste: **" + deal.InitialWaste + "**. Stock: 23 cards; no draws used.");
            text.AppendLine();
            text.AppendLine("| Slot | Card | Covering slots (must be removed first) |");
            text.AppendLine("|---|---|---|");
            for (int i = 0; i < deal.Cards.Count; i++)
                text.AppendLine("| " + i + " | " + deal.Cards[i] + " | " + string.Join(", ", deal.Layout[i].CoveringSlots) + " |");
            text.AppendLine();
            text.AppendLine("Stock in draw order: " + string.Join(", ", deal.Stock) + ".");
            text.AppendLine();
            text.AppendLine("| Step | Slot | Waste before | Played / waste after | Exposed | Rank legal | Chain | Cards left |");
            text.AppendLine("|---|---|---|---|---|---|---|---|");
            for (int i = 0; i < deal.SolutionSlots.Count; i++)
            {
                int slot = deal.SolutionSlots[i];
                CardModel before = game.Waste;
                Assert.That(game.IsExposed(slot), Is.True);
                Assert.That(game.CanPlay(slot), Is.True);
                Assert.That(game.TryPlay(slot), Is.True);
                text.AppendLine("| " + (i + 1) + " | " + slot + " | " + before + " | " + game.Waste
                    + " | yes | yes | " + game.Chain + " | " + game.RemainingCards + " |");
            }
            Assert.That(game.IsCleared, Is.True);
            text.AppendLine();
            text.AppendLine("Result: all 28 board cards removed through legal exposed-card moves; chain 28; stock unchanged at 23.");
            text.AppendLine("The test independently checked each covering edge and rank transition, then replayed every move through RuleEngine.");
            string trace = text.ToString();
            TestContext.WriteLine(trace);
            // Optional host output, never a production asset. Batch setup supplies this path.
            string output = Environment.GetEnvironmentVariable("CURSED_DECK_TRACE_PATH");
            if (!string.IsNullOrEmpty(output)) File.WriteAllText(output, trace);
        }
    }
}
