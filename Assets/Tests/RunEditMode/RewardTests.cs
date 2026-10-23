using System;
using System.Collections.Generic;
using NUnit.Framework;
using CursedDeck.Core;

namespace CursedDeck.Run.Tests
{
    public sealed class RewardTests
    {
        private static RelicSpec[] Catalog()
        {
            var result = new RelicSpec[8];
            for (int i = 0; i < 8; i++) result[i] = RelicTests.Spec((RelicKind)i);
            return result;
        }
        private static RoundSession WonRound()
        {
            var round = new RoundSession(RelicTests.Rules(new[] {RelicTests.C(6)},RelicTests.C(5)),1000m,0);
            round.Start(); round.TryPlay(0); return round;
        }
        private static string[] Ids(RewardOffer offer)
        {
            var result = new string[offer.Options.Count];
            for (int i = 0; i < result.Length; i++) result[i] = offer.Options[i].Id;
            return result;
        }

        [Test]
        public void OffersThreeUniqueUnownedRelicsAndOnlyOneCanBeClaimed()
        {
            var run = new RunState(42); var catalog = Catalog(); run.Acquire(catalog[0]);
            var round = WonRound(); var generator = new RewardGenerator(catalog,run.Seed);
            var offer = generator.OfferAfterRound(run,round);
            Assert.That(offer.Options.Count, Is.EqualTo(3));
            Assert.That(new HashSet<string>(Ids(offer)).Count, Is.EqualTo(3));
            foreach (var item in offer.Options) Assert.That(run.Owns(item.Id), Is.False);
            Assert.That(offer.TryChoose("missing"), Is.False);
            Assert.That(offer.Claimed, Is.False);
            Assert.That(offer.TryChoose(offer.Options[0].Id), Is.True);
            Assert.That(run.Owns(offer.Options[0].Id), Is.True);
            Assert.That(offer.TryChoose(offer.Options[1].Id), Is.False);
            Assert.That(run.Relics.Count, Is.EqualTo(2));
            Assert.That(round.IsSealed, Is.True);
            Assert.That(round.TryUndo(), Is.False);
            Assert.That(generator.OfferAfterRound(run,round), Is.SameAs(offer));
        }

        [Test]
        public void SameSeedAndCallSequenceProduceSameOffersRegardlessOfCatalogOrder()
        {
            var catalog = Catalog(); var reversed = Catalog(); Array.Reverse(reversed);
            var a = new RewardGenerator(catalog,-42); var b = new RewardGenerator(reversed,-42);
            var runA = new RunState(1); var runB = new RunState(1);
            for (int i = 0; i < 4; i++)
            {
                var first = a.OfferAfterRound(runA,WonRound()); var second = b.OfferAfterRound(runB,WonRound());
                CollectionAssert.AreEqual(Ids(first), Ids(second));
                if (first.Options.Count > 0) { first.TryChoose(first.Options[0].Id); second.TryChoose(second.Options[0].Id); }
            }
        }

        [TestCase(6,2)] [TestCase(7,1)] [TestCase(8,0)]
        public void SmallPoolReturnsRemainingUniqueOptionsWithoutOwnedDuplicates(int owned, int expected)
        {
            var catalog = Catalog(); var run = new RunState(1);
            for (int i = 0; i < owned; i++) run.Acquire(catalog[i]);
            var offer = new RewardGenerator(catalog,1).OfferAfterRound(run,WonRound());
            Assert.That(offer.Options.Count, Is.EqualTo(expected));
            foreach (var item in offer.Options) Assert.That(run.Owns(item.Id), Is.False);
        }

        [Test]
        public void OfferIsCachedAndCannotBeRerolledByRequestingAgain()
        {
            var run = new RunState(1); var round = WonRound(); var generator = new RewardGenerator(Catalog(),1);
            var offer = generator.OfferAfterRound(run,round);
            Assert.That(generator.OfferAfterRound(run,round), Is.SameAs(offer));
            Assert.Throws<InvalidOperationException>(() => generator.OfferAfterRound(new RunState(1),round));
            round.TryUndo();
            Assert.That(offer.TryChoose(offer.Options[0].Id), Is.False);
            round.TryPlay(0);
            Assert.That(generator.OfferAfterRound(run,round), Is.SameAs(offer));
        }

        [Test]
        public void RoundCanOnlyCommitOneRewardEvenAcrossGenerators()
        {
            var run = new RunState(1); var round = WonRound();
            var a = new RewardGenerator(Catalog(),1); var b = new RewardGenerator(Catalog(),2);
            var offerA = a.OfferAfterRound(run,round); var offerB = b.OfferAfterRound(run,round);
            Assert.That(offerA.TryChoose(offerA.Options[0].Id), Is.True);
            Assert.That(offerB.TryChoose(offerB.Options[0].Id), Is.False);
            Assert.That(run.Relics.Count, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => new RewardGenerator(Catalog(),3).OfferAfterRound(run,round));
        }

        [Test]
        public void ActiveAndDefeatedRoundsDoNotOfferRewards()
        {
            var run = new RunState(1); var generator = new RewardGenerator(Catalog(),1);
            var active = new RoundSession(RelicTests.Rules(new[] {RelicTests.C(6)},RelicTests.C(5)),1000m,0);
            active.Start();
            Assert.Throws<InvalidOperationException>(() => generator.OfferAfterRound(run,active));
            var lost = new RoundSession(RelicTests.Rules(new[] {RelicTests.C(9)},RelicTests.C(5)),1000m,0);
            lost.Start();
            Assert.Throws<InvalidOperationException>(() => generator.OfferAfterRound(run,lost));
        }

        [Test]
        public void InvalidCatalogsAreRejectedAndInputArrayIsCopied()
        {
            Assert.Throws<ArgumentNullException>(() => new RewardGenerator(null,1));
            var relic = RelicTests.Spec(RelicKind.LoadedDie);
            Assert.Throws<ArgumentException>(() => new RewardGenerator(new[] {relic,relic},1));
            Assert.Throws<ArgumentException>(() => new RewardGenerator(new RelicSpec[] {null},1));
            var catalog = new[] {relic}; var generator = new RewardGenerator(catalog,1); catalog[0] = null;
            Assert.That(generator.OfferAfterRound(new RunState(1),WonRound()).Options[0], Is.SameAs(relic));
        }
    }
}
