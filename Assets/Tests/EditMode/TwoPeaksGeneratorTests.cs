using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
namespace CursedDeck.Core.Tests
{
    public sealed class TwoPeaksGeneratorTests
    {
        [TestCase(DealerKind.None)][TestCase(DealerKind.Apprentice)][TestCase(DealerKind.Clockmaker)][TestCase(DealerKind.Mirror)]
        public void OneThousandTwoPeakSeedsHaveIndependentLegalBackwardWitnesses(DealerKind dealer)
        {
            for(int seed=-500;seed<500;seed++)
            {
                var deal=WinnableBoardGenerator.Generate(seed,BoardLayout.TwoPeaks,dealer);
                Assert.That(deal.Cards.Count,Is.EqualTo(19));Assert.That(deal.Stock.Count,Is.EqualTo(32));Assert.That(deal.SolutionSlots.Count,Is.EqualTo(19));
                var physical=new HashSet<CardModel>(deal.Cards);Assert.That(physical.Add(deal.InitialWaste));foreach(var card in deal.Stock)Assert.That(physical.Add(card));CollectionAssert.AreEquivalent(DeckModel.CreateStandardCards(),physical);
                var removed=new HashSet<int>();var waste=deal.InitialWaste;var game=deal.CreateGame();
                var trace=seed==42&&dealer==DealerKind.None?new StringBuilder("# Two peaks: seed 42 clearing witness\n\nGenerator version 1. 19 board cards + 1 waste + 32 stock. No relics or draws.\nInitial waste: "+waste+".\n\n| Step | Slot | Waste before | Played | Chain | Remaining |\n|---|---|---|---|---|---|\n"):null;
                foreach(int slot in deal.SolutionSlots)
                {
                    Assert.That(removed.Add(slot),Is.True,"duplicate slot, seed "+seed);
                    foreach(int covering in deal.Layout[slot].CoveringSlots)Assert.That(removed.Contains(covering),Is.True,"cover edge seed "+seed);
                    int a=(int)waste.Rank,b=(int)deal.Cards[slot].Rank;Assert.That(a%13+1==b||b%13+1==a,Is.True,"rank edge seed "+seed);
                    Assert.That(game.TryPlay(slot),Is.True,"seed "+seed+" dealer "+dealer+" slot "+slot);
                    trace?.AppendLine("| "+removed.Count+" | "+slot+" | "+waste+" | "+deal.Cards[slot]+" | "+game.Chain+" | "+game.RemainingCards+" |");waste=deal.Cards[slot];
                }
                Assert.That(game.IsCleared);Assert.That(game.Chain,Is.EqualTo(19));Assert.That(game.StockRemaining,Is.EqualTo(32));
                if(trace!=null)
                {
                    trace.AppendLine("\nResult: all 19 cards legally cleared. Every covering edge and rank transition independently checked, then replayed through RuleEngine. Stock unchanged at 32.");
                    TestContext.WriteLine(trace.ToString());string path=Environment.GetEnvironmentVariable("CURSED_DECK_TWOPEAK_TRACE_PATH");if(!string.IsNullOrEmpty(path))File.WriteAllText(path,trace.ToString());
                }
                var repeat=WinnableBoardGenerator.Generate(seed,BoardLayout.TwoPeaks,dealer);CollectionAssert.AreEqual(deal.Cards,repeat.Cards);CollectionAssert.AreEqual(deal.Stock,repeat.Stock);CollectionAssert.AreEqual(deal.SolutionSlots,repeat.SolutionSlots);Assert.That(deal.InitialWaste,Is.EqualTo(repeat.InitialWaste));
            }
        }
        [Test]public void LegacySeed42MatchesThePreviouslyRecordedWholeDeckAndWitness()
        {
            // Fixture recorded in Docs/Validation/seed-42-solution.md before this overhaul.
            var deal=WinnableBoardGenerator.Generate(42);
            var expected=new[]{25,21,13,31,29,1,18,19,9,27,22,34,51,0,15,14,10,16,20,17,26,50,3,49,7,4,11,12};
            for(int i=0;i<expected.Length;i++)Assert.That(deal.Cards[i].GetHashCode(),Is.EqualTo(expected[i]));
            Assert.That(deal.InitialWaste.GetHashCode(),Is.EqualTo(2));
            var stock=new[]{23,48,38,44,43,33,5,41,30,39,35,24,6,42,32,40,37,28,47,36,45,8,46};
            for(int i=0;i<stock.Length;i++)Assert.That(deal.Stock[i].GetHashCode(),Is.EqualTo(stock[i]));
            CollectionAssert.AreEqual(new[]{21,22,23,13,12,5,24,26,25,16,27,17,14,8,15,7,2,6,1,19,18,20,10,9,3,11,4,0},deal.SolutionSlots);
        }
        [Test]public void TwoPeaksHasTwoSummitsAndSevenExposedBaseCards()
        {
            var layout=BoardLayout.TwoPeaks;int top=0,baseCount=0;
            for(int i=0;i<layout.Count;i++){if(layout[i].Y==0)top++;if(layout[i].CoveringSlots.Count==0){Assert.That(layout[i].Y,Is.EqualTo(3));baseCount++;}}
            Assert.That(top,Is.EqualTo(2));Assert.That(baseCount,Is.EqualTo(7));
        }
    }
}
