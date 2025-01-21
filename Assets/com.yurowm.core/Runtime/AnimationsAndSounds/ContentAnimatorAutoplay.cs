using System;
using UnityEngine;
using Yurowm.Utilities;

namespace Yurowm {
    [RequireComponent(typeof(ContentAnimator))]
    public class ContentAnimatorAutoplay : BaseBehaviour {
        
        ContentAnimator animator;
        
        public enum Event {
            Awake,
            OnEnable,
            Blink
        }
        
        public WrapMode wrapMode = WrapMode.Default;
        
        public Event eventType;
        
        public string clipName;
        
        public float timeScale = 1;
        public float timeOffset = 0;
        
        void Awake() {
            animator = GetComponent<ContentAnimator>();
            
            if (eventType == Event.Awake)
                animator.Play(clipName, wrapMode, timeScale, timeOffset);
        }

        void OnEnable() {
            nextBlinkTime = -1;
            if (eventType == Event.OnEnable)
                animator.Play(clipName, wrapMode, timeScale, timeOffset);
        }

        float nextBlinkTime = -1;
        
        void Update() {
            if (eventType != Event.Blink)
                return;
            
            if (nextBlinkTime < 0)
                nextBlinkTime = Time.time + YRandom.main.Range(5f, 10f);
            
            if (nextBlinkTime <= Time.time) {
                animator.Play(clipName, wrapMode, timeScale, timeOffset);
                nextBlinkTime = -1;
            }
        }
    }
}