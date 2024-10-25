using UnityEngine;

namespace Yurowm {
    public class StepRotationAnimator : BaseBehaviour {
        public float stepAngle = 30;
        public float frequency = 1;
        
        float lastTime = float.MinValue;

        void Update() {
            if (lastTime + 1f / frequency <= Time.unscaledTime) {
                lastTime = Time.unscaledTime;
                transform.Rotate(0, 0, stepAngle); 
            }    
        }
    }
}