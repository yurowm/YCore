using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.Utilities;

namespace Yurowm.UI {
    [CustomEditor(typeof(LayoutPreset<,>), true)]
    public class LayoutPresetEditor : Editor {
        
        [MenuItem("Yurowm/Tools/Apply all Layout Presets")]
        static void ApplyAll() {
            FindObjectsOfType<LayoutPreset>(true)
                .ForEach(p => p.OnScreenResize());
        }
        
        LayoutPreset editable;
        SerializedProperty presetsProperty;
        int indexToEdit = -1;
        
        LayoutPreset.Layout layout = LayoutPreset.Layout.Landscape;
        
        void OnEnable() {
            editable = target as LayoutPreset;
            
            presetsProperty = serializedObject.FindProperty("presets");
        }
        
        Component GetTargetComponent() {
            return editable.GetType()
                .GetProperty("Target")?.GetValue(editable) as Component;
        }
        
        object GetElementAt(int index) {
            if (editable == null)
                return null;
            
            var list = editable.GetType().GetField("presets")?.GetValue(editable);
            
            return (list as IList)?[index];
        }
        
        void Read(int index) {
            var element = GetElementAt(index);
                
            if (element == null) return;
            
            element.GetType().GetMethod("Read")?.Invoke(element, new object[] {
                GetTargetComponent()
            });
            
            ForceSave();
        }
        
        void Write(int index) {
            var element = GetElementAt(index);
                
            if (element == null) return;
            
            element.GetType().GetMethod("Write")?.Invoke(element, new object[] {
                GetTargetComponent()
            });
            
            ForceSave();
        }

        void ForceSave() {
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        public override void OnInspectorGUI() {
            if (presetsProperty == null) {
                base.OnInspectorGUI();
                return;
            }
            
            serializedObject.Update();
            
            DrawPropertiesExcluding(serializedObject, presetsProperty.name);
            
            if (GUIHelper.Button(null, "Apply Actual", EditorStyles.miniButton)) 
                editable.OnScreenResize();
            
            for (var i = 0; i < presetsProperty.arraySize; i++) {
                var preset = presetsProperty.GetArrayElementAtIndex(i);
                var l = (LayoutPreset.Layout) preset.FindPropertyRelative("layout").enumValueFlag;
                using (GUIHelper.SingleLine.Start(l == 0 ? "-" : l.ToText())) {
                    if (GUILayout.Button("Read", EditorStyles.miniButton)) 
                        Read(i);
                    if (GUILayout.Button("Write", EditorStyles.miniButton)) 
                        Write(i);
                    
                    if (preset.hasChildren && GUILayout.Button("Edit", EditorStyles.miniButton)) {
                        indexToEdit = indexToEdit == i ? -1 : i;
                    }
                    
                    if (GUILayout.Button("Remove", EditorStyles.miniButton)) {
                        presetsProperty.DeleteArrayElementAtIndex(i);
                        if (indexToEdit == i) indexToEdit = -1;
                        i--;
                        break;
                    }
                }
                if (indexToEdit == i) {
                    using var indentLevel = GUIHelper.IndentLevel.Start();
                    var end = preset.GetEndProperty();
                    preset.NextVisible(true);
                    do {
                       if (SerializedProperty.EqualContents(end, preset)) break;
                       
                       EditorGUILayout.PropertyField(preset, true);
                    } while (preset.NextVisible(false));
                }
            }
            
            using (GUIHelper.Horizontal.Start()) {
                layout = (LayoutPreset.Layout) EditorGUILayout
                    .EnumFlagsField(layout, GUILayout.Width(EditorGUIUtility.labelWidth));
                using (GUIHelper.Lock.Start(layout == 0)) 
                    if (GUILayout.Button("Write New", EditorStyles.miniButton)) {
                        presetsProperty.arraySize++;
                        int i = presetsProperty.arraySize - 1;
                        var preset = presetsProperty.GetArrayElementAtIndex(i);
                        preset.FindPropertyRelative("layout").enumValueFlag = (int) layout;
                        serializedObject.ApplyModifiedProperties();
                        Write(i);
                    }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}