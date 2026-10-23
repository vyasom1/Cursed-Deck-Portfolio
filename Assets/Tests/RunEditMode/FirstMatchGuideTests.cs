using NUnit.Framework;
using CursedDeck.Core;
namespace CursedDeck.Run.Tests
{
    public sealed class FirstMatchGuideTests
    {
        [Test] public void TeachingDealUsesNormalRulesAndDemonstratesARealBlockThenUsefulDraw()
        {
            var deal=WinnableBoardGenerator.Generate(RunController.BoardSeedFor(FirstMatchGuide.Seed,1,1));var game=deal.CreateGame();Assert.That(game.TryPlay(FirstMatchGuide.Slot));Assert.That(game.HasNoMoves);Assert.That(game.TryDraw());Assert.That(game.HasNoMoves,Is.False);Assert.That(game.Chain,Is.Zero);
            var witness=deal.CreateGame();foreach(int slot in deal.SolutionSlots)Assert.That(witness.TryPlay(slot));Assert.That(witness.IsCleared);
        }
    }
}
