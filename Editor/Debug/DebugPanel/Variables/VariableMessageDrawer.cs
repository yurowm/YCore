using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.Utilities;

namespace Yurowm.DebugTools {
    public class VariableMessageDrawer : MessageDrawer<DebugVariableMessage> {
        
        static VariableEditor[] all = Utils
            .FindInheritorTypes<VariableEditor>(true, true)
            .Where(t => t.IsInstanceReadyType())
            .Select(Activator.CreateInstance)
            .Cast<VariableEditor>()
            .ToArray();
        
        static Dictionary<Type, VariableEditor> byType = new Dictionary<Type, VariableEditor>();
        
        static VariableEditor Get(Type type) {
            if (!byType.TryGetValue(type, out var editor)) {
                editor = all.FirstOrDefault(d => d.IsSuitableFor(type));
                byType.Add(type, editor);
            }
            
            return editor;
        }
        
        protected override void Draw(Rect rect, DebugVariableMessage message) {
            Get(message.variable.GetVariableType())?.CastAndEdit(rect, message.variable);
        }

        protected override void DrawFull(DebugVariableMessage message) { }
        protected override bool IsEmpty(DebugVariableMessage message) => true;
    }
    
    public abstract class VariableEditor {
        public abstract bool IsSuitableFor(Type variableType);
        
        public abstract void CastAndEdit(Rect rect, DebugVariable message);
    }
    
    public abstract class VariableEditor<T> : VariableEditor {
        public override bool IsSuitableFor(Type variableType) {
            return typeof(T).IsAssignableFrom(variableType);
        }

        public sealed override void CastAndEdit(Rect rect, DebugVariable message) {
            if (message is DebugVariable<T> messageT)
                Edit(rect, messageT);
        }

        protected abstract void Edit(Rect rect, DebugVariable<T> variable);
    }
}