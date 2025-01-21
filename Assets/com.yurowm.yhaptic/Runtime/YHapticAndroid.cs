using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Yurowm.Integrations {
    public class YHapticAndroid: IYHapticProvider {

        #if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject vibrator;     
        AndroidJavaClass vibrationEffectClass; 
        #endif
        
        bool isVibrationEffectSupported = false;
        
        public async UniTask Initialize() {
            #if UNITY_ANDROID && !UNITY_EDITOR
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")) {
                var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }

            try {
                vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                isVibrationEffectSupported = true;
            } catch {
                isVibrationEffectSupported = false;
            }
            #endif
        }

        public bool IsSupported() => isVibrationEffectSupported;

        public void Play(long[] pattern, int[] amplitudes) {
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (vibrator != null && isVibrationEffectSupported) {
                var vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                    "createWaveform", pattern, amplitudes, -1);
                vibrator.Call("vibrate", vibrationEffect);
            }
            #endif
        }
    }
}