using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.UI {
    public static class UIRefresh {
        [OnLaunch(Behaviour.INITIALIZATION_ORDER + 1)]
        static void OnLaunch() {
            Logic().Forget();
            
            OnLaunchAttribute.unload += () => {
                uiRefreshes.Clear();
                onCompleteRefresh = null;
            };
        }
        
        static List<IUIRefresh> uiRefreshes = new List<IUIRefresh>();
        static bool refreshExecuting = false;
        static Action onCompleteRefresh = null;
        
        static DateTime? nextInvokeTime;
        
        static async UniTask Logic() {
            while (true) {
                if (nextInvokeTime.HasValue && nextInvokeTime.Value <= DateTime.Now) {
                    nextInvokeTime = null;
                    Invoke();
                }
                await UniTask.Yield();
            }
        }

        public static void Add(IUIRefresh refresh) {
            if (refreshExecuting)
                onCompleteRefresh += () => {
                    uiRefreshes.Add(refresh);
                    refresh.Refresh();
                };
            else {
                uiRefreshes.Add(refresh);
                refresh.Refresh();
            }
        }

        public static void Remove(IUIRefresh refresh) {
            if (refreshExecuting)
                onCompleteRefresh += () => uiRefreshes.Remove(refresh);
            else
                uiRefreshes.Remove(refresh);
        }
        
        static List<Muter> muters = new();

        static bool mute => !muters.IsEmpty();

        public static void InvokeDelayed(float delay = 0) {
            var nextTime = DateTime.Now.AddSeconds(delay);
            if (!nextInvokeTime.HasValue || nextInvokeTime.Value > nextTime)
                nextInvokeTime = nextTime;
        }
        
        static int refreshCount = 0;
        
        public static void Invoke() {
            if (!mute && !refreshExecuting) {
                refreshExecuting = true;

                uiRefreshes.ForEach(r => {
                    if (r.visible)
                        r.Refresh();
                });

                refreshExecuting = false;

                onCompleteRefresh?.Invoke();
                onCompleteRefresh = null;
                
                DebugPanel.Log("Refresh", "UI", refreshCount++);
            }
        }

        class Muter: IDisposable {
            
            public Muter() {
                muters.Add(this);
            }
            
            public void Dispose() {
                muters.Remove(this);
                InvokeDelayed();
            }
        }
        
        public static IDisposable Mute() => new Muter();
    }
    
    public interface IUIRefresh {
        /// <summary>
        /// Refresher will ignore this object if this property returns 'false'
        /// </summary>
        bool visible { get; }
        /// <summary>
        /// Logic of UI refreshing
        /// </summary>
        void Refresh();
    }

    public class UIRefreshAction : IUIRefresh {
        Action _refresh;
        Func<bool> _enabled;

        public UIRefreshAction(Action refresh, Func<bool> enabled = null) {
            _refresh = refresh;
            _enabled = enabled;
        }

        public bool visible => _enabled?.Invoke() ?? true;
        
        public void Refresh() {
            _refresh.Invoke();
        }
    }
}