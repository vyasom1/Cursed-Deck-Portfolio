using System;
using CursedDeck.Core;
using UnityEngine;

namespace CursedDeck.Data
{
    [CreateAssetMenu(menuName = "Cursed Deck/Board Layout")]
    public sealed class BoardLayoutDefinition : ScriptableObject
    {
        [Serializable]
        private sealed class SlotDefinition
        {
            public int x;
            public int y;
            public int[] covering;
        }
        [SerializeField] private SlotDefinition[] slots;
        public BoardLayout CreateLayout()
        {
            if (slots == null) throw new InvalidOperationException("Layout slots are missing.");
            var result = new BoardSlot[slots.Length];
            for (int i = 0; i < result.Length; i++)
            {
                if (slots[i] == null) throw new InvalidOperationException("Null layout slot.");
                result[i] = new BoardSlot(slots[i].x, slots[i].y, slots[i].covering ?? new int[0]);
            }
            return new BoardLayout(result);
        }
    }
}
