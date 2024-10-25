using UnityEditor;
using UnityEngine;

namespace Yurowm.DebugTools {
    public class BoolVariableEditor : VariableEditor<bool> {
        protected override void Edit(Rect rect, DebugVariable<bool> variable) {
            var value = variable.Get();
            if (value != EditorGUI.Toggle(rect, value)) 
                variable.Set(!value);
        }
    }
    
    public class FloatVariableEditor : VariableEditor<float> {
        protected override void Edit(Rect rect, DebugVariable<float> variable) {
            if (variable is DebugVariableRange<float> range) {
                range.Set(EditorGUI.Slider(rect, range.Get(), range.min, range.max));
            } else {
                var value = variable.Get();
                var newValue = EditorGUI.FloatField(rect, value);
                if (value != newValue) 
                    variable.Set(newValue);
            }
        }
    }
    
    public class IntVariableEditor : VariableEditor<int> {
        protected override void Edit(Rect rect, DebugVariable<int> variable) {
            if (variable is DebugVariableRange<int> range) {
                range.Set(EditorGUI.IntSlider(rect, range.Get(), range.min, range.max));
            } else {
                var value = variable.Get();
                var newValue = EditorGUI.IntField(rect, value);
                if (value != newValue) 
                    variable.Set(newValue);
            }
        }
    }
    
    public class LongVariableEditor : VariableEditor<long> {
        protected override void Edit(Rect rect, DebugVariable<long> variable) {
            var value = variable.Get();
            var newValue = EditorGUI.LongField(rect, value);
            if (value != newValue) 
                variable.Set(newValue);
        }
    }
    
    public class TextVariableEditor : VariableEditor<string> {
        protected override void Edit(Rect rect, DebugVariable<string> variable) {
            var value = variable.Get();
            var newValue = EditorGUI.TextField(rect, value);
            if (value != newValue)
                variable.Set(newValue);
        }
    }
}