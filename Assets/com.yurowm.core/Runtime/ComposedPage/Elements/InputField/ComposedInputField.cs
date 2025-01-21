using System;
using TMPro;

namespace Yurowm.ComposedPages {
    public class ComposedInputField : ComposedElementTitled {
        public TMP_InputField field;
        
        public Action<string> onValueChanged;
        
        public void SetInputType(TMP_InputField.InputType inputType) {
            field.inputType = inputType;
        }

        public override void Initialize() {
            base.Initialize();
            field.onValueChanged.AddListener(OnValueChanged);
        }
        
        void OnValueChanged(string value) {
            onValueChanged?.Invoke(value);
        }

        public void SetText(string text) {
            field.text = text;
        }

        public string GetText() {
            return field.text;
        }
        
        public override void Rollout() {
            base.Rollout();
            SetInputType(TMP_InputField.InputType.Standard);
            onValueChanged = null;
            SetText("");
        }
    }
}