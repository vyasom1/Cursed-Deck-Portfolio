using System;
using System.Runtime.Serialization;

namespace CursedDeck.Run
{
    [DataContract]
    public sealed class RunConfig
    {
        [DataMember(Order=0)] public int StartingGold { get; private set; }
        [DataMember(Order=1)] public int DrawLimit { get; private set; }
        [DataMember(Order=2)] public int WinGold { get; private set; }
        [DataMember(Order=3)] public int GoldPerUnusedDraw { get; private set; }
        [DataMember(Order=4)] public int BaseTarget { get; private set; }
        [DataMember(Order=5)] public int FloorTargetStep { get; private set; }
        [DataMember(Order=6)] public int RoundTargetStep { get; private set; }
        [DataMember(Order=7)] public int ConsumablePriceMin { get; private set; }
        [DataMember(Order=8)] public int ConsumablePriceMax { get; private set; }
        [DataMember(Order=9)] public int RelicPriceMin { get; private set; }
        [DataMember(Order=10)] public int RelicPriceMax { get; private set; }
        [DataMember(Order=12)] public int FallbackRewardGold {get;private set;}
        [DataMember(Order=11)] public int DealerRulesVersion { get; private set; }
        public RunConfig(int startingGold=0, int drawLimit=23, int winGold=20, int goldPerUnusedDraw=2,
            int baseTarget=2000, int floorTargetStep=500, int roundTargetStep=250,
            int consumablePriceMin=15, int consumablePriceMax=25, int relicPriceMin=45, int relicPriceMax=65, int dealerRulesVersion=1,int fallbackRewardGold=25)
        {
            FallbackRewardGold=fallbackRewardGold;DealerRulesVersion=dealerRulesVersion; StartingGold=startingGold; DrawLimit=drawLimit; WinGold=winGold; GoldPerUnusedDraw=goldPerUnusedDraw;
            BaseTarget=baseTarget; FloorTargetStep=floorTargetStep; RoundTargetStep=roundTargetStep;
            ConsumablePriceMin=consumablePriceMin; ConsumablePriceMax=consumablePriceMax;
            RelicPriceMin=relicPriceMin; RelicPriceMax=relicPriceMax; Validate();
        }
        public void Validate()
        {
            if (FallbackRewardGold<0||FallbackRewardGold>100000||DealerRulesVersion<0 || DealerRulesVersion>1 || StartingGold<0 || DrawLimit<0 || DrawLimit>1000 || WinGold<0 || GoldPerUnusedDraw<0 ||
                BaseTarget<1 || FloorTargetStep<0 || RoundTargetStep<0 || ConsumablePriceMin<1 ||
                ConsumablePriceMax<ConsumablePriceMin || RelicPriceMin<1 || RelicPriceMax<RelicPriceMin ||
                RelicPriceMax>100000 || ConsumablePriceMax>100000)
                throw new ArgumentException("Invalid run configuration.");
            checked { int maximum=BaseTarget+2*FloorTargetStep+2*RoundTargetStep; int gold=WinGold+DrawLimit*GoldPerUnusedDraw; }
        }
        public decimal Target(int floor,int round) => checked(BaseTarget+(floor-1)*FloorTargetStep+(round-1)*RoundTargetStep);
        public int GoldAward(int unusedDraws) => checked(WinGold+Math.Min(DrawLimit,Math.Max(0,unusedDraws))*GoldPerUnusedDraw);
    }

    public enum RunStage { Playing, Reward, Shop, Won, Lost }
    public enum ConsumableKind { Undo, Peek, ShuffleDrawPile }
}
