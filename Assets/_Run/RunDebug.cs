namespace CursedDeck.Run
{
    public sealed partial class RunController
    {
        /// <summary>Development command callers are compiled out of release UI. A debug
        /// mutation always redeals, establishing an honest replay/save baseline.</summary>
        public void DebugRestart(int floor,int round,int? seedOverride=null,int grantGold=0,bool grantRelic=false)
        {
            if(floor<1 || floor>3 || round<1 || round>3 || grantGold<0) throw new System.ArgumentOutOfRangeException();
            State=RunSnapshot.From(State).Restore(catalog); State.Floor=floor; State.Round=round; State.Stage=RunStage.Playing;
            if(grantGold>0) State.AddGold(grantGold);
            if(grantRelic) for(int i=0;i<catalog.Length;i++) if(!State.Owns(catalog[i].Id)) { State.Acquire(catalog[i]); break; }
            roundStart=RunSnapshot.From(State); roundSeed=seedOverride ?? Derive(State.Seed,floor,round,0); BuildRound();
        }
    }
}
