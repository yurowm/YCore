using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;
using Yurowm.Utilities;

namespace Yurowm.DebugTools {
    public class VariableMessageUIBuilder : MessageUIBuilder {
        
        
        static VariableUIBuilder[] all = Utils
            .FindInheritorTypes<VariableUIBuilder>(true, true)
            .Where(t => t.IsInstanceReadyType())
            .Select(Activator.CreateInstance)
            .Cast<VariableUIBuilder>()
            .ToArray();
        
        static Dictionary<Type, VariableUIBuilder> byType = new Dictionary<Type, VariableUIBuilder>();
        
        static VariableUIBuilder Get(Type type) {
            if (!byType.TryGetValue(type, out var editor)) {
                editor = all.FirstOrDefault(d => d.IsSuitableFor(type));
                byType.Add(type, editor);
            }
            
            return editor;
        }
        
        public override bool IsSuitableFor(Type messageType) {      
            return typeof(DebugVariableMessage).IsAssignableFrom(messageType);
        }

        protected override MessageUI EmitMessageUI(DebugPanel.Entry entry) {
            if (entry.message is DebugVariableMessage debugVariable)
                return Get(debugVariable.variable.GetVariableType())?.CastAndEmitMessage(debugPanelUI, entry);
            return null;
        }
    }

    [Preserve]
    public abstract class VariableUIBuilder {
        public abstract bool IsSuitableFor(Type variableType);
        public abstract MessageUI CastAndEmitMessage(DebugPanelUI debugPanelUI, DebugPanel.Entry entry);
    }
    
    public abstract class VariableUIBuilder<T> : VariableUIBuilder {       
        public override bool IsSuitableFor(Type variableType) {
            return typeof(T).IsAssignableFrom(variableType);
        }
        
        public sealed override MessageUI CastAndEmitMessage(DebugPanelUI debugPanelUI, DebugPanel.Entry entry) {
            if (entry.message is DebugVariableMessage messageT && messageT.variable is DebugVariable<T> variableT)
                return EmitMessage(debugPanelUI, variableT);
            return null;
        }
        
        public abstract MessageUI EmitMessage(DebugPanelUI debugPanelUI, DebugVariable<T> variable);
    }
}