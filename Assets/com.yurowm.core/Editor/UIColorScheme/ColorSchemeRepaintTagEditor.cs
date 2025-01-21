using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.GUIHelpers;

namespace Yurowm.Colors {
    [CustomEditor(typeof(ColorSchemeRepaintTag))]
    [CanEditMultipleObjects]
    public class ColorSchemeRepaintTagEditor : UnityEditor.Editor {
    
        string[] allTags;
        
        SerializedProperty colorTag_property;
        
        void OnEnable() {
            allTags = UIColorScheme.storage.items
                .SelectMany(s => s.colors.Values)
                .Select(e => e.key)
                .Distinct()
                .OrderBy(t => t)
                .ToArray();
            
            colorTag_property = serializedObject.FindProperty("colorTag");
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            
            serializedObject.Update();
            
            if (GUIHelper.Button(
                colorTag_property.displayName,
                colorTag_property.hasMultipleDifferentValues ?
                    "-" : colorTag_property.stringValue, 
                EditorStyles.popup)) {
                
                GenericMenu menu = new GenericMenu();

                foreach (var tag in allTags) {
                    var t = tag;
                    menu.AddItem(new GUIContent(tag), 
                        !colorTag_property.hasMultipleDifferentValues && tag == colorTag_property.stringValue, 
                        () => {
                           colorTag_property.stringValue = t; 
                           serializedObject.ApplyModifiedProperties();
                        });
                }
                
                if (menu.GetItemCount() > 0) 
                    menu.ShowAsContext();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}