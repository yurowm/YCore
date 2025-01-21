using UnityEngine.UI;
using Yurowm.Extensions;

namespace Yurowm.DebugTools {
    public class StringVariableUIBuilder : VariableUIBuilder<string> {
        public override MessageUI EmitMessage(DebugPanelUI debugPanelUI, DebugVariable<string> variable) {
            var messageUI = debugPanelUI.EmitMessageUI("InputMessageUI");

            if (messageUI.SetupComponent(out InputField inputUI)) {
                inputUI.onValueChanged.RemoveAllListeners();

                inputUI.contentType = InputField.ContentType.Standard;
                inputUI.text = variable.Get().ToString();
                inputUI.onValueChanged.AddListener(v => variable.Set(v));
            }
                
            return messageUI;
        }
    }
}