using System;
using UnityEngine;

namespace Yurowm.UI {
    public class LayoutPresetRectTransform : LayoutPreset<LayoutPresetDataRectTransform, RectTransform> { }
    
    [Serializable]
    public class LayoutPresetDataRectTransform : LayoutPresetData<RectTransform> {
        public Vector2 pivot;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 offsetMin;
        public Vector2 offsetMax;
        public Quaternion rotation;
        public Vector3 localScale;
            
        public override void Read(RectTransform toComponent) {
            toComponent.pivot = pivot;
            
            toComponent.anchorMin = anchorMin;
            toComponent.anchorMax = anchorMax;
            
            toComponent.offsetMin = offsetMin;
            toComponent.offsetMax = offsetMax;
            
            toComponent.rotation = rotation;
            toComponent.localScale = localScale;
        }
            
        public override void Write(RectTransform fromComponent) {
            pivot = fromComponent.pivot;
            
            anchorMin = fromComponent.anchorMin;
            anchorMax = fromComponent.anchorMax;
            
            offsetMin = fromComponent.offsetMin;
            offsetMax = fromComponent.offsetMax;
            
            rotation = fromComponent.rotation;
            localScale = fromComponent.localScale;
        }
    }
}