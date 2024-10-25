using System;
using UnityEngine.UI;
using Yurowm.Extensions;

namespace Yurowm.ComposedPages {
    public class ComposedToggle : ComposedElementTitled {

        public Toggle toggle;

        public Action<bool> onValueChanged = delegate{};
        
        ContentAnimator animator;
        
        public override void Initialize() {
            base.Initialize();
            toggle.onValueChanged.AddListener(OnValueChanged);
            
            this.SetupChildComponent(out animator);
        }
        
        public void SetValue(bool value, bool withAnimation = false) {
            toggle.isOn = value;
            
            Animate(value, !withAnimation);
        }

        public bool GetValue() {
            return toggle.isOn;
        }
        
        void Animate(bool value, bool immediate) {
            if (!animator) return;
            
            string clip = value ? "On" : "Off";
            
            if (immediate)
                animator.RewindEnd(clip);
            else
                animator.Play(clip);
            
        }

        void OnValueChanged(bool value) {
            onValueChanged?.Invoke(value);
            Animate(value, false);
        }

        #region IReserved

        public void Rollout() {
            onValueChanged = null;
            SetTitle("");
        }

        #endregion
    }
}
