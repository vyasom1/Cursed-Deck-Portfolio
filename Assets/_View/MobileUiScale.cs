using UnityEngine;
namespace CursedDeck.View
{
    public static class MobileUiScale
    {
        /// <summary>Query once at board initialization. Android dp uses logical density, not physical Screen.dpi.</summary>
        public static float ReadPixelsPerDp()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
                using(var resources=activity.Call<AndroidJavaObject>("getResources"))
                using(var metrics=resources.Call<AndroidJavaObject>("getDisplayMetrics"))
                {
                    float density=metrics.Get<float>("density");
                    if(density>0&&!float.IsNaN(density)&&!float.IsInfinity(density))return density;
                }
            }
            catch(AndroidJavaException error){Debug.LogWarning("Logical Android density unavailable; card sizing uses estimated DPI. "+error.Message);}
#endif
            return Screen.dpi>0?Screen.dpi/160f:0;
        }
    }
}
