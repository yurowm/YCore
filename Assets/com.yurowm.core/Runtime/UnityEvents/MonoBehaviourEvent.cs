using System;

namespace Yurowm {
    public class MonoBehaviourEvent : BaseBehaviour {
        public Action onEnable = null;
        public Action onDisable = null;
        public Action onAwake = null;
        public Action onApplicationQuit = null;

        public void Rollout() {
            onEnable = null;            
            onDisable = null;            
            onAwake = null;            
            onApplicationQuit = null;            
        }

        void OnEnable() {
            onEnable?.Invoke();
        }
        
        void OnDisable() {
            onDisable?.Invoke();
        }

        void Awake() {
            onAwake?.Invoke();
        }

        void OnApplicationQuit() {
            onApplicationQuit?.Invoke();
        }
    }
}