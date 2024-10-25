using System;
using UnityEngine;

namespace Yurowm {
    public class Trigger2DEvent : MonoBehaviour {
        #if PHYSICS_2D
        
        public Action<Collider2D> onEnter = delegate {};
        public Action<Collider2D> onStay = delegate {};
        public Action<Collider2D> onExit = delegate {};
        public Action<Collider2D, PhysicCollisionPhase> onEvent = delegate {};

        public void Rollout() {
            onEnter = delegate {};            
            onStay = delegate {};            
            onExit = delegate {}; 
            onEvent = delegate {};         
        }

        void OnTriggerEnter2D(Collider2D other) {
            onEnter.Invoke(other);
            onEvent.Invoke(other, PhysicCollisionPhase.Enter);
        }
        
        void OnTriggerStay2D(Collider2D other) {
            onStay.Invoke(other);
            onEvent.Invoke(other, PhysicCollisionPhase.Stay);
        }

        void OnTriggerExit2D(Collider2D other) {
            onExit.Invoke(other);
            onEvent.Invoke(other, PhysicCollisionPhase.Exit);
        }
        
        #endif
    }
}