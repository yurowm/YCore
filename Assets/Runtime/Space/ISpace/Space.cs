using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using Yurowm.ContentManager;
using Yurowm.Controls;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Jobs;
using Yurowm.Profiling;
using Yurowm.Serialization;
using Yurowm.UI;
using Yurowm.Utilities;
using Object = UnityEngine.Object;

namespace Yurowm.Spaces {
    [Preserve]
    public abstract class Space : ILiveContextHolder {
        public static List<Space> all = new List<Space>();
        public LiveContext _context = null;
        public LiveContext context {
            get => _context;
            
            set {
                if (_context == value) return;
                
                _context?.RemoveArgument(this);
                
                _context = value;
                
                if (_context == null) return;
                
                _context.SetArgument(typeof(Space), this);
                _context.SetArgument(GetType(), this);
            }
        }
        public Clickables clickables = new Clickables();
        public CoroutineCore coroutine = new CoroutineCore();
        
        [OnLaunch]
        static void InitializeOnLoad() {
            OnLaunchAttribute.unload += () => 
                all.ToArray().ForEach(s => s.Destroy());
        }
        
        public YRandom random;

        List<IJob> jobs;
        public Transform root;

        public SpaceTime time;
        
        public void SetTimeScale(float timeScale) {
            timeScale = timeScale.ClampMin(0);
            time.Scale = timeScale;
            Physics2DJob.TimeScale = timeScale;
            Physics3DJob.TimeScale = timeScale;
        }
        
        bool _enabled = false;
        public bool enabled {
            get => _enabled;
            set {
                if (_enabled != value) {
                    _enabled = value;
                    if (_enabled)
                        OnEnable();
                    else
                        OnDisable();
                }
            }
        }
        
        public Space() {}
        
        public Space(LiveContext context) : this() {
            this.context = context;
        }
        
        public virtual void Initialize() {
            time = new SpaceTime();
            context.SetArgument(time);
            context.SetArgument(coroutine);
            context.SetArgument(this);
            context.SetArgument<Space>(this);
            time.Updating().Run(coroutine);
            
            DebugPanel.Log($"{this}_timeScale", "Space", new DebugVariableRange<float>(
                    () => time.Scale,
                    v => time.Scale = v,
                    0, 2));
        }

        #region Creating

        static int indexer = 0;
        
        public static Space Create(Type spaceType, LiveContext context = null) {
            if (spaceType == null || !typeof(Space).IsAssignableFrom(spaceType))
                throw new Exception("Wrong Space type");
                
            Space result = null;
            
            if (spaceType.GetConstructor(new [] {typeof(LiveContext)}) != null)
                result = Activator.CreateInstance(spaceType, context) as Space;
            
            if (spaceType.GetConstructor(new Type[0]) != null)
                result = Activator.CreateInstance(spaceType) as Space;
            
            if (result == null)
                throw new Exception("Space creation is failed");

            result.context = context ?? new LiveContext($"{spaceType.Name} ({++indexer})");
            
            result.random = new YRandom();
            
            result.jobs = result.RegisterJobs().ToList();
            
            result.root = new GameObject($"ROOT: {result.context.Name}").transform;
            result.root.gameObject.SetActive(false);

            all.Add(result);
            
            result.SpaceUpdate(CoroutineCore.Loop.Update).Run(loop: CoroutineCore.Loop.Update);
            result.SpaceUpdate(CoroutineCore.Loop.FixedUpdate).Run(loop: CoroutineCore.Loop.FixedUpdate);
            result.SpaceUpdate(CoroutineCore.Loop.LateUpdate).Run(loop: CoroutineCore.Loop.LateUpdate);

            result.Initialize();
            
            result.jobs.CastIfPossible<ICatchJob>().ForEach(j => j.CatchInSpace(result));

            if (result is IUIRefresh refresh)
                UIRefresh.Add(refresh);

            OnCreateSpace(result);
            
            return result;
        }
            
        public static S Create<S>(LiveContext context = null) where S : Space {
            return Create(typeof(S), context) as S;
        }
            
        #endregion
        
        #region Pause

        float targetTimeScale = 1;
        float timeScaleChangingSpeed = 3;

        IEnumerator timeScaleChangingLogic = null;
        
        public bool TimeScaleChanging => timeScaleChangingLogic != null;
        
        public Action onPause = delegate {};
        public Action onUnpause = delegate {};
        
        public void Pause() {
            SetTimeScaleSmooth(0);
            onPause();
        }
        
        public void Unpause() {
            SetTimeScaleSmooth(1);
            onUnpause();
        }

        public void SetTimeScaleSmooth(float timeScale, float speed = 3) {
            targetTimeScale = timeScale;
            timeScaleChangingSpeed = speed;

            if (!TimeScaleChanging) {
                timeScaleChangingLogic = TimeScaleChangingLogic();
                timeScaleChangingLogic.Run(coroutine);
            }
        }

