using NUnit.Framework;
using UnityEngine;
using CursedDeck.Data;
namespace CursedDeck.Run.Tests
{
    public sealed class BalanceSimulatorTests
    {
        [Test] public void SameSeedAndProfileProduceIdenticalCsvAndOffers(){var c=Resources.Load<GameContent>("CursedDeck/Content").CreateCatalog();var a=BalanceSimulator.Evaluate(42,3,c);var b=BalanceSimulator.Evaluate(42,3,c);Assert.That(a.Csv("none"),Is.EqualTo(b.Csv("none")));CollectionAssert.AreEqual(a.Offers,b.Offers);Assert.That(a.Steps,Is.InRange(1,128));Assert.That(a.Csv("none"),Does.Contain(",Mirror,"));}
        [Test] public void HeuristicOnlyChoosesCurrentlyLegalExposedCards(){var c=Resources.Load<GameContent>("CursedDeck/Content").CreateCatalog();var run=new RunController(73,c);var rng=new Core.SeededRNG(73);for(int n=0;n<100&&run.State.Stage==RunStage.Playing;n++){int slot=BalanceSimulator.ChoosePlay(run,rng);if(slot>=0){Assert.That(run.CurrentRound.CanPlay(slot));Assert.That(run.CurrentRound.IsExposed(slot));Assert.That(run.Execute(new RunAction(RunActionKind.Play,slot)));}else if(run.CurrentRound.CanDraw)Assert.That(run.Execute(new RunAction(RunActionKind.Draw)));else break;}Assert.That(run.State.Stage,Is.Not.EqualTo(RunStage.Playing));}
        [TestCase(1)][TestCase(2)][TestCase(3)] public void FloorSamplesTerminateAndOfferOnlyUnownedRelics(int floor){var c=Resources.Load<GameContent>("CursedDeck/Content").CreateCatalog();var starting=new[]{c[0],c[1]};for(int seed=1;seed<=10;seed++){var r=BalanceSimulator.Evaluate(seed,floor,c,starting);Assert.That(r.Score,Is.GreaterThanOrEqualTo(0));Assert.That(r.Steps,Is.LessThanOrEqualTo(128));CollectionAssert.DoesNotContain(r.Offers,c[0].Id);CollectionAssert.DoesNotContain(r.Offers,c[1].Id);if(r.Won)CollectionAssert.Contains(r.Offers,r.Pick);else Assert.That(r.Pick,Is.Null);}}
    }
}
