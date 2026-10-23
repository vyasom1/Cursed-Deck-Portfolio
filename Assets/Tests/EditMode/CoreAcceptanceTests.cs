using NUnit.Framework;

namespace CursedDeck.Core.Tests
{
    public sealed class CoreAcceptanceTests
    {
        [Test]
        public void GeneratorVersionOnePreservesSeed42Witness()
        {
            var deal = WinnableBoardGenerator.Generate(42);
            Assert.That(GeneratedBoard.GeneratorVersion, Is.EqualTo(1));
            Assert.That(deal.InitialWaste, Is.EqualTo(new CardModel(Rank.Ace, Suit.Hearts)));
            CollectionAssert.AreEqual(new[] {
                21,22,23,13,12,5,24,26,25,16,27,17,14,8,15,7,2,6,1,19,18,20,10,9,3,11,4,0
            }, deal.SolutionSlots);
            var game = deal.CreateGame();
            foreach (int slot in deal.SolutionSlots) Assert.That(game.TryPlay(slot), Is.True);
            Assert.That(game.RemainingCards, Is.Zero);
            Assert.That(game.StockRemaining, Is.EqualTo(23));
        }

        [Test]
        public void CompiledCoreAssemblyHasNoEngineOrPresentationReference()
        {
            foreach (var dependency in typeof(CardModel).Assembly.GetReferencedAssemblies())
            {
                Assert.That(dependency.Name.StartsWith("Unity"), Is.False, dependency.Name);
                Assert.That(dependency.Name.StartsWith("CursedDeck."), Is.False, dependency.Name);
            }
        }
    }
}
