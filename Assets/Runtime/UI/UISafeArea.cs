using System;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.Core;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.UI {
    [RequireComponent(typeof(RectTransform))]
    public class UISafeArea : MonoBehaviour {
        
        public RectOffsetFloat zeroOffset = new(0, 0, -10, 20);
        
        void OnEnable() {
            App.onScreenResize += ApplySafeArea;    
            ApplySafeArea();    
        }
        
        void OnDisable() {
            App.onScreenResize -= ApplySafeArea;
        }

        void ApplySafeArea() {
            var rect = transform as RectTransform;
            
            if (!rect) return;

            var safeArea = App.safeOffset;
            var full = new Vector2(Screen.width, Screen.height);

            foreach (var fix in SafeAreaFix.GetAll()) 
                safeArea = fix.Invoke(safeArea);
            
            var min = new Vector2(
                safeArea.Left / full.x,
                safeArea.Bottom / full.y);
            var max = new Vector2(
                1f - safeArea.Right / full.x, 
                1f - safeArea.Top / full.y);
            
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = default;
            rect.offsetMax = default;
            
            if (safeArea.Left == 0) 
                rect.offsetMin = rect.offsetMin.ChangeX(zeroOffset.Left);
            
            if (safeArea.Right == 0) 
                rect.offsetMax = rect.offsetMax.ChangeX(zeroOffset.Right);
            
            if (safeArea.Bottom == 0)
                rect.offsetMin = rect.offsetMin.ChangeY(zeroOffset.Bottom);
            
            if (safeArea.Top == 0) 
                rect.offsetMax = rect.offsetMax.ChangeY(zeroOffset.Top);
        }
        
    }
    
    public class SafeAreaFix {
        static List<SafeAreaFix> all = new();
        
        public Func<RectOffsetFloat, RectOffsetFloat> fix = null;
        
        public static SafeAreaFix Make() {
            var instance = new SafeAreaFix();
            
            all.Add(instance);
            
            return instance;
        }
        
        public void Kill() {
            if (all.Remove(this))
                App.onScreenResize?.Invoke();
        }
        
        public void Apply() {
            if (fix != null)
                App.onScreenResize?.Invoke();
        }
        
        public static IEnumerable<Func<RectOffsetFloat, RectOffsetFloat>> GetAll() {
            foreach (var offset in all)
                if (offset.fix != null)
                    yield return offset.fix;
        }
    }
}