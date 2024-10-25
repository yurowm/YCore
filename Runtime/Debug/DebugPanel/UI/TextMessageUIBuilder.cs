using System;
using UnityEngine.UI;
using Yurowm.Extensions;

namespace Yurowm.DebugTools {
    public class TextMessageUIBuilder : MessageUIBuilder {
        
        public override bool IsSuitableFor(Type messageType) {
            if (messageType == typeof(DebugPanel.TextMessage)) return true;
            if (messageType == typeof(DebugPanel.OtherMessage)) return true;
            if (messageType == typeof(DebugPanel.NullMessage)) return true;
            return false;
        }

        protected override MessageUI EmitMessageUI(DebugPanel.Entry entry) {
            
            var messageUI = debugPanelUI.EmitMessageUI("TextMessageUI");
            
            if (messageUI.SetupComponent(out Text textUI)) {
                textUI.color = DebugPanel.GroupToColor(entry.group);
                
                var text = "";
                
                switch (entry.message) {
                    case DebugPanel.TextMessage tm: text = tm.Value; break;
                    case DebugPanel.OtherMessage om: text = om.text; break;
                    case DebugPanel.NullMessage nm: text = "null"; break;
                }

                textUI.text = text; 
            }
            
            return messageUI;
        }
    }
}