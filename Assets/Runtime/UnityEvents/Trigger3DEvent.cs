#if PHYSICS_3D
using System;
using UnityEngine;
#endif

namespace Yurowm {
    public class Trigger3DEvent : BaseBehaviour {
        #if PHYSICS_3D
        
        public Action<Collider> onEnter = delegate {};
        public Action<Collider> onStay = delegate {};
        public Action<Collider> onExit = delegate {};
        public Action<Collider, PhysicCollisionPhase> onEvent = delegate {};

        public void Rollout() {
            onEnter = delegate {};            
            onStay = delegate {};            
            onExit = delegate {}; 
            onEvent = delegate {};         
        }

        void OnTriggerEnter(Collider other) {
            onEnter.Invoke(other);
            onEvent.Invoke(other, PhysicCollisionPhase.Enter);
        }
        
        void OnTriggerStay(Collider other) {
            onStay.Invoke(other);
            onEvent.Invoke(other, PhysicCollisionPhase.Stay);
        }

        void OnTriggerExit(Collider other) {
            onExit.Invoke(other);
            onEvent.Invoke(other, PhysicCollisionPhase.Exit);
        }
        
        #endif
    }
}