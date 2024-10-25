using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Spaces;
using Yurowm.Utilities;

namespace Yurowm {
    [ExecuteAlways]
    public class ContentAnimator : BaseBehaviour, SpaceTime.ISensitiveComponent, IClipComponent {
        public bool ignoreTimeScale;
        public bool playSound;
        public string rewindClip;

        State state = null;

        // [HideInInspector]
        public List<Clip> clips = new();

        public float TimeScale { get; set; } = 1;
        
        ContentSound sound;

        void Awake() {
            this.SetupComponent(out sound);
        }

        void OnEnable() {
            if (!rewindClip.IsNullOrEmpty())
                Rewind(rewindClip);
        }

        public bool GetClip(string name, out Clip clip) {
            clip = clips.FirstOrDefault(x => x.name == name);
            return clip != null && clip.clip != null;
        }

        public AnimationEvent[] GetClipEvents(string name) {
            if (GetClip(name, out var clip))
                return clip.clip.events;

            return Array.Empty<AnimationEvent>();
        }
        
        public void Play(string clipName) {
            if (GetClip(clipName, out var clip)) 
                state = new State(clip);
            PlaySound(clipName);
        }

        void PlaySound(string clipName) {
            if (playSound && sound)
                sound.Play(clipName);
        }
        
        public void Play(string clipName, WrapMode wrapMode) {
            if (GetClip(clipName, out var clip))
                state = new State(clip, wrapMode);
            PlaySound(clipName);
        }

        public void Play(string clipName, WrapMode wrapMode, float timeScale, float timeOffset) {
            if (GetClip(clipName, out var clip)) {
                state = new State(clip, wrapMode);
                state.Time = timeOffset.Repeat(1);
                state.timeScale = timeScale;
            }
            PlaySound(clipName);
        }

        public IEnumerator WaitPlaying() {
            while (this && IsPlaying() && gameObject.activeInHierarchy)
                yield return null;
        }

        public IEnumerator PlayAndWait(string clipName) {
            if (GetClip(clipName, out var clip)) {
                state = new State(clip);
                PlaySound(clipName);
                return WaitPlaying();
            }

            return null;
        }

        public bool HasClip(string clipName) {
            return GetClip(clipName, out _);
        }

        public void Stop(string clipName) {
            if (IsPlaying(clipName))
                Stop();
        }

        public void Stop() {
            state = null;
        }

        public void Complete() {
            if (IsPlaying())
                state.mode = WrapMode.Once;
        }

        public void Complete(string clipName) {
            if (IsPlaying(clipName))
                state.mode = WrapMode.Once;
        }

        public IEnumerator CompleteAndWait(string clipName) {
            Complete(clipName);
            while (IsPlaying(clipName))
                yield return null;
        }

        public void CompleteAndPlay(string clipName) {
            if (IsPlaying(clipName)) {
                state.mode = WrapMode.Once;
                WaitPlaying().ContinueWith(() => Play(clipName)).Run();
            }
            else
                Play(clipName);
        }

        public string GetPlayingClip() {
            return state?.clip?.name;
        }
        
        public bool IsPlaying(string clipName) {
            return state != null && state.clip.name == clipName;
        }

        public bool IsPlaying() {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                return true;
            #endif

            return state != null;
        }

        public void Rewind(string clipName) {
            if (GetClip(clipName, out var clip)) {
                state = new State(clip);
                state.timeScale = 0;
                Sample();
            }
        }

        public void RewindEnd(string clipName) {
            if (GetClip(clipName, out var clip)) {
                state = new State(clip);
                state.Time = 1;
                state.timeScale = 0;
                Sample();
            }
        }
        
        public void SetPlaytime(string clipName, float time) {
            if (GetClip(clipName, out var clip))
                if (state != null && state.clip == clip)
                    state.Time = time;
        }

        void Update() {
            Sample();
        }

        SpaceTime time = null;

        Action<string> onEvent = delegate { };

