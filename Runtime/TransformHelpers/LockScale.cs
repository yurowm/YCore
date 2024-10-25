using UnityEngine;

namespace Yurowm {
    public class LockScale : BaseBehaviour {
        public bool initialScale = true;
        public Vector3 scale;
        
        void Awake() {
            if (initialScale)
                scale = transform.lossyScale;
        }

        void Update() {
            if (transform.lossyScale == scale) return;
            transform.localScale = transform.parent.InverseTransformVector(scale);
        }
    }
}