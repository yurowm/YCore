using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.ObjectEditors;

namespace Yurowm.Localizations {
    public class LocalizedTextEditor : ObjectEditor<LocalizedText> {
        public override void OnGUI(LocalizedText text, object context = null) {
            text.localized = EditorGUILayout.Toggle("Localized", text.localized);

            if (text.localized) {
                using (GUIHelper.Horizontal.Start()) {
                    text.key = EditorGUILayout.TextField("Localization Key", text.key);
                    
                    if (GUILayout.Button("<", EditorStyles.miniButton, GUILayout.Width(18))) {
                        var menu = new GenericMenu();
                        LocalizationEditor
                            .GetKeyList()
                            .ForEach(k => menu.AddItem(new GUIContent(k), false, func: () => text.key = k));    
                        if (menu.GetItemCount() > 0) 
                            menu.ShowAsContext();
                        GUI.FocusControl("");
                    }
                }
                
                if (!text.key.IsNullOrEmpty()) 
                    LocalizationEditor.EditOutside(text.key, true);
            } else
                text.text = EditorGUILayout.TextArea(text.text);
        }
    }
}