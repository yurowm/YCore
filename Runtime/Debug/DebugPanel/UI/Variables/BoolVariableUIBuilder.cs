using UnityEngine.UI;
using Yurowm.Extensions;

namespace Yurowm.DebugTools {
    public class BoolVariableUIBuilder : VariableUIBuilder<bool> {
        public override MessageUI EmitMessage(DebugPanelUI debugPanelUI, DebugVariable<bool> variable) {
            var messageUI = debugPanelUI.EmitMessageUI("ToggleMessageUI");

            if (messageUI.SetupComponent(out Toggle toggleUI)) {
                toggleUI.onValueChanged.RemoveAllListeners();

                toggleUI.isOn = variable.Get();
                toggleUI.onValueChanged.AddListener(v => variable.Set(v));
            }
                
            return messageUI;
        }
    }
}