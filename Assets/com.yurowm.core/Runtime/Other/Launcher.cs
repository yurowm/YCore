using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;

[assembly: Preserve]
namespace Yurowm.Utilities {
    public class OnLaunchAttribute : Attribute {
        readonly int _order;
        
        public OnLaunchAttribute(int order = 0) {
            _order = order;
        }

        static OnLaunchModifier[] launchModifiers = null;
        public static Action unload = delegate {};
        
        public static float ProgressCurrent;
        public static int Progress { get; private set; }
        public static int ProgressVolume { get; private set; }
        
        public static float ProgressValue {
            get {
                if (ProgressVolume <= 0)
                    return 0;
                return (ProgressCurrent.Clamp01() + Progress) / ProgressVolume;
            }
        } 
            

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void PreLaunch() {
            launchModifiers = Utils.FindInheritorTypes<OnLaunchModifier>(true)
                            .Select(t => {
                                try {
                                    return (OnLaunchModifier) Activator.CreateInstance(t);
                                } catch (Exception e) {
                                    UnityEngine.Debug.LogException(e);
                                }
                                return null;
                            })
                            .NotNull().ToArray();
            
            launchModifiers.ForEach(m => {
                try {
                    m.BeforeSceneLoaded();
                } catch (Exception e) {
                    UnityEngine.Debug.LogException(e);
                }
            });
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Launch() {
            Utils.SetMainThread();
            Launching().Forget();
        }

        static async UniTask Launching() {
            var launches = UnityUtils.GetAllMethodsWithAttribute<OnLaunchAttribute>(
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .ToDictionary();
            
            ProgressVolume = launches.Count;
            Progress = 0;
            
            foreach (var modifier in launchModifiers)
                await modifier.PreLoad();

            var report = new StringBuilder();
            
            using (var executionTimer = new ExecutionTimer("OnLaunch", r => report.Append(r))) {
                
                executionTimer.Flash("Reflection");
                
                var parameters = Array.Empty<object>();
                
                unload?.Invoke();
                unload = delegate {};

                foreach (var l in launches.OrderBy(l => l.Value._order)) {
                    var name = l.Key.DeclaringType?.FullName;
                    object result = null;
                    try {
                        result = l.Key.Invoke(null, parameters);
                    } catch (Exception e) {
                        Debug.LogException(e);
                    }
    
                    if (result is UniTask task)
                        await task;
                    
                    Progress++;
                    ProgressCurrent = 0;
                    
                    executionTimer.Flash($"{name}:{l.Key.Name}");
                    
                    await UniTask.Yield();
                }
            }
            
            Debug.Log(report.ToString());
            
            foreach (var modifier in launchModifiers)
                await modifier.PostLoad();
        }
    }

    [Preserve]
    public abstract class OnLaunchModifier {
        public abstract void BeforeSceneLoaded();
        public abstract UniTask PreLoad();
        public abstract UniTask PostLoad();
    }
    
    public static class OnceAccess {
        static List<string> keys = new();
        
        public static bool GetAccess(string key) {
            if (keys.Contains(key))
                return false;
            keys.Add(key);
            return true;
        }
    }
}
