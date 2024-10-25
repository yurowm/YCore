using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Yurowm.Analytics;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.UI;

namespace Yurowm {
    public class Button : UIBehaviour, IPointerUpHandler, IPointerDownHandler, IPointerExitHandler {
        public UnityEvent onClick;

        ContentAnimator animator;
        ContentSound sound;
        AnimationSampler sampler;
        public string eventName;
        public bool lockWhileUIAnimation = true;
        
        [SerializeField]
        bool m_Interactable = true;
        public bool interactable {
            get => m_Interactable;
            set {
                if (m_Interactable == value) return;
                
                m_Interactable = value;
                
                animator?.Rewind(m_Interactable ? "Unlock" : "Lock");
                
                if (sampler)
                    sampler.Time = value ? 0 : 1;
            }
        }

        public override void Initialize() {
            base.Initialize();
            
            gameObject.SetupComponent(out animator);
            gameObject.SetupComponent(out sampler);
            gameObject.SetupComponent(out sound);
            
            if (sampler)
                sampler.Time = interactable ? 0 : 1;
        }

        protected override void OnEnable() {
            base.OnEnable();
            ResetAnimation();
        }
        
        protected override void OnDisable() {
            base.OnDisable();
            pressing = null;
            state = State.None;
            animator?.Rewind(pressDownClip);
        }
        
        public void SetAction(Action action) {
            NoAction();
            AddAction(action);
        }
        
        public void AddAction(Action action) {
            onClick.AddListener(action.Invoke);
        }

        public void NoAction() {
            onClick.RemoveAllListeners();
        }

        void ResetAnimation() {
            if (animator) {
                animator.Stop();
                animator?.RewindEnd(successClip);
            }
        }

        enum State {
            None,
            PressDown,
            PressUp,
            Escaped
        }
        
        State state = State.None;
        
        IEnumerator pressing = null;

        const string pressDownClip = "PressDown";
        const string successClip = "Click";
        
        IEnumerator Pressing() {
            state = State.PressDown;

            if (animator && animator.IsPlaying()) {
                var clip = animator.GetPlayingClip();
                animator.Stop();
                if (!clip.IsNullOrEmpty())
                    animator.RewindEnd(clip);
            }
            
            sound?.Play(pressDownClip);
            animator?.Play(pressDownClip);

            while (state == State.PressDown)
                yield return null;
            
            if (state == State.PressUp) {
                
                this.PlayClip(successClip);
                
                if (!lockWhileUIAnimation || !Page.IsAnimating) {
                    if (!eventName.IsNullOrEmpty())
                        Analytic.Event($"ButtonPress_{eventName}");
                    
                    try {
                        onClick.Invoke();
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
                }
            }
            
            if (state == State.Escaped) 
                animator?.Play(successClip);

            state = State.None;
            pressing = null;
        }
        
        #region Pointer Handlers

        public void OnPointerUp(PointerEventData eventData) {
            if (!interactable || pressing == null) return;
            
            if (!eventData.dragging && eventData.pointerCurrentRaycast.gameObject == gameObject
                && InputLock.GetAccess(contextTag.ID)) {
                state = State.PressUp;
            } else {
                state = State.Escaped;
            }
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (!interactable || pressing != null || (lockWhileUIAnimation && Page.IsAnimating))
                return;
            
            if (!InputLock.GetAccess(contextTag.ID)) 
                return;

            pressing = Pressing();
            pressing.Run();
        }

        #endregion

        public void OnPointerExit(PointerEventData eventData) {
            if (!interactable || pressing == null) return;
            
            if (state != State.None && eventData.pointerCurrentRaycast.gameObject != gameObject)
                state = State.Escaped;
        }
    }
}