        IEnumerator TimeScaleChangingLogic() {
            for (; time.Scale != targetTimeScale; time.Scale = time.Scale.MoveTowards(targetTimeScale, Time.unscaledDeltaTime * timeScaleChangingSpeed)) {
                SetTimeScale(time.Scale);
                yield return null;
            }
            
            SetTimeScale(targetTimeScale);
            
            timeScaleChangingLogic = null;
        }
        
        #endregion

        /// <summary>
        /// Подготовливаем Space к созданию в первый раз. Creation должен вызвать Complete.
        /// Creation вызывается как альтернатива десериализатора, который создает то же самое, но на основе сохранения.
        /// </summary>
        public virtual IEnumerator Creation() {
            yield return Complete();
        }

        /// <summary>
        /// Генерируем все объекты пространства.
        /// </summary>
        public abstract IEnumerator Complete();

        bool _destroyed = false;
        public virtual bool IsActual() {
            return !_destroyed;
        }

        public virtual IEnumerator<IJob> RegisterJobs() {
            var jobs = JobUtility.AllJobs()
                .Inheritors<ISpaceJob>()
                .Emit<IJob>()
                .ToList();
            jobs.ForEach(j => {
                j.context = context;
                (j as ISpaceJob).space = this;
            });
            jobs.Sort((a, b) => b.GetPriority().CompareTo(a.GetPriority()));
            foreach (IJob job in jobs)
                yield return job;
        }
        
        List<GameEntity> enabledCache = new List<GameEntity>();

        public virtual void OnDisable() {
            if (root)
                root.gameObject.SetActive(false);
            _enabled = false;
            enabledCache.Clear();
            enabledCache.AddRange(context.GetAll<GameEntity>().Where(e => e.enabled));
            enabledCache.ForEach(i => i.enabled = false);
        }

        public virtual void OnEnable() {
            _enabled = true;
            if (root)
                root.gameObject.SetActive(true);
            enabledCache.Where(i => i != null).ForEach(i => i.enabled = true);
            enabledCache.Clear();
        }

        #region Jobs

        public void Subscribe(object subscriber) {
            jobs.Where(j => j.IsSuitable(subscriber))
                .ForEach(j => j.Subscribe(subscriber));
        }
        
        public void Subscribe<J>(object subscriber) where J : IJob {
            IJob job = jobs.FirstOrDefault(j => j is J);
            if (job == null) throw new System.Exception("Job is not found");
            job.Subscribe(subscriber);
        }

        public void Unsubscribe(object subscriber) {
            jobs.ForEach(j => j.Unsubscribe(subscriber));
        }

        public J GetJob<J>() where J : IJob {
            return (J) jobs.FirstOrDefault(j => j is J);
        }
        
        public IEnumerable<J> GetJobs<J>() {
            return jobs.CastIfPossible<J>();
        }
        
        #endregion

        #region Update

        IEnumerator SpaceUpdate(CoroutineCore.Loop loop) {
            while (!_destroyed) {
                if (enabled) {
                    coroutine.Update(loop);
                    if (loop == CoroutineCore.Loop.Update)
                        jobs.CastIfPossible<IUpdateJob>().ForEach(Updater);
                    OnUpdate();
                }
                yield return null;
            }
        }
        
        public virtual void OnUpdate() {}
        
        void Updater(IUpdateJob job) {
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
            using (YProfiler.Area("Space Job: " + job.GetType().FullName))
                #endif
                job.Do();
        }
        
        #endregion
        
        public Action onDestroy = delegate {};
        public Action onDestroyed = delegate {};
        
        public virtual void Destroy() {
            if (_destroyed) return;
            _destroyed = true;

            if (this is IUIRefresh refresh)
                UIRefresh.Remove(refresh);
                
            onDestroy?.Invoke();
            
            context.Destroy();

            coroutine.Clear();
            
            onDestroyed?.Invoke();
            
            busy = false;
            
            DebugPanel.RemoveEntry($"{this}_timeScale", "Space");
            
            foreach (var entity in context.GetAll<GameEntity>())
                RemoveItem(entity);
            
            all.Remove(this);
            if (root)
                Object.Destroy(root.gameObject);
        }

        public Action<GameEntity> onAddItem = delegate { };

        public void AddItem(GameEntity item) {
            if (context.Add(item)) {
                item.space = this;
                item.OnAddToSpace(this);
                onAddItem(item);
                item.enabled = true;
            }
        }

        public void RemoveItem(GameEntity item) {
            if (item.space == this) {
                item.enabled = false;
                item.OnRemoveFromSpace(this);
                context.Remove(item);
                item.space = null;
            }
        }

        #region Catch
        
        static List<Func<Space, bool>> catchers = new();
       
        public static S Cast<S>() where S : Space {
            return all.CastOne<S>();
        }
        
        public static void BlindCatchSpace<S>(Action<S> action) where S : Space {
            if (action == null) return;
            CatchSpace(space => {
                if (space is S s) {
                    action.Invoke(s);
                    return true;
                }
                return false;
            });
        }  
        
