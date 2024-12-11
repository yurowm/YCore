using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using Yurowm.ContentManager;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Utilities;
using Yurowm.Profiling;
using Space = Yurowm.Spaces.Space;

namespace Yurowm.Jobs {
    public static class JobSystem  {
        static List<IJob> jobs = null;

        [OnLaunch()]
        static void Initialize() {
            if (OnceAccess.GetAccess("UpdaetJob"))
                DoUpdateJob().Forget();
        }

        static IEnumerator<IJob> RegisterJobs() {
            var jobs = new List<IJob>() {
                new SelfUpdateJob()
            };
            jobs.Sort((a, b) => b.GetPriority().CompareTo(a.GetPriority()));
            foreach (IJob job in jobs)
                yield return job;
        }

        static async UniTask DoUpdateJob() {
            await UniTask.Yield();

            while (true) {
                if (jobs != null) {
                    for (int i = 0; i < jobs.Count; i++) {
                        var job = jobs[i];   
                        if (job is IUpdateJob updateJob)
                            using (YProfiler.Area($"Global Job: ({job})"))
                                updateJob.Do();
                    }
                }
                
                await UniTask.Yield();
            }
        }

        public static void Subscribe<J>(object subscriber) where J : IJob {
            if (jobs == null)
                jobs = RegisterJobs().ToList();
            IJob job = jobs.FirstOrDefault(j => j is J);
            if (job == null) throw new Exception("Job is not found");
            job.Subscribe(subscriber);
        }

        public static void Unsubscribe(object subscriber) {
            jobs?.ForEach(j => j.Unsubscribe(subscriber));
        }
    }

    public abstract class Job<S> : IJob where S : class {
        CodeLocker locker = new CodeLocker();
        CodeLocker queueLocker = new CodeLocker();
        Queue<IDelayedAction> subscribersQueue = new Queue<IDelayedAction>();
        public LiveContext context { get; set; }

        public Job() { }
        public List<S> subscribers = new List<S>();
        
        public virtual void Do() {
            using (locker.Lock()) {
                try {
                    ToWork();
                } catch (Exception e) {
                    Debug.LogException(e);
                }
                using (queueLocker.Lock())
                    while (subscribersQueue.Count > 0) {
                        try {
                            IDelayedAction action = subscribersQueue.Dequeue();
                            switch (action) {
                                case SubscribeAction _: _Subscribe(action.target); break;
                                case UnsubscribeAction _: _Unsubscribe(action.target); break;
                            }
                        } catch (Exception e) {
                            Debug.LogException(e);
                        }
                    }
            }
        }

        public abstract void ToWork();

        public virtual int GetPriority() {
            return 0;
        }

        public virtual bool IsSuitable(object subscriber) {
            return subscriber is S;
        }

        public void Unsubscribe(object subscriber) {
            if (locker.TryLock()) {
                _Unsubscribe(subscriber);
                locker.Unlock();
            } else
                using (queueLocker.Lock())
                    subscribersQueue.Enqueue(new UnsubscribeAction(subscriber));
        }

        public void Subscribe(object subscriber) {
            if (locker.TryLock()) {
                _Subscribe(subscriber);
                locker.Unlock();
            } else
                using (queueLocker.Lock())
                    subscribersQueue.Enqueue(new SubscribeAction(subscriber));
        }

        void _Subscribe(object subscriber) {
            try {
                if (subscriber is IJobFiltered filtured && !filtured.IsSuitableForJob(this))
                    return;
                
                if (subscriber is S s) {
                    subscribers.Add(s);
                    OnSubscribe(s);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        void _Unsubscribe(object subscriber) {
            try {
                if (subscriber is S s) {
                    subscribers.Remove(s);
                    OnUnsubscribe(s);
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public virtual void OnSubscribe(S subscriber) { }
        public virtual void OnUnsubscribe(S subscriber) { }

        interface IDelayedAction {
            object target { get; }
        }
        
        struct SubscribeAction : IDelayedAction {
            public object target { get; }
            public SubscribeAction(object target) {
                this.target = target;
            }
        }
        
        struct UnsubscribeAction : IDelayedAction {
            public object target { get; }
            public UnsubscribeAction(object target) {
                this.target = target;
            }
        }
    }

    public abstract class Job : Job<object> { }

    [Preserve]
    public interface IJob {
        LiveContext context {get; set;}

        void Subscribe(object subscriber);
        void Unsubscribe(object subscriber);
        int GetPriority();
        
        bool IsSuitable(object subscriber);
    }
    
    public interface IUpdateJob {
        void Do();
    }
        

    public interface IJobFiltered {
        bool IsSuitableForJob(IJob job);
    }
    
    public interface ICatchJob : IJob {
        void CatchInSpace(Space space);
    }

    public static class JobUtility {
        static readonly List<Type> allJobs;

        static JobUtility() {
            allJobs = Utils.FindInheritorTypes<IJob>(true).ToList();
        }

        public static IEnumerator<Type> AllJobs() {
            foreach (Type type in allJobs)
                yield return type;
        }

        public static IEnumerator<Type> Inheritors<T>(this IEnumerator<Type> types) {
            Type targetType = typeof(T);
            while (types.MoveNext())
                if (targetType.IsAssignableFrom(types.Current))
                    yield return types.Current;
        }

        public static IEnumerator<T> Emit<T>(this IEnumerator<Type> types) {
            while (types.MoveNext())
                yield return (T) Activator.CreateInstance(types.Current);
        }
    }
}