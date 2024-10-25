using System;
using UnityEngine;
using UnityEngine.UI;

namespace Yurowm.UI {
    public class LayoutPresetCanvasResolution : LayoutPreset<LayoutPresetDataCanvasResolution, CanvasScaler> { }
    
    [Serializable]
    public class LayoutPresetDataCanvasResolution : LayoutPresetData<CanvasScaler> {
        public Vector2 resolution;
        [Range(0, 1)]
        public float match;
            
        public override void Read(CanvasScaler toComponent) {
            toComponent.referenceResolution = resolution;
            toComponent.matchWidthOrHeight = match;
        }
            
        public override void Write(CanvasScaler fromComponent) {
            resolution = fromComponent.referenceResolution;
            match = fromComponent.matchWidthOrHeight;
        }
    }
}