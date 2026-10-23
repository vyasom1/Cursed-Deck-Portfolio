using System;

namespace CursedDeck.Run
{
    public enum RelicKind { LoadedDie, Metronome, RedVelvet, CrackedMirror, GamblersRing, LastBreath, SuitBridge, ColorEcho, BlackVelvet, LongWick, LowStakes, HighStakes, FirstLight, GraveCompass, VoidStep, BoneAbacus, SoulLedger, StillHour, FrostPocket, BoneCrown }

    /// <summary>Immutable content snapshot. New variants of these mechanics require data only.</summary>
    public sealed class RelicSpec
    {
        public string Id { get; }
        public RelicKind Kind { get; }
        public int Order { get; }
        public int Amount { get; }
        public int Interval { get; }
        public decimal Multiplier { get; }
        public RelicSpec(string id, RelicKind kind, int order, int amount = 1, int interval = 1, decimal multiplier = 1m)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Relic needs a stable ID.", nameof(id));
            if (!Enum.IsDefined(typeof(RelicKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (amount < 1 || interval < 1 || multiplier <= 0) throw new ArgumentException("Relic parameters must be positive.");
            Id = id; Kind = kind; Order = order; Amount = amount; Interval = interval; Multiplier = multiplier;
        }
    }
}
