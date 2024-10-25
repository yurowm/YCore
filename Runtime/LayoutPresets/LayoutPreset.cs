using System;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.Core;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.UI {
    public abstract class LayoutPreset : Behaviour {
        public abstract void OnScreenResize();
        
        [OnLaunch(int.MinValue)]
        static void OnLaunch() {
            App.onScreenResize?.Invoke();
            App.onScreenResize += () => DebugPanel.Log("Preset", "UI", GetCurrentLayout().ToText());
        }
            
        public override void Initialize() {
            base.Initialize();
            App.onScreenResize += OnScreenResize;
        }
    
        public override void OnKill() {
            base.OnKill();
            App.onScreenResize -= OnScreenResize;
        }
    
        void OnEnable() {
            OnScreenResize();
        }
        
        [Flags]
        public enum Layout {
            Landscape = 1 << 1,
            Portrait = 1 << 2,
            Phone = 1 << 3,
            Tablet = 1 << 4
        }
    
        public static Layout GetCurrentLayout() {
            Layout result = GameParameters.GetModule<GameParametersGeneral>()?.forceLayout ?? 0;
                
            if (result != 0)
                return result;
                
            var aspectRatio = 1f;
                
            #if UNITY_EDITOR
            if (Camera.main != null) 
                aspectRatio = Camera.main.aspect;
            else 
                aspectRatio = 1f * Screen.width / Screen.height;
            #else
            aspectRatio = 1f * Screen.width / Screen.height;
            #endif
                
            if (aspectRatio >= 1)
                result |= Layout.Landscape;
            else
                result |= Layout.Portrait;
                
            if (App.isTablet)
                result |= Layout.Tablet;
            else
                result |= Layout.Phone;
                
            return result;
        }
    }
    
    public abstract class LayoutPreset<PC, C> : LayoutPreset 
        where PC: LayoutPresetData<C>
        where C: Component {
        
        public List<PC> presets = new();
        
        C _target;
        
        public C Target {
            get {
                if (_target || gameObject.SetupComponent(out _target))
                    return _target;
                return null;
            }
        }

        public override void OnScreenResize() {
            if (Application.isPlaying && !isActiveAndEnabled) 
                return;
            
            var target = Target;
            
            if (!target) return;
            
            var currentLayout = GetCurrentLayout();
            
            presets
                .FirstOrDefaultFiltered(
                    r => currentLayout.HasFlag(r.layout),
                    r => r.layout.OverlapFlag(currentLayout))?
                .Read(target);
        }
    }

    [Serializable]
    public abstract class LayoutPresetData<C> where C : Component {
        public LayoutPreset.Layout layout;
        
        public abstract void Write(C fromComponent);
        public abstract void Read(C toComponent);
    }
}