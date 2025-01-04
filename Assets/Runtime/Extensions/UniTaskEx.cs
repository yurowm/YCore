using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Yurowm.Extensions {
    public static partial class UniTaskEx {
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
        
        public static UniTask AddTo(this UniTask task, TaskDisposables disposables) {
            return disposables.Add(task);
        }
        
        public static void Forget(this UniTask task, TaskDisposables disposables) {
            AddTo(task, disposables).Forget();
        }
        
        public static void Forget<T>(this UniTask<T> task, TaskDisposables disposables) {
            AddTo(task, disposables).Forget();
        }
        
        public static TaskStopable RunStopable(this UniTask task) {
            return new TaskStopable(task);
        }
    }
    
    public struct TaskStopable {
        readonly UniTask task;
        CancellationTokenSource cts;
        
        public TaskStopable(UniTask task) {
            cts = new ();
            this.task = task.AttachExternalCancellation(cts.Token);
        }
        
        public bool IsEmpty() => cts == null;
        
        public bool IsActive() => !IsEmpty() && task.Status == UniTaskStatus.Pending;

        public void Stop() {
            if (cts == null) return;
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

    }
    
    public class TaskDisposables : IDisposable {
        readonly CancellationTokenSource cts = new();
        private bool _isDisposed;

        public UniTask Add(UniTask task) {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TaskDisposables));
        
            return task.AttachExternalCancellation(cts.Token);
        }

        public void Dispose() {
            cts.Cancel();
            cts.Dispose();
            _isDisposed = true;
        }
    }
}