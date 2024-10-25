using UnityEngine;

namespace Yurowm {
    public class LockRotation : BaseBehaviour {
        void LateUpdate() {
            transform.rotation = Quaternion.identity;
        }
    }
}