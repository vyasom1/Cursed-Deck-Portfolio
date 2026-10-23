using CursedDeck.Core;

namespace CursedDeck.Run
{
    /// <summary>Interpreter of authored mechanics. Its Frost Pocket flag participates in undo mementos;
    /// shared ScriptableObjects hold configuration only.</summary>
    public sealed class ConfiguredRelic : RelicBase, IRelicRules
    {
        private bool firstDrawUsed;
        public ConfiguredRelic(RelicSpec definition) : base(definition) { }
        public override void OnRoundStart(RelicContext context){firstDrawUsed=false;if(Definition.Kind==RelicKind.StillHour)context.Round.GrantDraws(Definition.Amount);}
        public override void OnDraw(RelicContext context,bool isReroll){if(Definition.Kind==RelicKind.FrostPocket&&!isReroll&&!firstDrawUsed){firstDrawUsed=true;context.Round.GrantDraws(Definition.Amount);}}
        public override object CaptureState()=>Definition.Kind==RelicKind.FrostPocket?(object)firstDrawUsed:null;
        public override void RestoreState(object state){if(Definition.Kind==RelicKind.FrostPocket)firstDrawUsed=state is bool used&&used;}


        public override void OnCardPlayed(RelicContext context, CardModel card, CardModel previousWaste)
        {
            if(Definition.Kind==RelicKind.SoulLedger && (int)card.Rank>=11)context.Run.AddGold(Definition.Amount);
            if (Definition.Kind == RelicKind.Metronome && context.Round.Chain % Definition.Interval == 0)
                context.Round.GrantDraws(Definition.Amount);
        }

        public override decimal OnScore(RelicContext context, ScoringStage stage, ScoreContext score, decimal value)
        {
            if(stage==ScoringStage.FlatBonuses&&Definition.Kind==RelicKind.BoneAbacus&&score.Card.Color==CardColor.Black)return value+Definition.Amount;
            if (stage==ScoringStage.FlatBonuses && Definition.Kind==RelicKind.FirstLight && context.Round.State.CardsPlayed<=Definition.Interval) return value+Definition.Amount;
            if (stage != ScoringStage.RelicMultipliers) return value;
            if(Definition.Kind==RelicKind.BoneCrown&&score.Card.Rank==Rank.Ace)return value*Definition.Multiplier;
            if (Definition.Kind==RelicKind.BlackVelvet && score.Card.Color==CardColor.Black) return value*Definition.Multiplier;
            if (Definition.Kind==RelicKind.LongWick && score.Chain>=Definition.Interval) return value*Definition.Multiplier;
            if (Definition.Kind==RelicKind.LowStakes && (int)score.Card.Rank>=2 && (int)score.Card.Rank<=6) return value*Definition.Multiplier;
            if (Definition.Kind==RelicKind.HighStakes && (int)score.Card.Rank>=11) return value*Definition.Multiplier;
            if (Definition.Kind == RelicKind.RedVelvet && score.Card.Color == CardColor.Red) return value * Definition.Multiplier;
            if (Definition.Kind == RelicKind.CrackedMirror && context.Round.State.CardsPlayed == 1) return value * Definition.Multiplier;
            return value;
        }

        public bool AllowsMove(RelicContext context, CardModel card, CardModel waste)
        {
            switch (Definition.Kind)
            {
                case RelicKind.GraveCompass:return context.Round!=null&&context.Round.Chain>=Definition.Interval&&card.Rank==Rank.Ace;
                case RelicKind.VoidStep:int diff=System.Math.Abs((int)card.Rank-(int)waste.Rank);return context.Round!=null&&(context.Round.State.CardsPlayed+1)%Definition.Interval==0&&(diff==2||diff==11);
                case RelicKind.LoadedDie: return card.Rank == Rank.King || waste.Rank == Rank.King;
                case RelicKind.SuitBridge: return context.AfterDraw && card.Suit == waste.Suit;
                case RelicKind.ColorEcho: return context.Round != null &&
                    (context.Round.State.CardsPlayed + 1) % Definition.Interval == 0 && card.Color == waste.Color;
                default: return false;
            }
        }

        public int ReservedStockCards(RelicContext context) =>
            Definition.Kind == RelicKind.LastBreath && !context.Run.LastBreathUsed ? 1 : 0;

        public bool CanReroll(RelicContext context) => Definition.Kind == RelicKind.GamblersRing &&
            context.Run.Gold >= Definition.Amount && context.Round.HasNoMoves &&
            (context.Round.State.LastCommand == CommandKind.Draw || context.Round.State.LastCommand == CommandKind.Reroll);

        public bool PayForReroll(RelicContext context) => CanReroll(context) && context.Run.TrySpendGold(Definition.Amount);

        public bool TryPreventDefeat(RelicContext context)
        {
            if (Definition.Kind != RelicKind.LastBreath || context.Round.StockRemaining == 0 || !context.Run.TryUseLastBreath()) return false;
            context.Round.SetRescueDrawAllowance(1);
            return true;
        }
    }
}
