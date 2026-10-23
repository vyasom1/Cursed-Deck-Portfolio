using CursedDeck.Meta;
using UnityEngine;
namespace CursedDeck.Data
{
    [CreateAssetMenu(menuName="Cursed Deck/Meta Rules")]
    public sealed class MetaDefinition:ScriptableObject
    {
        [SerializeField] private int deckCost=20,relicCost=10,baseShards=2,roundShards=5,winShards=20;
        [SerializeField] private RelicDefinition[] unlockRelics;
        public RelicDefinition[] UnlockRelics=>unlockRelics;
        public MetaRules Rules()=>new MetaRules{DeckCost=deckCost,RelicCost=relicCost,BaseShards=baseShards,RoundShards=roundShards,WinShards=winShards};
    }
}
