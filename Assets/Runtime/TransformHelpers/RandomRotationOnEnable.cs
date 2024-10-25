using UnityEngine;
using Yurowm.Utilities;

namespace Yurowm {
    public class RandomRotationOnEnable : BaseBehaviour {
        
        public bool local = true;
        public bool multiply = false;
        
        public FloatRange xRange = new(0, 360);
        public FloatRange yRange = new(0, 360);
        public FloatRange zRange = new(0, 360);
        
        void OnEnable() {
            Randomize();
        }

        public void Randomize() {
            var rotation = Quaternion.Euler(
                       YRandom.main.Range(xRange),
                       YRandom.main.Range(yRange),
                       YRandom.main.Range(zRange));
            
            if (multiply) {
                if (local) 
                    rotation = transform.localRotation * rotation;
                else
                    rotation = transform.rotation * rotation;
            }
            
            if (local) 
                transform.localRotation = rotation;
            else
                transform.rotation = rotation;
        }
    }
}