using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.Utilities;

namespace Yurowm.Animations {
    public class Tween {
        Action<float> update;
        float speed;
        public Tween(Action<float> update, float speed = 1f) {
            this.update = update;
            this.speed = speed;
        }

        float processSeed;

        public void GoToStart() {
            GoTo(0f);
        }

        public void GoToEnd() {
            GoTo(1f);
        }

        void GoTo(float time) {
            if (targetTime != time) {
                targetTime = time;
                if (currentTime < 0)
                    currentTime = 1f - targetTime;
                Start();
            }
        }

        void Start() {
            if (processSeed == 0) {
                processSeed = YRandom.main.Value();
                Process().Forget();
            }
        }
               
        public void SetTime(float time) {
            currentTime = Mathf.Clamp01(time);
            targetTime = currentTime;
            update(currentTime);
        }

        public float GetTime() {
            return currentTime;
        }

        float currentTime = -1;
        float targetTime = 0;
        async UniTask Process() {
            var seed = processSeed;
            while (seed == processSeed && currentTime != targetTime) {
                currentTime = Mathf.MoveTowards(currentTime, targetTime, Time.deltaTime * speed);
                update(currentTime);
                await UniTask.Yield();
            }
            processSeed = 0;
        }

        public void Break() {
            currentTime = -1;
            processSeed = 0;
        }
    }
}