using System;
using UnityEngine;
using Yurowm.Utilities;

namespace Yurowm {
    [ExecuteAlways]
    public class AnimationSampler : BaseBehaviour {

        public AnimationClip clip;
        
        [SerializeField]
        [Range(0, 1)]
        float time = 0;
        
        public EasingFunctions.Easing easing = EasingFunctions.Easing.Linear;
        
        public float Time {
            get => time;
            set {
                value = value.Clamp01();
                if (time == value) return;
                time = value;
                Refresh();
            }
        }
        
        public float RealTime {
            get => Time * Length;
            set => Time = value / Length;
        }
        
        public int Frame {
            set => clip?.SampleAnimation(gameObject, 1f * value / clip.frameRate);
        }

        public float Length => clip?.length ?? 0;

        public void Zero() {
            Time = 0;            
        }

        void OnValidate() {
            Refresh();    
        }
        
        void OnDidApplyAnimationProperties() {
            Refresh();    
        }

        void OnEnable() {
            Refresh();
        }

        void Refresh() {
            if (enabled && clip != null)
                clip.SampleAnimation(gameObject, time.Clamp(0, 0.9999f).Ease(easing) * clip.length);
        }
    }
}
