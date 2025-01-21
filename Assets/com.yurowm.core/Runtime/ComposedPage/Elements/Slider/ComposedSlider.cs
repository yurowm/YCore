using System;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.ContentManager;
using Yurowm.Utilities;

namespace Yurowm.ComposedPages {
    public class ComposedSlider : ComposedElementTitled, IReserved {

        public Slider slider;

        public Action<float> onValueChanged;

        public override void Initialize() {
            base.Initialize();
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        void OnValueChanged(float value) {
            onValueChanged?.Invoke(value);
        }

        void OnEnable() {
            slider.onValueChanged.Invoke(slider.value);
        }

        public void SetRange(IntRange range, int current) {
            slider.wholeNumbers = true;
            slider.minValue = range.min;
            slider.maxValue = range.max;
            slider.value = current;
	    }

        public void SetRange(FloatRange range, float current) {
            slider.wholeNumbers = false;
            slider.minValue = range.min;
            slider.maxValue = range.max;
            slider.value = current;
        }

        public int Int() {
            return Mathf.RoundToInt(slider.value);
        }

        public float Float() {
            return slider.value;
        }

        #region IReserved

        public override void Rollout() {
            onValueChanged = null;
        }

        #endregion
    }
}