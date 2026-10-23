using TMPro;
using UnityEngine;
namespace CursedDeck.View
{
    public static class CardReadability
    {
        private static Material outline;
        public static void Rank(TextMeshProUGUI label)
        {
            label.fontSize=36;label.enableAutoSizing=false;label.fontStyle=FontStyles.Bold;
            if(outline==null)outline=Resources.Load<Material>("TCG/RankOutline");
            if(outline!=null)label.fontSharedMaterial=outline;
            label.extraPadding=true;label.overflowMode=TextOverflowModes.Overflow;
        }
    }
}
