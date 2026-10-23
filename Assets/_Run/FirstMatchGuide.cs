using System;
using CursedDeck.Core;
namespace CursedDeck.Run
{
    /// <summary>Finds a deterministic normal deal that teaches a match, a genuine block, then a useful draw.
    /// No card identities or rank rules are changed. Cached once, outside frame loops.</summary>
    public static class FirstMatchGuide
    {
        public readonly struct Opening{public readonly int Seed,Slot;public Opening(int seed,int slot){Seed=seed;Slot=slot;}}
        private static readonly Opening Value=Find(BoardLayout.TriPeaks);
        private static readonly Opening TwoPeakValue=Find(BoardLayout.TwoPeaks);
        public static Opening For(BoardLayout layout)=>layout.Count==19?TwoPeakValue:layout.Count==28?Value:Find(layout);
        public static int Seed=>Value.Seed;
        public static int Slot=>Value.Slot;
        private static Opening Find(BoardLayout layout)
        {
            for(int seed=1;seed<=2048;seed++)
            {
                var deal=WinnableBoardGenerator.Generate(RunController.BoardSeedFor(seed,1,1),layout);
                for(int slot=0;slot<layout.Count;slot++)
                {
                    var game=deal.CreateGame();if(!game.TryPlay(slot)||!game.HasNoMoves)continue;
                    if(game.TryDraw()&&!game.HasNoMoves)return new Opening(seed,slot);
                }
            }
            throw new InvalidOperationException("No first-match teaching deal found within the deterministic search bound.");
        }
    }
}
