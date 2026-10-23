using CursedDeck.Run;
using UnityEngine;

namespace CursedDeck.Data
{
    [CreateAssetMenu(menuName="Cursed Deck/Run Rules")]
    public sealed class RunConfigDefinition : ScriptableObject
    {
        [SerializeField] private int startingGold=0;
        [SerializeField,Min(0)] private int fallbackRewardGold=25;
        [SerializeField] private int drawLimit=23;
        [SerializeField] private int winGold=20;
        [SerializeField] private int goldPerUnusedDraw=2;
        [SerializeField] private int baseTarget=2000;
        [SerializeField] private int floorTargetStep=500;
        [SerializeField] private int roundTargetStep=250;
        [SerializeField] private int consumableMin=15,consumableMax=25,relicMin=45,relicMax=65;
        public RunConfig CreateConfig() => new RunConfig(startingGold,drawLimit,winGold,goldPerUnusedDraw,
            baseTarget,floorTargetStep,roundTargetStep,consumableMin,consumableMax,relicMin,relicMax,fallbackRewardGold:fallbackRewardGold);
    }
}
