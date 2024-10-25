#if PHYSICS_3D
using System;
using UnityEngine;
#endif

namespace Yurowm {
    public class Collision3DEvent : BaseBehaviour {
        #if PHYSICS_3D
        
        public Action<Collision> onEnter = null;
        public Action<Collision> onStay = null;
        public Action<Collision> onExit = null;

        public void Rollout() {
            onEnter = null;            
            onStay = null;            
            onExit = null;            
        }

        void OnCollisionEnter(Collision other) {
            onEnter?.Invoke(other);
            
        }

        void OnCollisionStay(Collision other) {
            onStay?.Invoke(other);
        }

        void OnCollisionExit(Collision other) {
            onExit?.Invoke(other);
        }
        
        #endif
    }
}
