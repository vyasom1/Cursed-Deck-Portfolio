using System;
using UnityEngine;

namespace CursedDeck.Services
{
    public interface IHapticOutput { void Light(); void Heavy(); void Dispose(); }

    /// <summary>One cached native backend per session. Effects never decide gameplay outcomes.</summary>
    public static class HapticService
    {
        public const long LightMilliseconds=30,HeavyMilliseconds=80;
        private static IHapticOutput output;
        public static bool Enabled {get;set;}=true;
        public static void TriggerLightImpact(){if(Enabled){Prepare();output.Light();}}
        public static void TriggerHeavyImpact(){if(Enabled){Prepare();output.Heavy();}}
        public static void Prepare()
        {
            if(output!=null)return;
            output=new DeviceOutput();Application.quitting-=Shutdown;Application.quitting+=Shutdown;
        }
        public static void Shutdown(){output?.Dispose();output=null;Application.quitting-=Shutdown;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession(){Shutdown();Enabled=true;}
#if UNITY_EDITOR
        public static void SetOutputForTests(IHapticOutput value){Shutdown();output=value;}
#endif
        private sealed class DeviceOutput:IHapticOutput
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            private AndroidJavaObject vibrator,light,heavy;
            private bool supported;
            public DeviceOutput()
            {
                try
                {
                    using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
                    {if(activity!=null)vibrator=activity.Call<AndroidJavaObject>("getSystemService","vibrator");}
                    supported=vibrator!=null&&vibrator.Call<bool>("hasVibrator");
                    if(!supported)return;
                    using(var build=new AndroidJavaClass("android.os.Build$VERSION"))
                    if(build.GetStatic<int>("SDK_INT")>=26)
                    using(var effect=new AndroidJavaClass("android.os.VibrationEffect"))
                    {light=effect.CallStatic<AndroidJavaObject>("createOneShot",LightMilliseconds,-1);heavy=effect.CallStatic<AndroidJavaObject>("createOneShot",HeavyMilliseconds,-1);}
                }
                catch(AndroidJavaException ex){supported=false;Debug.LogWarning("Haptics unavailable: "+ex.Message);}
            }
            private void Vibrate(AndroidJavaObject effect,long milliseconds)
            {
                if(!supported)return;
                try{if(effect!=null)vibrator.Call("vibrate",effect);else vibrator.Call("vibrate",milliseconds);}
                catch(AndroidJavaException ex){supported=false;Debug.LogWarning("Haptics disabled: "+ex.Message);}
            }
            public void Light()=>Vibrate(light,LightMilliseconds);
            public void Heavy()=>Vibrate(heavy,HeavyMilliseconds);
            public void Dispose(){light?.Dispose();heavy?.Dispose();vibrator?.Dispose();light=heavy=vibrator=null;supported=false;}
#else
            public void Light(){
#if UNITY_IOS && !UNITY_EDITOR
                Handheld.Vibrate();
#endif
            }
            public void Heavy(){
#if UNITY_IOS && !UNITY_EDITOR
                Handheld.Vibrate();
#endif
            }
            public void Dispose(){}
#endif
        }
    }
}
