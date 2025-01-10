using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.ObjectEditors;
using Yurowm.UI;

namespace Yurowm.Localizations {
    [CustomPropertyDrawer(typeof(LocalizationKeyAttribute))]
    public class LocalizationKeyProperty : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var rect = position;
            rect.width -= 40;
            
            EditorGUI.PropertyField(rect, property, label);
            
            rect = position;
            rect.xMin = rect.xMax - 40;
            rect.width = 20;
            
            if (GUI.Button(rect, "<", EditorStyles.miniButton)) {
                var menu = new GenericMenu();
                LocalizationEditor
                    .GetKeyList()
                    .ForEach(k => menu.AddItem(new GUIContent(k), false, () => property.stringValue = k));    
                if (menu.GetItemCount() > 0) 
                    menu.ShowAsContext();
                GUI.FocusControl("");
            }
            
            rect = position;
            rect.xMin = rect.xMax - 20;
            
            if (GUI.Button(rect, "E", EditorStyles.miniButton)) 
                LocalizationEditor.EditPopup(property.stringValue);
        }
    }
}
