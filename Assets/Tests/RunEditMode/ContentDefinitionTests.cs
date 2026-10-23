using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CursedDeck.Core;
using CursedDeck.Data;

namespace CursedDeck.Run.Tests
{
    public sealed class ContentDefinitionTests
    {
        [Test]
        public void FifteenAuthoredRelicAssetsImportWithTheExpectedParameters()
        {
            string[] guids = AssetDatabase.FindAssets("t:RelicDefinition",new[] {"Assets/_Data/Relics"});
            Assert.That(guids.Length, Is.EqualTo(15));
            var kinds = new HashSet<RelicKind>(); var ids = new HashSet<string>();
            foreach (string guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<RelicDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(asset, Is.Not.Null);
                Assert.That(asset.DisplayName, Is.Not.Empty);
                Assert.That(asset.Description, Is.Not.Empty);
                RelicSpec spec = asset.CreateSpec();
                Assert.That(kinds.Add(spec.Kind), Is.True); Assert.That(ids.Add(spec.Id), Is.True);
                if (spec.Kind == RelicKind.Metronome) { Assert.That(spec.Interval, Is.EqualTo(5)); Assert.That(spec.Amount, Is.EqualTo(1)); }
                if (spec.Kind == RelicKind.GamblersRing) Assert.That(spec.Amount, Is.EqualTo(10));
                if (spec.Kind == RelicKind.ColorEcho) Assert.That(spec.Interval, Is.EqualTo(3));
                if (spec.Kind == RelicKind.RedVelvet || spec.Kind == RelicKind.CrackedMirror) Assert.That(spec.Multiplier, Is.EqualTo(2m));
            }
            Assert.That(kinds.Count, Is.EqualTo(15));
        }

        [Test]
        public void AuthoredLayoutMatchesMvpAndDrivesTheSameSeededGenerator()
        {
            var asset = AssetDatabase.LoadAssetAtPath<BoardLayoutDefinition>("Assets/_Data/Layouts/TriPeaks.asset");
            Assert.That(asset, Is.Not.Null);
            BoardLayout layout = asset.CreateLayout();
            Assert.That(layout.Count, Is.EqualTo(28));
            for (int i = 0; i < layout.Count; i++)
            {
                Assert.That(layout[i].X, Is.EqualTo(BoardLayout.TriPeaks[i].X));
                Assert.That(layout[i].Y, Is.EqualTo(BoardLayout.TriPeaks[i].Y));
                CollectionAssert.AreEqual(BoardLayout.TriPeaks[i].CoveringSlots,layout[i].CoveringSlots);
            }
            var authored = WinnableBoardGenerator.Generate(42,layout);
            var original = WinnableBoardGenerator.Generate(42);
            CollectionAssert.AreEqual(original.Cards,authored.Cards);
            CollectionAssert.AreEqual(original.Stock,authored.Stock);
            CollectionAssert.AreEqual(original.SolutionSlots,authored.SolutionSlots);
            var game = authored.CreateGame();
            foreach (int slot in authored.SolutionSlots) Assert.That(game.TryPlay(slot), Is.True);
            Assert.That(game.IsCleared, Is.True);
        }

        [Test]
        public void DataOnlyVariantChangesMechanicWithoutNewRuntimeCode()
        {
            var asset = ScriptableObject.CreateInstance<RelicDefinition>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"id\":\"quick-beat\",\"kind\":1,\"interval\":2,\"amount\":3}",asset);
                var run = new RunState(1); run.Acquire(asset.CreateSpec());
                var round = RelicTests.Begin(run,new[] {RelicTests.C(6),RelicTests.C(7),RelicTests.C(8)},RelicTests.C(5));
                round.TryPlay(0); round.TryPlay(1);
                Assert.That(round.State.DrawsRemaining, Is.EqualTo(3));
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
        }

        [Test]
        public void SharedDefinitionDoesNotShareMutableRuntimeState()
        {
            var asset = AssetDatabase.LoadAssetAtPath<RelicDefinition>("Assets/_Data/Relics/last-breath.asset");
            string before = EditorJsonUtility.ToJson(asset);
            var first = new RunState(1); var second = new RunState(1);
            first.Acquire(asset.CreateSpec()); second.Acquire(asset.CreateSpec());
            RelicTests.Begin(first,new[] {RelicTests.C(9)},RelicTests.C(3),0,RelicTests.C(8,Suit.Spades));
            Assert.That(first.LastBreathUsed, Is.True);
            Assert.That(second.LastBreathUsed, Is.False);
            Assert.That(EditorJsonUtility.ToJson(asset), Is.EqualTo(before));
        }

        [Test]
        public void InvalidDefinitionsAndUnsupportedLayoutSizesAreRejected()
        {
            var relic = ScriptableObject.CreateInstance<RelicDefinition>();
            var layout = ScriptableObject.CreateInstance<BoardLayoutDefinition>();
            try
            {
                Assert.Throws<ArgumentException>(() => relic.CreateSpec());
                Assert.Throws<InvalidOperationException>(() => layout.CreateLayout());
                Assert.Throws<ArgumentNullException>(() => WinnableBoardGenerator.Generate(1,null));
                Assert.Throws<ArgumentException>(() => WinnableBoardGenerator.Generate(1,new BoardLayout(new[] {new BoardSlot(0,0)})));
            }
            finally { UnityEngine.Object.DestroyImmediate(relic); UnityEngine.Object.DestroyImmediate(layout); }
        }
    }
}
