using System;
using CursedDeck.Run;
using UnityEngine;

namespace CursedDeck.Data
{
    [CreateAssetMenu(menuName = "Cursed Deck/Relic")]
    public sealed class RelicDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private Sprite relicIcon;
        public Sprite Icon=>relicIcon;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private RelicKind kind;
        [SerializeField] private int order;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField, Min(1)] private int interval = 1;
        [SerializeField, Min(0.01f)] private float multiplier = 1;
        public string DisplayName => displayName;
        public string Description => description;
        public RelicSpec CreateSpec()
        {
            if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) throw new InvalidOperationException("Invalid multiplier.");
            return new RelicSpec(id, kind, order, amount, interval, (decimal)multiplier);
        }
    }
}
