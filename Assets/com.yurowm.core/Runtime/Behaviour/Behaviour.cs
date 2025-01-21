using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yurowm.ContentManager;
using Yurowm.Extensions;
using Yurowm.Jobs;
using Yurowm.UI;
using Yurowm.Utilities;

namespace Yurowm {
    public abstract class Behaviour : BaseBehaviour, IBehaviour {

        public const int INITIALIZATION_ORDER = -1000;

        [OnLaunch(INITIALIZATION_ORDER)]
        public static void InitializeOnLoad() {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var list = new List<IBehaviour>();
            foreach (GameObject root in roots)
            foreach (var comp in root.GetComponentsInChildren<IBehaviour>(true))
                if (comp.ContextTag.registerOnLaunch)
                    list.Add(comp);

            list.ForEachConsistently(
                b => Register(b, false), 
                b => b.Initialize());
            
            OnLaunchAttribute.unload += () => {
                behaviourContext.Destroy();
                behaviourContext = new LiveContext("Behaviours");
            };
        }
        
        #region ISelfUpdate 
        public bool readyForUpdate {
            get => isActiveAndEnabled;
            set => enabled = value;
        }

        public int updateID { get; set; }

        public LiveContext context { get; set; }

        public void MakeUnupdated() {
            updateID = -1;
        }

        public void SureToUpdate(Updater updater) {
            if (this is ISelfUpdate update && updateID != updater.frameID) {
                update.UpdateFrame(updater);
                update.updateID = updater.frameID;
            }
        }
        #endregion

        #region Context
        public ContextTag ContextTag => contextTag;
        public ContextTag contextTag;

        public static LiveContext behaviourContext = new LiveContext("Behaviours");

        public static IEnumerable<B> GetAll<B>() where B : class, IBehaviour {
            return behaviourContext.GetAll<B>();
        }
        public static IEnumerable<B> GetAllByID<B>(string id) where B : class, IBehaviour {
            return FindAll<B>(b => b.ContextTag.ID == id);
        }
        
        public static B Get<B>() where B : class, IBehaviour {
            return behaviourContext.Get<B>();
        }
        
        public static B GetByID<B>(string id) where B : class, IBehaviour {
            return Find<B>(b => b.ContextTag.ID == id);
        }

        public static IEnumerable<B> FindAll<B>(Func<B, bool> predicate) where B : class, IBehaviour {
            return behaviourContext.GetAll(predicate);
        }

        public static B Find<B>(Func<B, bool> predicate) where B : class, IBehaviour {
            return behaviourContext.Get(predicate);
        }
        #endregion
        
        public bool visible => isActiveAndEnabled;

        protected virtual void Awake() {
            Register(this);    
        }

        public static void Register(IBehaviour behaviour, bool initialize = true) {
            if (!behaviourContext.Contains(behaviour)) {
                behaviourContext.Add(behaviour, initialize);
                behaviour.OnRegister();
                if (behaviour is ISelfUpdate update) JobSystem.Subscribe<SelfUpdateJob>(update);
                if (behaviour is IUIRefresh refresh) UIRefresh.Add(refresh);
            }
        } 
        
        public static void Unregister(IBehaviour behaviour) {
            if (behaviourContext.Contains(behaviour)) {
                
                if (behaviour is IUIRefresh refresh) UIRefresh.Remove(refresh);
                JobSystem.Unsubscribe(behaviour);

                behaviourContext.Remove(behaviour);
            }
        } 

        protected void OnDestroy() {
            if (!killed) OnKill();
        }

        public virtual void OnRegister() {}
        public virtual void Initialize() {}

        public virtual void OnKill() {
            if (killed) return;
            
            killed = true;
                    
            Unregister(this);
        }


        #region ILiveContexted
        bool killed = false;
        public void Kill() {
            if (!killed) OnKill();
            Destroy(gameObject);
        }

        public bool EqualContent(ILiveContexted obj) {
            return Equals(obj);
        }
        #endregion
    }
    
    [Serializable]
    public class ContextTag {
        public string ID = "";
        public bool registerOnLaunch = false;
    }
    
    public interface IBehaviour : ILiveContexted {
        
        ContextTag ContextTag {get;}
        void OnRegister();
    }
    
}