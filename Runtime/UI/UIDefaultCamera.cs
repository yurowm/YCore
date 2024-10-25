using UnityEngine;

namespace Yurowm.UI {
    [RequireComponent(typeof(Camera))]
    public class UIDefaultCamera : Behaviour {
        public override void Initialize() {
            base.Initialize();
            SetUICamera.SetDefault(GetComponent<Camera>());    
        }
    }
}