using UnityEngine;
namespace CursedDeck.View
{
    public static class BoardGeometry
    {
        public const float ReferenceCardWidth=145,MinimumTouchDp=50;
        public static float RowStep(float cardHeight,float boardHeight,int rows)=>rows<=0?0:Mathf.Min(cardHeight*.83f,(boardHeight-cardHeight-24)/rows);
        public static float CardWidth(float width,float height,float horizontalSteps,int rows,bool twoPeaks,float canvasScale,float pixelsPerDp)
        {
            float desired=twoPeaks?Mathf.Max(ReferenceCardWidth,MinimumTouchDp*pixelsPerDp/Mathf.Max(.001f,canvasScale)):float.MaxValue;
            return Mathf.Min(desired,width/(1+horizontalSteps),(height-24)/(1.4f*(1+rows*.65f)));
        }
    }
}
