namespace CursedDeck.Core
{
    public interface IMovePolicy
    {
        bool AllowsMove(CardModel card, CardModel waste);
    }

    /// <summary>Pure C# extension boundary. Implementations own their run/relic state and
    /// provide mementos so command rollback and undo include all side effects.</summary>
    public interface IRoundEffects : IScoreHook, IMovePolicy
    {
        bool IsActive { get; }
        int ReservedStockCards { get; }
        void OnRoundStart(RoundSession round);
        void OnCardPlayed(RoundSession round, CardModel card, CardModel previousWaste);
        void OnDraw(RoundSession round, bool isReroll);
        void OnRoundEnd(RoundSession round, RoundOutcome outcome);
        bool CanReroll(RoundSession round);
        bool PayForReroll(RoundSession round);
        bool TryPreventDefeat(RoundSession round);
        object CaptureState();
        void RestoreState(object state);
    }
}
