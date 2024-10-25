using UnityEngine.UI;
using Yurowm.Extensions;

namespace Yurowm.DebugTools {
    public class FloatVariableUIBuilder : VariableUIBuilder<float> {
        public override MessageUI EmitMessage(DebugPanelUI debugPanelUI, DebugVariable<float> variable) {
            if (variable is DebugVariableRange<float> range) {
                var messageUI = debugPanelUI.EmitMessageUI("SliderMessageUI");

                messageUI.SetupChildComponent(out Text valueUI);
                
                if (messageUI.SetupComponent(out Slider sliderUI)) {
                    sliderUI.onValueChanged.RemoveAllListeners();

                    sliderUI.wholeNumbers = false;
                    sliderUI.minValue = range.min;
                    sliderUI.maxValue = range.max;
                    sliderUI.value = range.Get();
                    if (valueUI)
                        valueUI.text = range.Get().ToString();
                    sliderUI.onValueChanged.AddListener(v => {
                        range.Set(v);
                        if (valueUI)
                            valueUI.text = range.Get().ToString();
                    });
                }
                
                return messageUI;
            } else {
                var messageUI = debugPanelUI.EmitMessageUI("InputMessageUI");

                if (messageUI.SetupComponent(out InputField inputUI)) {
                    inputUI.onValueChanged.RemoveAllListeners();

                    inputUI.contentType = InputField.ContentType.DecimalNumber;
                    inputUI.text = variable.Get().ToString();
                    inputUI.onValueChanged.AddListener(v => {
                        if (float.TryParse(v, out var result))
                            variable.Set(result);
                    });
                }
                
                return messageUI;
            }
        }
    }
    
    public class IntVariableUIBuilder : VariableUIBuilder<int> {
        public override MessageUI EmitMessage(DebugPanelUI debugPanelUI, DebugVariable<int> variable) {
            if (variable is DebugVariableRange<int> range) {
                var messageUI = debugPanelUI.EmitMessageUI("SliderMessageUI");

                messageUI.SetupChildComponent(out Text valueUI);
                
                if (messageUI.SetupComponent(out Slider sliderUI)) {
                    sliderUI.onValueChanged.RemoveAllListeners();

                    sliderUI.wholeNumbers = true;
                    sliderUI.minValue = range.min;
                    sliderUI.maxValue = range.max;
                    sliderUI.value = range.Get();
                    if (valueUI)
                        valueUI.text = range.Get().ToString();
                    sliderUI.onValueChanged.AddListener(v => {
                        range.Set(v.RoundToInt());
                        if (valueUI)
                            valueUI.text = range.Get().ToString();
                    });
                }
                
                return messageUI;
            } else {
                var messageUI = debugPanelUI.EmitMessageUI("InputMessageUI");

                if (messageUI.SetupComponent(out InputField inputUI)) {
                    inputUI.onValueChanged.RemoveAllListeners();

                    inputUI.contentType = InputField.ContentType.IntegerNumber;
                    inputUI.text = variable.Get().ToString();
                    
                    inputUI.onSubmit.AddListener(v => {
                        if (int.TryParse(v, out var result))
                            variable.Set(result);
                    });
                }
                
                return messageUI;
            }
        }
    }
}