        public void InvokeEvent(string eventName) {
            onEvent?.Invoke(eventName);
        }

        public void OnEvent(string eventName, Action action) {
            if (action == null) return;

            void Invoke(string name) {
                if (eventName == name) {
                    action.Invoke();
                    onEvent -= Invoke;
                }
            }

            onEvent += Invoke;
        }

        public void OnChangeTime(SpaceTime time) {
            this.time = time;
        }

        void Sample() {
            if (!enabled) return;
            if (state != null) {
                var delta = time?.Delta ?? (ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime);
                if (!state.Next(gameObject, delta * TimeScale.ClampMin(0)))
                    state = null;
                SendOnAnimate();
            }
        }

        IOnAnimateHandler[] onAnimateHandlersCache;

        void LateUpdate() {
            if (!enabled) return;
            if (IsPlaying()) {
                if (onAnimateHandlersCache == null)
                    onAnimateHandlersCache = gameObject.GetComponentsInChildren<IOnAnimateHandler>();
                SendOnAnimate();
            }
            else
                onAnimateHandlersCache = null;
        }

        public void SendOnAnimate() {
            (onAnimateHandlersCache ?? gameObject.GetComponentsInChildren<IOnAnimateHandler>()).ForEach(h =>
                h.OnAnimate());
        }

        [Serializable]
        public class Clip {
            public string name;
            public bool reverse = false;
            public AnimationClip clip;

            public Clip(string name) {
                this.name = name;
            }
        }

        class State {
            public Clip clip;

            float time = 0;

            public float Time {
                get => (clip.reverse ? length - time : time) / length;
                set => time = (clip.reverse ? 1f - value : value).Clamp01() * length;
            }

            public float timeScale;
            public WrapMode mode;

            float length;
            bool complete = false;
            bool hasEvents = false;

            public State(Clip clip) : this(clip, clip.clip.wrapMode) {
            }

            public State(Clip clip, WrapMode wrapMode) {
                this.clip = clip;
                length = clip.clip.length;
                mode = wrapMode;
                timeScale = 1;
                Time = 0;
                hasEvents = !clip.clip.events.IsEmpty();
            }

            public bool Next(GameObject gameObject, float deltaTime) {
                if (complete)
                    return false;

                if (this.clip.reverse)
                    deltaTime *= -1;

                var t = time + deltaTime * timeScale;

                var clip = this.clip.clip;

                switch (mode) {
                    case WrapMode.Default:
                    case WrapMode.Once: {
                        if (t < 0 || t > length) {
                            complete = true;
                            t = t.Clamp(0, length);
                        }

                        Sample(gameObject, t);
                        break;
                    }
                    case WrapMode.PingPong:
                        Sample(gameObject, Mathf.PingPong(t, clip.length));
                        break;
                    case WrapMode.Loop: {
                        t = Mathf.Repeat(t, clip.length);
                        Sample(gameObject, t);
                        break;
                    }
                }

                if (timeScale == 0)
                    complete = true;

                return !complete;
            }

            void Sample(GameObject gameObject, float newTime) {
                if (!gameObject) return;

                var clip = this.clip?.clip;

                if (!clip) return;

                clip.SampleAnimation(gameObject, newTime);
                #if ANIMATION_DEBUG
                Debug.Log($"[ANIMATION] {gameObject.name} ({clip.name}: {newTime:F2}s.)");
                #endif

                if (hasEvents && time != newTime)
                    clip.events.Where(e =>
                            AnimationEventUtilities.IsTimeForEvent(this.clip.reverse, e.time, time, newTime))
                        .ForEach(e => AnimationEventUtilities.Invoke(gameObject, e));

                time = newTime;
            }
        }
    }

    public interface IOnAnimateHandler {
        void OnAnimate();
    }
    
    public class ANIMATION_DEBUG_SYMBOL: ScriptingDefineSymbol {
        public override string GetSybmol() {
            return "ANIMATION_DEBUG";
        }
    }
}