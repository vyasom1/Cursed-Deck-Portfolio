using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
namespace CursedDeck.Meta
{
    public static class DailyRun
    {
        public static string DateKey(DateTime utc) { if(utc.Kind!=DateTimeKind.Utc) throw new ArgumentException("UTC required."); return utc.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture); }
        public static int Seed(DateTime utc) { string key="CursedDeck/daily/v1/"+DateKey(utc); uint hash=2166136261; unchecked { foreach(char c in key) hash=(hash^c)*16777619; } return unchecked((int)hash); }
        public static string Share(string date,int seed,decimal score,int rounds,bool won)
        {
            if(!DateTime.TryParseExact(date,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out _) || score<0 || rounds<0 || rounds>9) throw new ArgumentException("Invalid daily result.");
            var text=new StringBuilder("Cursed Deck Daily ").Append(date).Append(won ? " WIN\n" : " END\n");
            for(int i=0;i<9;i++) { text.Append(i<rounds ? "\U0001F7E8" : "\u2B1B"); if(i%3==2) text.Append('\n'); }
            return text.Append("Score ").Append(score.ToString("0.##",CultureInfo.InvariantCulture)).Append(" | ").Append(rounds).Append("/9\nSeed ").Append(seed.ToString(CultureInfo.InvariantCulture)).ToString();
        }
    }
    [DataContract] public sealed class MetaRules
    {
        [DataMember] public int DeckCost=20;
        [DataMember] public int RelicCost=10;
        [DataMember] public int BaseShards=2;
        [DataMember] public int RoundShards=5;
        [DataMember] public int WinShards=20;
        public void Validate() { if(DeckCost<0||RelicCost<0||BaseShards<0||RoundShards<0||WinShards<0) throw new ArgumentException("Invalid meta rules."); }
    }
    public static class MetaUnlocks
    {
        public static readonly string[] Ids={"red-deck","black-velvet","long-wick","low-stakes","high-stakes","first-light"};
        public static bool Known(string id) { for(int i=0;i<Ids.Length;i++) if(Ids[i]==id)return true; return false; }
    }
    [DataContract] public sealed class MetaSession
    {
        [DataMember] public string Id;
        [DataMember] public int Seed;
        [DataMember] public string Deck="classic";
        [DataMember] public string DailyDate;
        [DataMember] public long StartedUtc;
        [DataMember] public string RunCheckpoint;
        [DataMember] public decimal[] Scores=new decimal[9];
        [DataMember] public int[] Chains=new int[9];
        [DataMember] public bool[] RoundsWon=new bool[9];
        public void Observe(int floor,int round,decimal score,int chain,bool won)
        {
            int index=(floor-1)*3+round-1; if(index<0||index>=9||score<0||chain<0)throw new ArgumentException("Invalid run observation.");
            Scores[index]=score; Chains[index]=Math.Max(Chains[index],chain); RoundsWon[index]=won;
        }
        public decimal Score {get {decimal total=0;foreach(decimal v in Scores)total+=v;return total;}}
        public int LongestChain {get {int best=0;foreach(int v in Chains)best=Math.Max(best,v);return best;}}
        public int WonRounds {get {int total=0;foreach(bool v in RoundsWon)if(v)total++;return total;}}
        public void Validate() { if(string.IsNullOrWhiteSpace(Id)||Scores==null||Scores.Length!=9||Chains==null||Chains.Length!=9||RoundsWon==null||RoundsWon.Length!=9||StartedUtc<=0||StartedUtc>DateTime.MaxValue.Ticks||(Deck!="classic"&&Deck!="red-deck"))throw new SerializationException("Invalid session.");for(int i=0;i<9;i++)if(Scores[i]<0||Chains[i]<0)throw new SerializationException("Invalid metrics.");if(DailyDate!=null)DailyRun.Share(DailyDate,Seed,Score,WonRounds,false); }
    }
    [DataContract] public sealed class MetaProfile
    {
        [DataMember] public int Version=1;
        [DataMember] public int SoulShards {get;private set;}
        [DataMember] public decimal BestScore {get;private set;}
        [DataMember] public int Wins {get;private set;}
        [DataMember] public long FastestRunSeconds {get;private set;}
        [DataMember] public int LongestChain {get;private set;}
        [DataMember] public int DailyStreak {get;private set;}
        [DataMember] public string LastDailyWin {get;private set;}
        [DataMember] public string LastShare {get;private set;}
        [DataMember] private List<string> unlocked=new List<string>();
        [DataMember] private List<string> receipts=new List<string>();
        [DataMember] public MetaSession Active;
        [DataMember] public bool TutorialDone;
        [DataMember] public GameSettings Settings;
        public bool IsUnlocked(string id)=>id=="classic"||unlocked.Contains(id);
        public bool TryUnlock(string id,MetaRules rules)
        {
            rules.Validate();if(!MetaUnlocks.Known(id)||IsUnlocked(id))return false;int cost=id=="red-deck"?rules.DeckCost:rules.RelicCost;
            if(SoulShards<cost)return false;SoulShards-=cost;unlocked.Add(id);return true;
        }
        public void Begin(int seed,string deck,string date,DateTime utc)
        {
            DailyRun.DateKey(utc);if(!IsUnlocked(deck)||(date!=null&&(deck!="classic"||date!=DailyRun.DateKey(utc)||seed!=DailyRun.Seed(utc))))throw new InvalidOperationException("Starting deck or daily challenge is gated.");
            Active=new MetaSession{Id=Guid.NewGuid().ToString("N"),Seed=seed,Deck=deck,DailyDate=date,StartedUtc=utc.Ticks};
        }
        public bool Finish(bool won,DateTime utc,MetaRules rules)
        {
            rules.Validate();DailyRun.DateKey(utc);if(Active==null||receipts.Contains(Active.Id))return false;Active.Validate();
            if(won && Active.WonRounds!=9)throw new InvalidOperationException("A run win requires nine won rounds.");
            var session=Active;int award=checked(rules.BaseShards+rules.RoundShards*session.WonRounds+(won?rules.WinShards:0));
            int balance=checked(SoulShards+award);long elapsed=Math.Max(1,(utc.Ticks-session.StartedUtc)/TimeSpan.TicksPerSecond);
            BestScore=Math.Max(BestScore,session.Score);LongestChain=Math.Max(LongestChain,session.LongestChain);
            if(won){Wins++;if(FastestRunSeconds==0||elapsed<FastestRunSeconds)FastestRunSeconds=elapsed;}
            if(session.DailyDate!=null)
            {
                LastShare=DailyRun.Share(session.DailyDate,session.Seed,session.Score,session.WonRounds,won);
                if(won&&(LastDailyWin==null||StringComparer.Ordinal.Compare(session.DailyDate,LastDailyWin)>0))
                {DailyStreak=LastDailyWin==DateTime.ParseExact(session.DailyDate,"yyyy-MM-dd",CultureInfo.InvariantCulture).AddDays(-1).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)?DailyStreak+1:1;LastDailyWin=session.DailyDate;}
            }
            SoulShards=balance;receipts.Add(session.Id);return true;
        }
        public void Validate()
        {
            if(Version!=1||SoulShards<0||BestScore<0||Wins<0||FastestRunSeconds<0||LongestChain<0||DailyStreak<0||unlocked==null||receipts==null)throw new SerializationException("Unsupported or invalid profile; preserve original.");
            var ids=new HashSet<string>();foreach(string id in unlocked)if(!MetaUnlocks.Known(id)||!ids.Add(id))throw new SerializationException("Invalid unlock.");ids.Clear();foreach(string id in receipts)if(string.IsNullOrEmpty(id)||!ids.Add(id))throw new SerializationException("Invalid receipt.");Active?.Validate();
        }
    }
}
