using UnityEditor;
using UnityEngine;
using Yurowm.GUIHelpers;
using Yurowm.Utilities;

namespace Yurowm.Properties {
    [CustomPropertyDrawer(typeof(int2))]
    public class Int2Editor : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            using (GUIHelper.IndentLevel.Start()) {
                Rect xl = new Rect(position.x, position.y, 12, position.height);
                Rect x = new Rect(xl.xMax, position.y, position.width / 2 - xl.width, position.height);

                Rect yl = new Rect(x.xMax, position.y, 12, position.height);
                Rect y = new Rect(yl.xMax, position.y, position.width / 2 - xl.width, position.height);

                GUI.Label(xl, "X");
                EditorGUI.PropertyField(x, property.FindPropertyRelative("_x"), GUIContent.none);
                GUI.Label(yl, "Y");
                EditorGUI.PropertyField(y, property.FindPropertyRelative("_y"), GUIContent.none);
            }
            
            EditorGUI.EndProperty();
        }
        
        public static int2 Edit(string label, int2 value, bool nullable = false) {
            using (GUIHelper.Horizontal.Start()) {
                EditorGUILayout.PrefixLabel(label);
                if (value == int2.Null) {
                    if (!nullable) {
                        value = default;
                    } else {
                        if (GUILayout.Button("Null"))
                            return default;
                        return value;
                    }
                }
                
                value.X = EditorGUILayout.IntField(value.X);
                value.Y = EditorGUILayout.IntField(value.Y);
                if (nullable && GUILayout.Button("Null", GUILayout.Width(40))) {
                    GUI.FocusControl("");
                    value = int2.Null;
                }
            }
            
            return value;
        }
    }
}