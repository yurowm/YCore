using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.UI;
using Yurowm.Utilities;

namespace Yurowm {
    public class ToggleButton: Button, IUIRefresh {
        AnimationSampler sampler;
        
        public Func<bool> getState;
        public Action<bool> setState;
        
        public float animationSpeed = 4;
        
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
            animationSeed = YRandom.main.Value();
            Animation(state).Forget();
        }
        
        float animationSeed;
        async UniTask Animation(bool state) {
            var seed = animationSeed;
            var targetTime = state ? 1f : 0f;

            while (seed == animationSeed && targetTime != sampler.Time) {
                sampler.Time = sampler.Time.MoveTowards(targetTime, Time.unscaledDeltaTime * animationSpeed);
                await UniTask.Yield();
            }
        }

        public void Refresh() {
            if (sampler)
                sampler.Time = State ? 1f : 0f;
        }
    }
}