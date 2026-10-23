using System;
using System.Collections.Generic;
using CursedDeck.Core;

namespace CursedDeck.Run
{
    public sealed class RewardOffer
    {
        private readonly RunState owner;
        private readonly RoundSession round;
        public IReadOnlyList<RelicSpec> Options { get; }
        public bool Claimed { get; private set; }
        public int FallbackGold {get;}
        internal RewardOffer(RunState owner, RoundSession round, RelicSpec[] options,int fallbackGold)
        {
            FallbackGold=options.Length==0?fallbackGold:0;this.owner = owner; this.round = round; Options = Array.AsReadOnly(options);
        }
        internal bool BelongsTo(RunState run) => ReferenceEquals(owner, run);
        public bool TryChoose(string id)
        {
            if (Claimed || !round.CanSeal || round.State.Outcome != RoundOutcome.Won) return false;
            if(Options.Count==0){if(!string.IsNullOrEmpty(id)||!round.Seal())return false;owner.AddGold(Math.Min(FallbackGold,int.MaxValue-owner.Gold));Claimed=true;return true;}
            for (int i = 0; i < Options.Count; i++)
            {
                if (StringComparer.Ordinal.Equals(Options[i].Id, id) && owner.Acquire(Options[i]))
                { round.Seal(); Claimed = true; return true; }
            }
            return false;
        }
    }

    public sealed class RewardGenerator
    {
        private readonly RelicSpec[] catalog;
        private readonly SeededRNG rng;
        private readonly Dictionary<RoundSession, RewardOffer> offers = new Dictionary<RoundSession, RewardOffer>();

        public RewardGenerator(IReadOnlyList<RelicSpec> catalog, int seed)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            this.catalog = new RelicSpec[catalog.Count];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i] == null || !seen.Add(catalog[i].Id)) throw new ArgumentException("Null/duplicate catalog relic.");
                this.catalog[i] = catalog[i];
            }
            Array.Sort(this.catalog, (a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
            rng = new SeededRNG(seed);
        }

        public RewardOffer OfferAfterRound(RunState run, RoundSession round,int fallbackGold=0)
        {
            if(fallbackGold<0)throw new ArgumentOutOfRangeException(nameof(fallbackGold));
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (round == null) throw new ArgumentNullException(nameof(round));
            if (round.State.Phase != RoundPhase.End || round.State.Outcome != RoundOutcome.Won)
                throw new InvalidOperationException("Rewards follow won rounds; defeated runs end.");
            if (offers.TryGetValue(round, out RewardOffer existing))
            {
                if (!existing.BelongsTo(run)) throw new InvalidOperationException("Offer belongs to another run.");
                return existing;
            }
            if (round.IsSealed) throw new InvalidOperationException("This round's reward was already committed.");
            var eligible = new List<RelicSpec>(catalog.Length);
            for (int i = 0; i < catalog.Length; i++) if (!run.Owns(catalog[i].Id)) eligible.Add(catalog[i]);
            rng.Shuffle(eligible);
            var result = new RelicSpec[Math.Min(3, eligible.Count)];
            for (int i = 0; i < result.Length; i++) result[i] = eligible[i];
            var offer = new RewardOffer(run, round, result,fallbackGold);
            offers.Add(round, offer);
            return offer;
        }
    }
}
