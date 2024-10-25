using System;
using UnityEngine;

namespace Yurowm.Utilities {
    public class TransformConnector : Behaviour {
        
        public bool positionContorl;
        public bool rotationContorl;
        public bool scaleContorl;
        
        public Action onMove = null;
        public Action onRotate = null;
        public Action onScale = null;

        Vector3 position;
        Vector3 scale;
        Quaternion rotation;

        public override void Initialize() {
            base.Initialize();
            position = transform.position;
            rotation = transform.rotation;
        }

        void LateUpdate() {
            if (positionContorl && position != transform.position) {
                position = transform.position;
                onMove?.Invoke();
            }
            if (rotationContorl && rotation != transform.rotation) {
                rotation = transform.rotation;
                onRotate?.Invoke();
            }
            if (scaleContorl && scale != transform.localScale) {
                scale = transform.localScale;
                onScale?.Invoke();
            }
        }
    }
}