using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.UI;
using Yurowm.Utilities;

namespace Yurowm {
    public class UIPanel : Behaviour, ISerializationCallbackReceiver {

        public int linkID;
                
        [NonSerialized]
        public bool freez;
        
        public bool warm = false;
        
        public string overrideShowClip;
        public string overrideHideClip;
        
        IDisposable activeAnimation;
        public bool isPlaying {
            get => activeAnimation != null;
            set {
                if (isPlaying == value) 
                    return;
                
                if (value)
                    activeAnimation = Page.NewActiveAnimation();
                else {
                    activeAnimation.Dispose();
                    activeAnimation = null;
                }
            }
        }

        protected ContentAnimator animator;
        protected AnimationSampler sampler;
        protected ContentSound sound;
        
        public override void Initialize() {
            base.Initialize();
            
            this.SetupComponent(out sampler);
            this.SetupComponent(out sound);
            if (this.SetupComponent(out animator))
                animator.Rewind(ShowClip);
        }

        protected virtual void OnEnable() {
            freez = false;
        }

        void OnDisable() {
            isPlaying = false;
        }
        
        const string hideClip = "Hide";
        const string showClip = "Show";
        
        string HideClip => overrideHideClip.IsNullOrEmpty() ? hideClip : overrideHideClip;
        string ShowClip => overrideShowClip.IsNullOrEmpty() ? showClip : overrideShowClip;

        IEnumerator Play(bool visible) {
            while (isPlaying)
                yield return null;
            
            isPlaying = true;
            
            if (visible) gameObject.SetActive(true);
            
            yield return Playing(visible);
            
            if (!visible) gameObject.SetActive(false);
            
            isPlaying = false;
        }

        IEnumerator Playing(bool visible) {
            if (animator) {
                var clip = visible ? ShowClip : HideClip;
                yield return animator.PlayAndWait(clip);
                yield break;
            }
            if (sampler) {
                var length = sampler.Length;
                
                float start, end;
                
                if (visible) {
                    start = 0;
                    end = length;
                } else {
                    start = length;
                    end = 0;
                }
                
                for (float t = start;
                    t != end; 
                    t = Mathf.MoveTowards(t, end, Time.unscaledDeltaTime)) {
                    
                    sampler.RealTime = t;
                    
                    yield return  null;
                }
                
                sampler.RealTime = end;
            }
            
        }
        
        public IEnumerator WaitPlaying() {
            while (isPlaying) 
                yield return null;
        }
        
        public void SetVisible(bool visible, bool immediate = false) {
            if (gameObject.activeSelf == visible)
                return;

            // Initialize();
            
            var clip = visible ? ShowClip : HideClip;
            
            if (immediate) {
                if (animator)
                    animator.RewindEnd(clip);
                else if (sampler)
                    sampler.Time = visible ? 1 : 0;
                
                gameObject.SetActive(visible);
            } else 
                Play(visible).Run();
            
            sound?.Play(clip);
        }

        #if UNITY_EDITOR
        static List<UIPanel> linked = new List<UIPanel>();
        #endif
        
        public void OnBeforeSerialize() {
            #if UNITY_EDITOR
            try {
                if (gameObject.scene.path.IsNullOrEmpty())
                    return;
            } catch {
                return;
            }
            
            contextTag.registerOnLaunch = true;
            
            if (!linked.Contains(this))
                linked.Add(this);
            
            linked.RemoveAll(l => !l);
            
            while (linkID == 0 || linked.Any(l => l != this && l.linkID == linkID)) 
                linkID = YRandom.main.Range(int.MinValue, int.MaxValue);
            #endif
        }

        public void OnAfterDeserialize() {}
    }
}