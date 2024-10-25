using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yurowm.Extensions;
using Yurowm.UI;
using Yurowm.Utilities;
using IUIRefresh = Yurowm.UI.IUIRefresh;

namespace Yurowm.ContentManager {
    public abstract class ContextedBehaviour : BaseBehaviour, ILiveContexted {
        [OnLaunch()]
        static void InitializeOnLoad() {
            SceneManager.GetActiveScene().GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<ContextedBehaviour>(true))
                .ForEach(c => {
                    if (!c.isInitialized)
                        c.Initialize();
                });
        }
        
        public LiveContext context { get; set; }

        public bool active {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }
        
        internal GameObject _original = null;
        public GameObject original => _original;

        public Action onKill;
        
        public void Kill() {
            context?.Remove(this);
            
            onKill?.Invoke();
            onKill = null;
            OnKill();
            
            if (!this) return;
            
            if (this is IReserved reserved)
                Reserve.Put(reserved);
            else {
                if (this is IUIRefresh uiRefresh)
                    UIRefresh.Remove(uiRefresh);
                Destroy(gameObject);
            }
        }
        
        public void KillCompletely() {
            context?.Remove(this);
            
            OnKill();
            if (gameObject == null) return;
            
            if (this is IUIRefresh uiRefresh)
                UIRefresh.Remove(uiRefresh);
            
            Destroy(gameObject);
        }

        public bool EqualContent(ILiveContexted obj) {
            if (!(obj is ContextedBehaviour content)) return false;
            if (!content) return false;
            if (content == this) return true;
            if (content._original && _original) return content._original == _original;
            return content._original == gameObject || _original == content.gameObject;
        }

        #region Virtual
        [System.NonSerialized]
        public bool isInitialized = false;
        public virtual void Initialize() {
            isInitialized = true;
            
            if (this is IUIRefresh uiRefresh)
                UIRefresh.Add(uiRefresh);
        }
        public virtual void OnKill() {}
        #endregion
    }

    public interface IReserved {
        void Rollout();
        void Prepare();
    }
}