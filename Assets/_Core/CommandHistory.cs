using System;
using System.Collections.Generic;

namespace CursedDeck.Core
{
    public enum CommandKind { Play, Draw, Reroll, Shuffle, Reward }
    public readonly struct RoundCommand
    {
        public CommandKind Kind { get; }
        public int Slot { get; }
        internal RoundCommand(CommandKind kind, int slot) { Kind = kind; Slot = slot; }
    }

    internal sealed class RoundMemento
    {
        internal object Effects;
        internal CommandKind? LastCommand;
        internal RuleEngine.Memento Board;
        internal decimal Score;
        internal int Draws, CardsPlayed;
        internal RoundPhase Phase;
        internal RoundOutcome Outcome;
    }

    /// <summary>Records only accepted commands. Undo consumes the most recent record.
    /// Snapshots allocate at command boundaries, never in a per-frame polling loop.</summary>
    public sealed class CommandHistory
    {
        private readonly List<RoundCommand> commands = new List<RoundCommand>(64);
        private readonly List<RoundMemento> before = new List<RoundMemento>(64);
        public int Count => commands.Count;
        public RoundCommand this[int index] => commands[index];

        internal void Record(RoundCommand command, RoundMemento snapshot)
        {
            commands.Add(command); before.Add(snapshot);
        }

        internal bool TryPop(out RoundMemento snapshot)
        {
            if (Count == 0) { snapshot = null; return false; }
            int index = Count - 1;
            snapshot = before[index];
            before.RemoveAt(index); commands.RemoveAt(index);
            return true;
        }
    }
}
