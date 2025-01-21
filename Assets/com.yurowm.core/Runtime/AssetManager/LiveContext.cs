using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.ContentManager {
    public class LiveContext {
        List<ILiveContexted> all = new();
        Dictionary<Type, object> arguments;

        public string Name {
            get;
            private set;
        }
        
        public LiveContext(string name, params object[] arguments) {
            Name = name;
            contexts.Add(this);
            if (arguments != null)
                this.arguments = arguments.GroupBy(a => a.GetType()).ToDictionary(g => g.Key, g => g.First());
        }

        #region Arguments
        
        public A GetArgument<A>(bool inherits = false) where A : class {
            if (arguments == null) return null;
            Type t = typeof(A);
            if (inherits)
                return (A) arguments.FirstOrDefault(p => p.Key.IsAssignableFrom(t)).Value;

            return (A) arguments.Get(t);
        }

        public IEnumerable<object> GetAllArguments() {
            foreach (var arg in arguments.Values)
                yield return arg;
        }

        public void SetArgument(Type keyType, object value) {
            if (value == null) throw new NullReferenceException("value");
            if (keyType == null) throw new NullReferenceException("keyType");
            if (!keyType.IsInstanceOfType(value)) 
                throw new Exception("Type of the Value is not instance of keyType");
            
            arguments.Set(keyType, value);
        }

        public void SetArgument<A>(A value) where A : class {
            if (value == null) throw new NullReferenceException("value");
            Type t = typeof(A);
            arguments.Set(t, value);
        }
        
        public void RemoveArgument(object value) {
            if (arguments == null || arguments.Count == 0) return;
            arguments = arguments
                .RemoveAll(a => a.Value == value)
                .ToDictionary();
        }

        #endregion

        #region Singletons

        List<LiveContextSingleton> singletons = new();

        public S GetSingleton<S>(string key = null) where S: LiveContextSingleton {
            S result;
            
            if (key.IsNullOrEmpty())
                result = singletons.CastIfPossible<S>().FirstOrDefault();
            else
                result = singletons.CastIfPossible<S>().FirstOrDefault(s => s.SingletonKey == key);
            
            if (result == null) {
                result = Activator.CreateInstance<S>();
                result.Initialize(this, key);
                singletons.Add(result);
            }
            
            return result;
        }
        
        public void RemoveSingleton(LiveContextSingleton singleton) {
            if (singletons.Remove(singleton))
                singleton.OnKill();
        }

        #endregion

        #region Items
        
        bool Search<T>(ILiveContexted content, Type type, T original, Func<T, bool> condition) where T : class, ILiveContexted {
            return (original == null || original.EqualContent(content)) && type.IsInstanceOfType(content)
                && (condition == null || condition.Invoke((T) content));
            //return (!original || content._original == original.gameObject) && type.IsAssignableFrom(content.GetType())
            //    && (condition == null || condition.Invoke((T) content));
        }

        public T Get<T>(Func<T, bool> condition = null, T original = null) where T : class, ILiveContexted {
            Type type = typeof(T);
            return (T) all.FirstOrDefault(x => Search(x, type, original, condition));
        }
        
        public bool SetupItem<T>(out T item) where T : class, ILiveContexted {
            item = Get<T>();
            return item != null;
        }
        
        public bool SetupItem<T>(Func<T, bool> condition, out T item) where T : class, ILiveContexted {
            item = Get<T>(condition);
            return item != null;
        }

        public IEnumerable<T> GetAll<T>(Func<T, bool> condition = null, T original = null) where T : class, ILiveContexted {
            Type type = typeof(T);
            return all.Where(x => Search(x, type, original, condition)).Cast<T>();
        }

        public int Count<T>(Func<T, bool> condition = null, T original = null) where T : class, ILiveContexted {
            Type type = typeof (T);
            return all.Count(x => Search(x, type, original, condition));
        }

        public bool Contains<T>(Func<T, bool> condition = null, T original = null) where T : class, ILiveContexted {
            Type type = typeof(T);
            return all.Any(x => Search(x, type, original, condition));
        }

        public bool Contains(ILiveContexted liveContexted) {
            return all.Contains(liveContexted);
        }
        
        public bool Add(ILiveContexted item, bool initialize = true) {
            if (!all.Contains(item)) {
                item.context = this;
                if (initialize)
                    item.Initialize();
                all.Add(item);
                onAdd?.Invoke(item);
                return true;
            }
            return false;
        }

        public void Remove(ILiveContexted item) {
            if (all.Remove(item))
                onRemove.Invoke(item);
        }

        #endregion

        #region Catch
        
        public Action<ILiveContexted> onAdd = delegate {};
        public Action<ILiveContexted> onRemove = delegate {};
        
        public void Catch<I>(Func<I, bool> catcher) where I : class, ILiveContexted {
            foreach (var i in GetAll<I>())
                if (catcher(i))
                    return;

            bool wait = true;
            
            void Delayed(ILiveContexted item) {
                if (item is I _i && catcher(_i)) {
                    onAdd -= Delayed;
                    wait = false;
                }
            }

            onAdd += Delayed;
        }
        
        public async UniTask WaitCatch<I>(Func<I, bool> catcher) where I : class, ILiveContexted {
            foreach (var i in GetAll<I>())
                if (catcher(i))
                    return;

            bool wait = true;
            
            void Delayed(ILiveContexted item) {
                if (item is I _i && catcher(_i)) {
                    onAdd -= Delayed;
                    wait = false;
                }
            }

            onAdd += Delayed;

            while (wait) await UniTask.Yield();
        }

        public async UniTask WaitCatch<I>(Action<I> catcher) where I : class, ILiveContexted {
            var i = Get<I>();
            if (i != null) {
                catcher(i);
                return;
            }
            
            bool wait = true;
            
            void Delayed(ILiveContexted item) {
                if (item is I _i) {
                    catcher?.Invoke(_i);
                    onAdd -= Delayed;
                    wait = false;
                } 
            }
            
            onAdd += Delayed;
            
            while (wait) await UniTask.Yield();
        }

        public void Catch<I>(Action<I> catcher) where I : class, ILiveContexted {
            var i = Get<I>();
            if (i != null) {
                catcher(i);
                return;
            }
            
            bool wait = true;
            
            void Delayed(ILiveContexted item) {
                if (item is I _i) {
                    catcher?.Invoke(_i);
                    onAdd -= Delayed;
                    wait = false;
                } 
            }
            
            onAdd += Delayed;
        }

        public void CatchAll<I>(Action<I> catcher) where I : class, ILiveContexted {
            GetAll<I>().ForEach(catcher);
            
            void Delayed(ILiveContexted item) {
                if (item is I _i) catcher?.Invoke(_i);
            }
            
            onAdd += Delayed;
        }
        
        #endregion

        public void Destroy() {
            contexts.Remove(this);
            all.ToArray().ForEach(x => x.Kill());
            singletons.ForEach(s => s.OnKill());
            singletons.Clear();
            arguments.Clear();
        }

        #region Static
        
        static List<LiveContext> contexts = new List<LiveContext>();

        public static readonly LiveContext globalContext = new("Global");
        
        public static IEnumerable<LiveContext> Contexts => contexts;

        #endregion
    }

    public interface ILiveContextHolder {
        LiveContext GetContext();
    }
    
    public abstract class LiveContextSingleton {
        public string SingletonKey { get; private set; }
        
        public LiveContext context {get; private set; }
        
        public void Initialize(LiveContext context, string singletonKey) {
            if (this.context != null)
                return;
            this.context = context;
            SingletonKey = singletonKey;
            OnInitialize();
        }

        protected virtual void OnInitialize() {}
        
        public void Kill() {
            context.RemoveSingleton(this);
        }
        
        public virtual void OnKill() {}
    }
        
    public interface ILiveContexted {
        LiveContext context { get; set; }

        void Initialize();
        void Kill();
        bool EqualContent(ILiveContexted obj);
    }
}