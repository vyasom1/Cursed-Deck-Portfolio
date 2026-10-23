using System;
namespace CursedDeck.Run
{
    public sealed partial class EventBus
    {
        // Presentation request only: no Run command, progression or state mutation.
        public event Action<int> CardInspectionRequested;
        public void RequestCardInspection(int slot)=>CardInspectionRequested?.Invoke(slot);
    }
}
