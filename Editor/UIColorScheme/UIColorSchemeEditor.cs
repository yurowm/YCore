using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.GUIHelpers;
using Yurowm.ObjectEditors;
using Yurowm.Utilities;

namespace Yurowm.Colors {
    public class UIColorSchemeEditor : ObjectEditor<UIColorScheme> {
        public override void OnGUI(UIColorScheme scheme, object context = null) {
            foreach (var key in scheme.colors.Keys.ToArray()) {
                var entry = scheme.colors[key];
                using (GUIHelper.Horizontal.Start()) {
                    entry.key = EditorGUILayout.TextField(entry.key, GUILayout.Width(EditorGUIUtility.labelWidth));
                    entry.color = EditorGUILayout.ColorField(entry.color, GUILayout.ExpandWidth(true));
                }
                scheme.colors[key] = entry;
            }
            if (GUIHelper.Button("New Color Entry", "Add")) {
                scheme.colors.Add(YRandom.staticMain.GenerateKey(16), new UIColorScheme.ColorEntry() {
                    key = "",
                    color = Color.white
                });
            }
        }
    }
}