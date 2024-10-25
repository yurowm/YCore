using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.Utilities;

namespace Yurowm.Properties {
    [CustomPropertyDrawer(typeof(IntRange))]
    [CustomPropertyDrawer(typeof(FloatRange))]
    public class RangeDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            using (GUIHelper.IndentLevel.Start()) {
                Rect minRect = new Rect(position.x, position.y, position.width / 2, position.height);
                Rect maxRect = new Rect(minRect.x + minRect.width, position.y, minRect.width, position.height);

                EditorGUI.PropertyField(minRect, property.FindPropertyRelative("min"), GUIContent.none);
                EditorGUI.PropertyField(maxRect, property.FindPropertyRelative("max"), GUIContent.none);
            }
            
            EditorGUI.EndProperty();
        }
        
        public static void Edit(string label, ref IntRange range) {
            if (range == null)
                range = new IntRange();
            using (GUIHelper.Horizontal.Start()) {
                EditorGUILayout.PrefixLabel(label);
                range.min = EditorGUILayout.IntField(range.min);
                range.max = EditorGUILayout.IntField(range.max);
                if (range.min > range.max && GUILayout.Button("Fix", GUILayout.Width(30))) {
                    GUI.FocusControl("");
                    var min = range.max;
                    range.max = range.min;
                    range.min = min;
                }
            }
        }
        
        public static void Edit(string label, ref FloatRange range) {
            if (range == null)
                range = new FloatRange();
            using (GUIHelper.Horizontal.Start()) {
                EditorGUILayout.PrefixLabel(label);
                range.min = EditorGUILayout.FloatField(range.min);
                range.max = EditorGUILayout.FloatField(range.max);

                if (range.min > range.max && GUILayout.Button("Fix", GUILayout.Width(30))) {
                    GUI.FocusControl("");
                    var min = range.max;
                    range.max = range.min;
                    range.min = min;
                }
            }
        }
    }
    
    [CustomPropertyDrawer(typeof(ColorRange))]
    public class ColorRangeDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            using (GUIHelper.IndentLevel.Start()) {
                var minRect = new Rect(position.x, position.y, position.width / 2, position.height);
                var maxRect = new Rect(minRect.x + minRect.width, position.y, minRect.width, position.height);

                EditorGUI.PropertyField(minRect, property.FindPropertyRelative("start"), GUIContent.none);
                EditorGUI.PropertyField(maxRect, property.FindPropertyRelative("end"), GUIContent.none);
            }
            
            EditorGUI.EndProperty();
        }
        
        public static void Edit(string label, ref ColorRange range) {
            using (GUIHelper.Horizontal.Start()) {
                EditorGUILayout.PrefixLabel(label);
                range.start = EditorGUILayout.ColorField(range.start);
                range.end = EditorGUILayout.ColorField(range.end);
            }
        }
    }
}