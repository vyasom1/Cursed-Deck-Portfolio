using System;
namespace CursedDeck.Core
{
    public enum DealerKind { None, Apprentice, Clockmaker, Mirror }
    // Strategies are immutable. All counters live in RuleEngine's undo memento.
    public interface IDealerStrategy
    {
        DealerKind Kind { get; }
        int StartingDraws(int configured);
        bool Allows(CardModel card, int numberedPlayed);
        int DrawRefund(int chain);
        bool HidesNext(int plays);
    }
    public static class DealerStrategies
    {
        private sealed class Standard : IDealerStrategy
        { public DealerKind Kind=>DealerKind.None; public int StartingDraws(int n)=>n; public bool Allows(CardModel c,int n)=>true; public int DrawRefund(int n)=>0; public bool HidesNext(int n)=>false; }
        private sealed class Apprentice : IDealerStrategy
        { public DealerKind Kind=>DealerKind.Apprentice; public int StartingDraws(int n)=>n; public bool Allows(CardModel c,int n)=>(int)c.Rank<=10||n>=3; public int DrawRefund(int n)=>0; public bool HidesNext(int n)=>false; }
        private sealed class Clockmaker : IDealerStrategy
        { public DealerKind Kind=>DealerKind.Clockmaker; public int StartingDraws(int n)=>Math.Max(0,n-3); public bool Allows(CardModel c,int n)=>true; public int DrawRefund(int n)=>n>=5?1:0; public bool HidesNext(int n)=>false; }
        private sealed class Mirror : IDealerStrategy
        { public DealerKind Kind=>DealerKind.Mirror; public int StartingDraws(int n)=>n; public bool Allows(CardModel c,int n)=>true; public int DrawRefund(int n)=>0; public bool HidesNext(int n)=>n%4==3; }
        private static readonly IDealerStrategy[] All={new Standard(),new Apprentice(),new Clockmaker(),new Mirror()};
        public static IDealerStrategy For(DealerKind kind)
        { if(!Enum.IsDefined(typeof(DealerKind),kind))throw new ArgumentOutOfRangeException(nameof(kind));return All[(int)kind]; }
    }
}
