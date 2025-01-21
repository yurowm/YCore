using System;
using UnityEngine;
using Yurowm.Core;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.UI {
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class RectTransformLerp : UnityEngine.EventSystems.UIBehaviour, IOnAnimateHandler {
        
        public RectTransform rectStart;

        public RectTransform rectEnd {
            get {
                if (endIndex >= 0 && endIndex < rectsEnd.Length)
                    return rectsEnd[endIndex];
                return null;
            }
        }

        public RectTransform[] rectsEnd;
        public int endIndex = 0;
        
        RectTransform _rectTransform;
        RectTransform rectTransform {
            get {
                if (!_rectTransform)
                    this.SetupComponent(out _rectTransform);
                return _rectTransform;
            }   
        }

        [SerializeField]
        [Range(0, 1)]
        float time = 0;
        
        public Options options;
        
        [Flags]
        public enum Options {
            RefreshOnScreenResize = 1 << 0
        }
        
        public float Time {
            get => time;
            set {
                value = value.Clamp01();
                if (time == value) return;
                time = value;
                Refresh();
            }
        }
        
        public EasingFunctions.Easing easing = EasingFunctions.Easing.Linear;

        #if UNITY_EDITOR
        protected override void OnValidate() {
            base.OnValidate();
            Refresh();    
        }
        #endif

        protected override void OnEnable() {
            base.OnEnable();
            Refresh();
        }

        protected override void OnDisable() {
            base.OnDisable(); 
            App.onScreenResize -= Refresh;
        }

        protected override void OnDidApplyAnimationProperties() {
            base.OnDidApplyAnimationProperties();
            Refresh();    
        }

        public void OnAnimate() {
            Refresh();
        }

        void Refresh() {
            if (!this)
                return;
            
            App.onScreenResize -= Refresh;

            if (isActiveAndEnabled) {
                var t = easing.Evaluate(time.Clamp01());
                
                if (!rectStart || !rectEnd) return;
                if (rectStart.parent != rectEnd.parent) return;
                if (rectTransform.parent != rectStart.parent) return;
                
                rectTransform.pivot = Vector2.LerpUnclamped(rectStart.pivot, rectEnd.pivot, t);
                
                rectTransform.anchorMin = Vector2.LerpUnclamped(rectStart.anchorMin, rectEnd.anchorMin, t);
                rectTransform.anchorMax = Vector2.LerpUnclamped(rectStart.anchorMax, rectEnd.anchorMax, t);
                
                rectTransform.offsetMin = Vector2.LerpUnclamped(rectStart.offsetMin, rectEnd.offsetMin, t);
                rectTransform.offsetMax = Vector2.LerpUnclamped(rectStart.offsetMax, rectEnd.offsetMax, t);
                
                rectTransform.rotation = Quaternion.LerpUnclamped(rectStart.rotation, rectEnd.rotation, t);
                rectTransform.localScale = Vector3.LerpUnclamped(rectStart.localScale, rectEnd.localScale, t);
            }
            
            // перемещаем вызов в конец, чтобы быть уверенным, что все трансформы уже обновлены
            if (options.HasFlag(Options.RefreshOnScreenResize))
                App.onScreenResize += Refresh;
        }
    }
}