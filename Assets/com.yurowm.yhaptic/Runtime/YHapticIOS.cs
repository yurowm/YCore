using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Yurowm.Core;
using Yurowm.DebugTools;
using Yurowm.Extensions;

namespace Yurowm.Integrations {
    public class YHapticIOS: IYHapticProvider {
        #if UNITY_IOS
        [DllImport("__Internal")] 
        static extern bool isHapticSupported();

        [DllImport("__Internal")] 
        static extern void playAHAP(string fileName);

        [DllImport("__Internal")]
        static extern void playHaptic(int type);

        [DllImport("__Internal")]
        static extern void playContinuousHaptic(float intensity, float sharpness, double duration);
        
        [DllImport("__Internal")]
        static extern void updateContinuousHaptic(float intensity, float sharpness);

        [DllImport("__Internal")]
        static extern void stopContinuousHaptic();

        [DllImport("__Internal")]
        static extern void setAppState(bool active);
        #endif
        
        bool? isSupported;

        public async UniTask Initialize() {
            foreach (HapticType haptic in Enum.GetValues(typeof(HapticType))) 
                DebugPanel.Log(haptic.ToString(), "yHaptic", () => Play(haptic));
                
            var i = .5f; 
            var s = .5f;
            var d = 2d;
            
            DebugPanel.Log("Cont.I", "yHaptic", new DebugVariable<float>(
                getter: () => i,
                setter: v => i = v.Clamp01()));
            
            DebugPanel.Log("Cont.S", "yHaptic", new DebugVariable<float>(
                getter: () => s,
                setter: v => s = v.Clamp01()));
            
            DebugPanel.Log("Cont.D", "yHaptic", new DebugVariable<float>(
                getter: () => (float) d,
                setter: v => d = v.ClampMin(0)));
            
            DebugPanel.Log("Cont.Play", "yHaptic", () => {
                StopContinuous();
                PlayContinuous(i, s, d);
            });
            
            App.onFocus += () => OnFocus(true);
            App.onUnfocus += () => OnFocus(false);
        }

        public bool IsSupported() {
            #if UNITY_IOS
            isSupported ??= isHapticSupported();
            return isSupported.Value;
            #else
            return false;
            #endif
        }

        void OnFocus(bool focus) {
            #if UNITY_IOS && !UNITY_EDITOR
            setAppState(focus);
            #endif
        }
        
        public void Play(string ahapFileName) {
            #if UNITY_IOS
            playAHAP(ahapFileName);
            #endif
        }
        
        public void Play(HapticType type) {
            #if UNITY_IOS
            playHaptic((int)type);
            #endif
        }
        
        public void PlayContinuous(float intensity, float sharpness, double duration) {
            #if UNITY_IOS
            DebugPanel.Log("Continues", "yHaptic", $"P {intensity:F2}/{sharpness:F2}");
            playContinuousHaptic(intensity, sharpness, duration);
            #endif
        }

        public void UpdateContinuous(float intensity, float sharpness) {
            #if UNITY_IOS
            DebugPanel.Log("Continues", "yHaptic", $"U {intensity:F2}/{sharpness:F2}");
            updateContinuousHaptic(intensity, sharpness);
            #endif
        }

        public void StopContinuous() {
            #if UNITY_IOS
            DebugPanel.Log("Continues", "yHaptic", "S 0.00/0.00");
            stopContinuousHaptic();
            #endif
        }
        
        List<ContinuesHaptic> continues = new();
        bool continuesPlaying = false;

        public IDisposable PlayContinues(float intensity, float sharpness) {
            #if UNITY_IOS
            if (intensity <= 0)
                return null;
            
            var result = new ContinuesHaptic(intensity.Clamp01(), sharpness.Clamp01(),
                Guid.NewGuid().ToString());
            
            continues.Add(result);
            
            UpdateContinues();
            
            return result;
            #else
            return null;
            #endif
        }
        
        void UpdateContinues() {
            if (continues.IsEmpty() && !continuesPlaying)
                return;
            
            if (continues.IsEmpty()) {
                StopContinuous();
                continuesPlaying = false;
                return;
            }
            
            var intensity = continues.Max(h => h.intensity);
            var sharpness = continues.Max(h => h.sharpness);
            
            PlayContinuous(intensity, sharpness, 120);
            continuesPlaying = true;
        }
        
        void Stop(ContinuesHaptic haptic) {
            continues.RemoveAll(h => h.ID == haptic.ID);
            UpdateContinues();
        }
        
        struct ContinuesHaptic: IDisposable {
            public float intensity;
            public float sharpness;
            public readonly string ID;
            
            public ContinuesHaptic(float intensity, float sharpness, string ID) {
                this.intensity = intensity;
                this.sharpness = sharpness;
                this.ID = ID;
            }
            
            public void Dispose() {
                if (Integration.Get<YHaptic>()?.GetActiveProvider() is YHapticIOS provider)
                    provider.Stop(this);
            }
        }
    }
    
    public enum HapticType {
        None = 0,
        LightImpact = 1,
        MediumImpact = 2,
        HeavyImpact = 3,
        RigidImpact = 4,
        SoftImpact = 5,
        Selection = 6,
        Success = 7,
        Warning = 8,
        Failure = 9
    }
}