using System;
using System.Collections.Generic;
using CursedDeck.Core;

namespace CursedDeck.Run
{
    /// <summary>One manager per round; RunState survives rounds. All hooks iterate in
    /// ascending Order then ordinal ID, never acquisition order or dictionary order.</summary>
    public sealed class RelicManager : IRoundEffects
    {
        private readonly IRelic[] relics;
        private readonly RelicContext context;
        public int Count => relics.Length;
        public bool IsActive => ReferenceEquals(context.Run.ActiveManager, this);

        public RelicManager(RunState run) : this(run, MakeRelics(run)) { }
        public RelicManager(RunState run, IReadOnlyList<IRelic> source)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (source == null) throw new ArgumentNullException(nameof(source));
            context = new RelicContext(run);
            relics = new IRelic[source.Count];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] == null || !seen.Add(source[i].Definition.Id)) throw new ArgumentException("Null/duplicate relic.");
                relics[i] = source[i];
            }
            Array.Sort(relics, (a, b) => {
                int order = a.Definition.Order.CompareTo(b.Definition.Order);
                return order != 0 ? order : StringComparer.Ordinal.Compare(a.Definition.Id, b.Definition.Id);
            });
        }

        private static IRelic[] MakeRelics(RunState run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            var result = new IRelic[run.Relics.Count];
            for (int i = 0; i < result.Length; i++) result[i] = new ConfiguredRelic(run.Relics[i]);
            return result;
        }

        public void OnRoundStart(RoundSession round)
        {
            if (context.Round != null && context.Round != round) throw new InvalidOperationException("Create a new manager for each round.");
            var active = context.Run.ActiveManager;
            if (active != null && active != this && active.context.Round.State.Phase != RoundPhase.End)
                throw new InvalidOperationException("Finish the active round before starting another.");
            context.Run.ActiveManager = this;
            context.Round = round; context.AfterDraw = false;
            for (int i = 0; i < relics.Length; i++) relics[i].OnRoundStart(context);
        }
        public void OnCardPlayed(RoundSession round, CardModel card, CardModel previousWaste)
        {
            for (int i = 0; i < relics.Length; i++) relics[i].OnCardPlayed(context, card, previousWaste);
            context.AfterDraw = false;
        }
        public void OnDraw(RoundSession round, bool isReroll)
        {
            context.AfterDraw = true;
            for (int i = 0; i < relics.Length; i++) relics[i].OnDraw(context, isReroll);
        }
        public decimal Apply(ScoringStage stage, ScoreContext score, decimal value)
        {
            for (int i = 0; i < relics.Length; i++) value = relics[i].OnScore(context, stage, score, value);
            return value;
        }
        public void OnRoundEnd(RoundSession round, RoundOutcome outcome)
        {
            for (int i = 0; i < relics.Length; i++) relics[i].OnRoundEnd(context, outcome);
        }
        public bool AllowsMove(CardModel card, CardModel waste)
        {
            for (int i = 0; i < relics.Length; i++)
                if (relics[i] is IRelicRules rules && rules.AllowsMove(context, card, waste)) return true;
            return false;
        }
        public int ReservedStockCards
        {
            get
            {
                int reserved = 0;
                for (int i = 0; i < relics.Length; i++)
                    if (relics[i] is IRelicRules rules) reserved = Math.Max(reserved, rules.ReservedStockCards(context));
                return reserved;
            }
        }
        public bool CanReroll(RoundSession round)
        {
            for (int i = 0; i < relics.Length; i++)
                if (relics[i] is IRelicRules rules && rules.CanReroll(context)) return true;
            return false;
        }
        public bool PayForReroll(RoundSession round)
        {
            for (int i = 0; i < relics.Length; i++)
                if (relics[i] is IRelicRules rules && rules.PayForReroll(context)) return true;
            return false;
        }
        public bool TryPreventDefeat(RoundSession round)
        {
            for (int i = 0; i < relics.Length; i++)
                if (relics[i] is IRelicRules rules && rules.TryPreventDefeat(context)) return true;
            return false;
        }

        private sealed class Memento
        {
            internal RunState.Memento Run;
            internal RoundSession Round;
            internal bool AfterDraw;
            internal object[] Relics;
        }
        public object CaptureState()
        {
            var states = new object[relics.Length];
            for (int i = 0; i < states.Length; i++) states[i] = relics[i].CaptureState();
            return new Memento {Run = context.Run.Capture(), Round = context.Round, AfterDraw = context.AfterDraw, Relics = states};
        }
        public void RestoreState(object state)
        {
            var saved = (Memento)state;
            context.Run.Restore(saved.Run); context.Round = saved.Round; context.AfterDraw = saved.AfterDraw;
            for (int i = 0; i < relics.Length; i++) relics[i].RestoreState(saved.Relics[i]);
        }
    }
}
