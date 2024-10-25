using System;
using System.Collections;
using UnityEngine;
using Yurowm.Coroutines;

namespace Yurowm.Animations {
    public class Tween {
        Action<float> update;
        float speed;
        public Tween(Action<float> update, float speed = 1f) {
            this.update = update;
            this.speed = speed;
        }

        IEnumerator process = null;

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
            if (process == null) {
                process = Process();
                process.Run();
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
        IEnumerator Process() {
            while (currentTime != targetTime) {
                currentTime = Mathf.MoveTowards(currentTime, targetTime, Time.deltaTime * speed);
                update(currentTime);
                yield return null;
            }
            process = null;
        }

        public void Break() {
            currentTime = -1;
            if (process != null) {
                GlobalCoroutine.Stop(process);
                process = null;
            }
        }
    }
}