using System;
using Yurowm.Extensions;

namespace Yurowm.DebugTools {

    public class ActionMessageUIBuilder : MessageUIBuilder {
        public override bool IsSuitableFor(Type messageType) {
            return messageType == typeof(DebugPanel.ActionMessage);
        }

        protected override MessageUI EmitMessageUI(DebugPanel.Entry entry) {
            var messageUI = debugPanelUI.EmitMessageUI("ActionMessageUI");
            
            if (messageUI.SetupComponent(out UnityEngine.UI.Button button))
                button.onClick
                    .SetSingleListner((entry.message as DebugPanel.ActionMessage).Value);

            return messageUI;
        }
    }
}