        public static void CatchSpace<S>(Func<S, bool> filter) where S : Space {
            CatchSpace(space => {
                if (space is S s)
                    return filter?.Invoke(s) ?? true;
                return false;
            });
        }        
        
        public static void CatchSpace(Func<Space, bool> filter) {
            if (filter == null) return;
            foreach (Space space in Space.all)
                if (filter.Invoke(space))
                    return;
            catchers.Add(filter);
        }
        
        static void OnCreateSpace(Space space) {
            catchers.RemoveAll(c => c.Invoke(space));
        }

        #endregion
        
        #region Show

        public static void Hide(Type spaceType) {
            if (spaceType == null || !typeof(Space).IsAssignableFrom(spaceType))
                throw new Exception("Wrong Space type");
            
            var space = all.FirstOrDefault(spaceType.IsInstanceOfType);
            
            if (space) space.enabled = false;
        }

        public static void KillAll<S>() where S : Space {
            all
                .CastIfPossible<S>()
                .ToArray()
                .ForEach(s => s.Destroy());
        }

        public static void Show<S>(Action<S> onCreate = null) where S : Space {
            Showing(typeof(S), space => {
                onCreate?.Invoke(space as S);
            }).Run();
        }

        public static void Show(Type spaceType, Action<Space> onCreate = null) {
            Showing(spaceType, onCreate).Run();
        }

        #region Busy
        
        static int countOfBusy;
        
        bool _busy = false;
        bool busy {
            get => _busy;
            set {
                if (_busy == value)
                    return;
                _busy = value;
                countOfBusy += _busy ? 1 : -1;
            }
        }
        
        public bool IsBusy() => busy;
        
        public static bool AnyBusy() => countOfBusy > 0;
        
        #endregion

        public static IEnumerator Showing(Type spaceType, Action<Space> onCreate = null) {
            
            var space = all.FirstOrDefault(spaceType.IsInstanceOfType);
            
            if (space && !space.IsActual()) {
                space.Destroy();
                space = null;
            }

            if (!space) {
                yield return Prepare(spaceType, s => {
                    space = s;
                    onCreate?.Invoke(s);
                });
            }
            
            if (space) 
                space.enabled = true;
        }
        
        public static IEnumerator Prepare(Type spaceType, Action<Space> onCreate = null) {
            if (spaceType == null || !typeof(Space).IsAssignableFrom(spaceType))
                throw new Exception("Wrong Space type");
            
            var space = Create(spaceType);
            if (!space) yield break;
            
            onCreate?.Invoke(space);
            
            space.busy = true;
            
            yield return space.Creation();
            
            space.busy = false;
            
            if (space is IUIRefresh refresh)
                refresh.Refresh();
        }
        
        #endregion
        
        #region ISerializable
        public virtual void Serialize(IWriter writer) {
            writer.Write("objects", context.GetAll<ILiveContexted>(x => x is ISerializable).Cast<ISerializable>().ToList());
        }

        public virtual void Deserialize(IReader reader) {
            foreach (var entity in reader.ReadCollection<ISerializable>("objects")) {
                if (entity is GameEntity)
                    AddItem(entity as GameEntity);
                else
                    context.Add(entity as ILiveContexted);
            }
                
        }
        #endregion

        public static implicit operator bool(Space space) {
            return space != null;
        }

        [Flags]
        public enum LoadingType {
            Creation = 1 << 1,
            Deserialization = 1 << 2
        }

        #region ILiveContextHolder
        
        public LiveContext GetContext() {
            return context;
        }
        
        #endregion
    }

    public interface ISpaceJob {
        Space space {get; set;}
    }

    
    public class SpaceTime {
        
        public Action<float> onScaleChanged = delegate {};
        
        
        public IEnumerator Updating() {
            while (true) {
                Update();
                yield return null;
            }
        }
        
        public void Update() {
            RealDelta = Time.deltaTime.ClampMax(MaxDelta);
            Delta = RealDelta * Scale;
            AbsoluteTime += Delta;
        }

        public float AbsoluteTime {get; private set; }

        float _scale = 1f;
        public float Scale {
            get => _scale;
            set {
                if (_scale == value) return;
                _scale = value;
                onScaleChanged.Invoke(_scale);
            }
        }

        public float MaxDelta = 1f / 20;
        public float Delta { get; private set; }
        public float RealDelta { get; private set; }

        public void SetTime(float absoluteTime) {
            AbsoluteTime = absoluteTime;
        }

        public void Break() {
            SetTime(0);
        }

        public IEnumerator Wait(float duration) {
            var triggerTime = AbsoluteTime + duration;
            
            while (triggerTime > AbsoluteTime)
                yield return null;
        }
        
        public interface ISensitiveComponent {
            void OnChangeTime(SpaceTime time);
        }

        public IEnumerator Animate(float duration, Action<float> action) {
            if (action == null)
                yield break;
            
            for (var t = 0f; t < 1f; t += Delta / duration) {
                action.Invoke(t);
                yield return null;
            }
            action.Invoke(1f);
        }
    }
}
