using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.ObjectEditors;

namespace Yurowm.Localizations {
    public class LocalizedTextEditor : ObjectEditor<LocalizedText> {
        public override void OnGUI(LocalizedText text, object context = null) {
            text.text = EditorGUILayout.TextArea(text.text);
        }
    }
}