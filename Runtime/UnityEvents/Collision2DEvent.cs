using System;
using UnityEngine;

namespace Yurowm {
    public class Collision2DEvent : BaseBehaviour {

        #if PHYSICS_2D
        
        public Action<Collision2D> onEnter = delegate {};
        public Action<Collision2D> onStay = delegate {};
        public Action<Collision2D> onExit = delegate {};
        public Action<Collision2D, PhysicCollisionPhase> onEvent = delegate {};

        public void Rollout() {
            onEnter = delegate {};            
            onStay = delegate {};            
            onExit = delegate {}; 
            onEvent = delegate {};
        }
    
        void OnCollisionEnter2D(Collision2D other) {
            onEnter.Invoke(other);
            onEvent.Invoke(other, PhysicCollisionPhase.Enter);
        }
        
        void OnCollisionStay2D(Collision2D other) {
            onStay.Invoke(other);
            onEvent.Invoke(other, PhysicCollisionPhase.Stay);
        }

        void OnCollisionExit2D(Collision2D other) {
            onExit.Invoke(other);
            onEvent.Invoke(other, PhysicCollisionPhase.Exit);
        }
        
        #endif
    }
    
    public enum PhysicCollisionPhase {
        Enter,
        Stay,
        Exit
    }
}
