using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using CursedDeck.Run;
using CursedDeck.Data;
using UnityEditor;
using UnityEngine;
namespace CursedDeck.Bootstrap.Editor
{
    public static class RunBalanceStudy
    {
        [MenuItem("Tools/Cursed Deck/Run 1000 Portfolio Simulations")]
        public static void Build()
        {
            var catalog=Resources.Load<GameContent>("CursedDeck/Content").CreateCatalog();
            if(catalog.Length!=15)throw new InvalidOperationException("Prepare the 15-relic base pool first.");
            var samples=new List<FullRunSample>(1000);
            var runs=new StringBuilder("seed,won,total_score,playing_attempts,blind_misses,owned_relics\n");
            var rounds=new StringBuilder("seed,floor,round,dealer,won,score,net_draw_allowances,peak_chain\n");
            var offered=catalog.ToDictionary(r=>r.Id,r=>0);var picked=catalog.ToDictionary(r=>r.Id,r=>0);
            var pairs=new Dictionary<string,(int Count,int Wins)>(StringComparer.Ordinal);
            for(int seed=1;seed<=1000;seed++)
            {
                var sample=BalanceSimulator.RunFull(seed,catalog);samples.Add(sample);
                runs.AppendLine(seed+","+(sample.Won?1:0)+","+N(sample.Score)+","+sample.Steps+","+sample.BlindMisses+","+string.Join("|",sample.Owned));
                for(int i=0;i<9;i++)if(sample.Reached[i])rounds.AppendLine(seed+","+(i/3+1)+","+(i%3+1)+","+(i%3==2?Dealer(i/3):"None")+","+(sample.RoundWins[i]?1:0)+","+N(sample.RoundScores[i])+","+sample.RoundDraws[i]+","+sample.RoundChains[i]);
                foreach(string id in sample.Offered)offered[id]++;foreach(string id in sample.Picked)picked[id]++;
                for(int a=0;a<sample.Owned.Length;a++)for(int b=a+1;b<sample.Owned.Length;b++)
                {string key=sample.Owned[a]+"|"+sample.Owned[b];pairs.TryGetValue(key,out var count);pairs[key]=(count.Count+1,count.Wins+(sample.Won?1:0));}
            }
            int wins=samples.Count(s=>s.Won),misses=samples.Sum(s=>s.BlindMisses);
            var report=new StringBuilder("# Portfolio balancing study — real dealers\n\n");
            report.AppendLine("**1,000 complete run attempts**, seeds 1–1000, with the real Apprentice, Clockmaker and Mirror on each floor's third round. A run stops on defeat or after nine won rounds. This replaces the older STUB report; the historical 5,400 isolated-round CSVs remain archival, not current boss evidence.\n");
            report.AppendLine("Fresh Classic runs, 15 base relics, zero starting gold, no pre-acquired relics or unlock bonuses. Rewards and shop purchases are acquired organically. The bot buys affordable relics by its documented preference; it does not buy/use consumables. Current targets, scoring, gold, prices and existing relic parameters were retained. **No balance changes from this study were applied.**\n");
            report.AppendLine("Heuristic: prioritize uncovering cards and visible adjacent-rank continuations; seeded reservoir ties; draw when no selected play is available, then paid reroll if available. Mirror guesses select an untried exposed card without reading its rank or legality. Rejected guesses provide normal game feedback and cost nothing under current rules; guesses reset after accepted actions. No generator witness or hidden rank is read.\n");
            report.AppendLine("Reward/shop preference is fixed: Loaded Die > Red Velvet > Suit Bridge/Grave Compass > Color Echo/Void Step > Last Breath > Metronome > Cracked Mirror/Still Hour/Frost Pocket > Bone Abacus/Bone Crown > Soul Ledger > other kinds. Pick rates measure this bot's policy, not player desirability. Shop spending can leave little gold for Gambler's Ring.\n");
            report.AppendLine("| Metric | Result |\n|---|---:|\n| Full runs won | "+wins+" / 1000 |\n| Full-run win rate | "+P(wins,1000)+" |\n| Average total score, including failed runs | "+N(samples.Average(s=>s.Score))+" |\n| Average playing attempts, including rejected guesses | "+samples.Average(s=>s.Steps).ToString("0.0",CultureInfo.InvariantCulture)+" |\n| Mirror rejected guesses | "+misses+" |\n");
            report.AppendLine("## Conditional boss results\n\nLater bosses have fewer samples because earlier losses stop a run. Reached/won counts are observed progression, not 1,000 isolated attempts per boss. Net draw allowances exclude Clockmaker's initial tax and may be negative after refunds.\n\n| Dealer | Reached | Won | Conditional win rate | Average score |\n|---|---:|---:|---:|---:|");
            for(int floor=0;floor<3;floor++){int index=floor*3+2;var reached=samples.Where(s=>s.Reached[index]).ToArray();int won=reached.Count(s=>s.RoundWins[index]);report.AppendLine("| "+Dealer(floor)+" | "+reached.Length+" | "+won+" | "+P(won,reached.Length)+" | "+(reached.Length==0?"n/a":N(reached.Average(s=>s.RoundScores[index])))+" |");}
            var pickCsv=new StringBuilder("relic,offered,picked,pick_rate\n");report.AppendLine("\n## Reward pick rates\n\n| Relic | Offered | Picked | Pick rate |\n|---|---:|---:|---:|");
            foreach(var relic in catalog.OrderBy(r=>r.Id,StringComparer.Ordinal)){pickCsv.AppendLine(relic.Id+","+offered[relic.Id]+","+picked[relic.Id]+","+P(picked[relic.Id],offered[relic.Id]));report.AppendLine("| "+relic.Id+" | "+offered[relic.Id]+" | "+picked[relic.Id]+" | "+P(picked[relic.Id],offered[relic.Id])+" |");}
            var pairCsv=new StringBuilder("pair,owned_runs,wins,conditional_win_rate\n");report.AppendLine("\n## Co-occurrence combinations to review\n\nExploratory review band: 40–85% completed-run win rate among runs owning the pair, minimum 30 observed runs. Ownership has strong survivorship and acquisition bias; these are correlations, not causal relic power estimates. Controlled matched-seed comparisons are required before tuning.\n\n| Pair | Observed runs | Wins | Conditional win rate |\n|---|---:|---:|---:|");
            foreach(var pair in pairs.OrderBy(p=>p.Key,StringComparer.Ordinal)){pairCsv.AppendLine(pair.Key+","+pair.Value.Count+","+pair.Value.Wins+","+P(pair.Value.Wins,pair.Value.Count));double rate=(double)pair.Value.Wins/pair.Value.Count;if(pair.Value.Count>=30&&(rate<.40||rate>.85))report.AppendLine("| "+pair.Key.Replace("|"," + ")+" | "+pair.Value.Count+" | "+pair.Value.Wins+" | "+P(pair.Value.Wins,pair.Value.Count)+" |");}
            report.AppendLine("\n## Proposed follow-ups — not applied\n\n1. Mirror currently permits unlimited free invalid guesses. Test a miss cost or a bounded-attempt variant before claiming it creates sustained difficulty; either changes rules and requires approval.\n2. Run matched-seed, fixed-acquisition ablations for high/low co-occurrence pairs. The current ownership table cannot establish a causal combo effect.\n3. Review the first-round 2,000-point threshold: ten uninterrupted base plays already exceed it. Compare session length and target clarity with real players before changing targets.\n4. Compare alternate shop policies, including reserving gold for rerolls; this bot favors buying relics.\n5. Validate human first-minute completion, blind-card comprehension and phone performance. Bot results provide no retention, device-FPS or human win-rate evidence.\n");
            Directory.CreateDirectory("Docs/Balance");File.WriteAllText("Docs/Balance/portfolio-runs.csv",runs.ToString());File.WriteAllText("Docs/Balance/portfolio-rounds.csv",rounds.ToString());File.WriteAllText("Docs/Balance/portfolio-relic-picks.csv",pickCsv.ToString());File.WriteAllText("Docs/Balance/portfolio-pairs.csv",pairCsv.ToString());File.WriteAllText("BALANCING.md",report.ToString());
            Debug.Log("CURSED_DECK_PORTFOLIO_BALANCE_READY runs=1000 wins="+wins+" blind_misses="+misses+" NO_TUNING_APPLIED");
        }
        private static string Dealer(int floor)=>floor==0?"Apprentice":floor==1?"Clockmaker":"Mirror";
        private static string N(decimal number)=>number.ToString("0.##",CultureInfo.InvariantCulture);
        private static string P(int wins,int count)=>count==0?"n/a":((double)wins/count*100).ToString("0.0",CultureInfo.InvariantCulture)+"%";
    }
}
