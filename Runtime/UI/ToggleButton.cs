using System;
using System.Collections;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.UI;

namespace Yurowm {
    public class ToggleButton: Button, IUIRefresh {
        AnimationSampler sampler;
        
        public Func<bool> getState;
        public Action<bool> setState;
        
        bool State {
            get => getState?.Invoke() ?? false;
            set => setState?.Invoke(value);
        }
        
        public override void Initialize() {
            base.Initialize();
            
            this.SetupComponent(out sampler);
            
            SetAction(OnClick);
        }

        void OnClick() {
            State = !State;
            Animate(State);
        }
        
        void Animate(bool state) {
            animation = Animation(state);
            animation.Run();
        }
        
        IEnumerator animation;
        IEnumerator Animation(bool state) {
            var thisLogic = animation;
            var targetTime = state ? 1f : 0f;

            while (thisLogic == animation && targetTime != sampler.Time) {
                sampler.Time = sampler.Time.MoveTowards(targetTime, Time.unscaledDeltaTime * 4f);
                yield return null;
            }
            
            animation = null;
        }

        public void Refresh() {
            if (sampler)
                sampler.Time = State ? 1f : 0f;
        }
    }
}