using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Yurowm.Extensions {
    public static class UniTaskEx {
        public static void Complete(this UniTask task) {
            task.GetAwaiter().GetResult();
        }
        
        public static async UniTask WaitWithDelay(Func<bool> predicate, float delay) {
            float? lastTrue = null;

            while (true) {
                if (predicate()) {
                    if (!lastTrue.HasValue)
                        lastTrue = Time.time;
                    if (lastTrue.Value + delay < Time.time)
                        return;
                } else
                    lastTrue = null;
                
                await UniTask.Yield();
            }
        }
    }
}