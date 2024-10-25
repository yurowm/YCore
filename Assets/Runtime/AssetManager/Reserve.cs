using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.ContentManager;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.Utilities;
using Object = UnityEngine.Object;

namespace Yurowm {
    public static class Reserve {

        static Transform root;
        
        static Transform Root {
            get {
                if (!root) {
                    root = new GameObject().transform;
                    root.name = "Reserved";
                    pool.NotNull().ForEach(r => Object.Destroy(r.gameObject));
                    pool.Clear();
                    
                    OnLaunchAttribute.unload += () => pool.Clear();
                }
                return root;
            }
        }
        
        static List<ContextedBehaviour> pool = new();

        static ReserveParameters _parameters;
        static ReserveParameters parameters {
            get {
                _parameters ??= GameParameters.GetModule<ReserveParameters>();
                return _parameters;
            }
        }
        
        public static void Put(IReserved reserved) {
            if (reserved is ContextedBehaviour behaviour) {
                if (!parameters.active) {
                    Kill(behaviour);
                    return;
                }
                if (!pool.Contains(behaviour)) {
                    reserved.Rollout();
                
                    behaviour.gameObject.SetActive(false);

                    behaviour.transform.SetParent(Root);
                    behaviour.transform.Reset();

                    pool.Add(behaviour);
                    DebugPanel.Log("Reserve Pool", "Profiler", pool.Count);
                }
            } else
                throw new System.Exception("Only ContextedBehaviour items can be reserved");
        }

        public static L Take<L>(L reference, LiveContext context = null) where L : ContextedBehaviour {
            return Take<L>(x => x.original == reference.original, context);
        }

        public static L Take<L>(string name, LiveContext context = null) where L : ContextedBehaviour {
            return Take<L>(x => x.name == name, context);
        }

        public static L Take<L>(Func<ContextedBehaviour, bool> condition, LiveContext context = null) where L : ContextedBehaviour {
            L result = pool.CastIfPossible<L>().LastOrDefault(condition.Invoke);
            
            if (result == null) return null;
            
            pool.Remove(result);
            result.gameObject.SetActive(true);
            result.transform.SetParent(null);
            context?.Add(result);
            
            return result;
        }

        public static void Clear<T>() where T: ContextedBehaviour {
            pool.RemoveAll(r => {
                if (r == null || r.gameObject == null)
                    return true;
                if (r is T) {
                    Kill(r);
                    return true;
                }
                return false;
            });
        }
        
        static void Kill(ContextedBehaviour behaviour) {
            behaviour.KillCompletely();
        }

        #region Preparing

        static Dictionary<ContextedBehaviour, int> prepareOrder = new Dictionary<ContextedBehaviour, int>();

        static IEnumerator prepareLogic;
        static IEnumerator PrepareLogic() {
            DelayedAccess frameSkip = new DelayedAccess(1f / 30f);
            
            var created = new List<ContextedBehaviour>();
            var inPool = new List<ContextedBehaviour>();

            using var timer = new ExecutionTimer("Preparing", Debug.Log);
            
            while (true) {
                var reference = prepareOrder.Keys.FirstOrDefault();
                if (!reference) break;
                
                int targetCount = prepareOrder[reference];
                if (targetCount <= 0) {
                    prepareOrder.Remove(reference);
                    continue;
                }
                
                inPool.Clear();
                inPool.AddRange(AllByReference(reference));
                
                if (inPool.Count >= targetCount){
                    prepareOrder.Remove(reference);
                    continue;
                }
                
                int countToEmit = targetCount - inPool.Count;
                
                // Removing reserved elements to prevent emitting them below
                pool.RemoveRange(inPool);
                
                created.Clear();
                
                while (countToEmit > 0) {
                    created.Add(AssetManager.Emit(reference));
                    countToEmit--;
                    if (frameSkip.GetAccess())
                        break;
                }
                
                pool.AddRange(inPool);
                
                created.Cast<IReserved>().ForEach(r => r.Prepare());
                created.Cast<IReserved>().ForEach(Put);
                
                if (countToEmit == 0)
                    prepareOrder.Remove(reference);
                else 
                    yield return null;
            }
            
            prepareLogic = null;
        }
        
        static IEnumerable<ContextedBehaviour> AllByReference(ContextedBehaviour reference) {
            foreach (var reserved in pool)
                if (reserved.original == reference.gameObject)
                    yield return reserved;
        }
        
        public static void Prepare(ContextedBehaviour content, int count) {
            if (count <= 0 || !(content is IReserved)) return;
            
            if (!parameters.active) return;
            
            prepareOrder[content] = Mathf.Max(count, prepareOrder.Get(content));
            
            if (prepareLogic == null) {
                prepareLogic = PrepareLogic();
                prepareLogic.Run();
            }
        }

        #endregion
    }
    
    public class ReserveParameters: GameParameters.Module {
        public bool active = true;
        public bool levelPoolManagment = true;
        public bool LevelPoolManagment => active && levelPoolManagment;

        public override string GetName() {
            return "Reserve";
        }

        public override void Serialize(IWriter writer) {
            writer.Write("active", active);
            if (active)
                writer.Write("levelPoolManagment", levelPoolManagment);
        }

        public override void Deserialize(IReader reader) {
            reader.Read("active", ref active);
            if (active)
                reader.Read("levelPoolManagment", ref levelPoolManagment);
        }
    }
}